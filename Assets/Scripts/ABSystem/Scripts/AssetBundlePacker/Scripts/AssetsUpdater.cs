namespace game.Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEngine;

    public class AssetsUpdater : MonoBehaviour
    {
        public enum State
        {
            None,
            Preprocess,//包内资源初始化
            Initialize,//初始化更新器
            VerifyUrl,//验证URL
            DownloadMainConfig,//下载主配置文件
            UpdateAssetBundle,//下载资源
            CopyCacheFile,//复制缓存下的文件
            Dispose,//后续工作
            Completed,//完成
            Failed,//失败
            Cancel,//取消
            Abort,//中断
            Max
        }
        public State currentState { get; private set; }
        public ErrorCode errorCode { get; private set; }

        public event System.Action<AssetsUpdater> OnUpdate;
        public event System.Action<AssetsUpdater> OnDone;

        public bool isDone { get; private set; }
        public bool isFailed { get { return errorCode != ErrorCode.None; } }
        /// <summary>
        /// 当前状态完成度
        /// </summary>
        public float currentStateCompleteValue { get; private set; }
        /// <summary>
        /// 当前状态总完成度
        /// </summary>
        public float currentStateTotalValue { get; private set; }

        private List<string> m_Urls;
        /// <summary>
        /// 当前可用url
        /// </summary>
        private string m_CurUrl;
        /// <summary>
        /// url校验器
        /// </summary>
        private URLVerify m_Verify;
        /// <summary>
        /// 文件下载器
        /// </summary>
        private FileDownload m_FileDownload;
        /// <summary>
        /// 资源下载器
        /// </summary>
        private AssetBundleDownloader m_Downloader;

        protected AssetsUpdater() { }

        private void Awake()
        {
            Reset();
        }

        /// <summary>
        /// 启动更新逻辑
        /// </summary>
        public bool StartUpdate(List<string> urls)
        {
            if (!AssetBundleManager.Instance.isReady)
                return false;
            if (!isDone && currentState != State.None)
                return false;
            m_Urls = urls;
            m_CurUrl = null;
            StartCoroutine(OnUpdating());
            return true;
        }

        IEnumerator OnUpdating()
        {
            OnUpdateState(State.Preprocess);
            yield return OnCheckInitialFile();
            OnUpdateState(State.Initialize);
            yield return StartInitialize();
            OnUpdateState(State.VerifyUrl);
            yield return StartVerifyURL();
            OnUpdateState(State.DownloadMainConfig);
            yield return StartDownloadMainConfig();
            OnUpdateState(State.UpdateAssetBundle);
            yield return StartUpdateAssetBundle();
            OnUpdateState(State.CopyCacheFile);
            yield return StartCopyCacheFile();
            OnUpdateState(State.Dispose);
            yield return StartDispose();
            OnUpdateState(errorCode == ErrorCode.None ? State.Completed : State.Failed);
            Done();
        }
        public void CancelUpdate()
        {
            StopAllCoroutines();
            if(m_Verify != null)
            {
                m_Verify.Abort();
                m_Verify = null;
            }

            if(m_Downloader != null)
            {
                m_Downloader.Cancel();
                m_Downloader = null;
            }

            if(m_FileDownload != null)
            {
                m_FileDownload.Cancel();
                m_FileDownload = null;
            }

            SaveDownloadCacheData();
            OnUpdateState(State.Cancel);
            Done();
        }

        public void AbortUpdate()
        {
            StopAllCoroutines();
            if (m_Verify != null)
            {
                m_Verify.Abort();
                m_Verify = null;
            }

            if (m_Downloader != null)
            {
                m_Downloader.Abort();
                m_Downloader = null;
            }

            if (m_FileDownload != null)
            {
                m_FileDownload.Abort();
                m_FileDownload = null;
            }

            SaveDownloadCacheData();
            OnUpdateState(State.Abort);
            Done();
        }


#region Initialize
        IEnumerator StartPreprocess()
        {
            yield break;
        }
        IEnumerator StartInitialize()
        {
            if (errorCode != ErrorCode.None)
                yield break;
            UpdateCompleteValue(0f, 1f);
            //创建资源缓存目录
            if (!Directory.Exists(PathResolver.CACHE_PATH))
                Directory.CreateDirectory(PathResolver.CACHE_PATH);
            UpdateCompleteValue(1f, 1f);
            yield return 0;
        }

        IEnumerator StartVerifyURL()
        {
            if (errorCode != ErrorCode.None)
                yield break;
            UpdateCompleteValue(0f, 1f);
            //下载地址重定向为资源根目录
            for (int i = 0; i < m_Urls.Count; i++)
                m_Urls[i] = PathResolver.CalcAssetBundleDownloadURL(m_Urls[i]);

#if ASSET_URL
            //寻找合适的资源服务器
            m_Verify = new URLVerify(m_Urls);
            m_Verify.Start();
            while (!m_Verify.isDone)
                yield return null;
            if(string.IsNullOrEmpty(m_Verify.curURL))
            {
                Debug.LogError("Can't Find valid Resource URL");
                Error(ErrorCode.InvalidURL);
            }
            m_CurUrl = m_Verify.curURL;
            m_Verify = null;
#else 
            m_CurUrl = m_Urls[0];
#endif
            UpdateCompleteValue(1f, 1f);
            yield return 0;
        }
        /// <summary>
        /// 下载主要文件至缓存目录
        /// </summary>
        /// <returns></returns>
        IEnumerator StartDownloadMainConfig()
        {
            if (errorCode != ErrorCode.None)
                yield break;

            var len = PathResolver.MAIN_CONFIG_NAME_ARRAY.Length;
            for(int i = 0; i < len; ++i)
            {
                var fileName = PathResolver.MAIN_CONFIG_NAME_ARRAY[i];
                m_FileDownload = new FileDownload(m_CurUrl, PathResolver.CACHE_PATH, fileName);
                m_FileDownload.Start();
                while (!m_FileDownload.isDone)
                    yield return null;
                if(m_FileDownload.isFailed)
                {
                    Error(ErrorCode.DownloadMainConfigFileFailed, fileName + "download failed");
                    yield break;
                }
                m_FileDownload = null;
                UpdateCompleteValue(i, len);
            }

            yield return 0;
        }
        /// <summary>
        /// 更新AB资源
        /// </summary>
        /// <returns></returns>
        IEnumerator StartUpdateAssetBundle()
        {
            if (errorCode != ErrorCode.None)
                yield break;

            UpdateCompleteValue(0f, 0f);
            //载入ResourceManifest
            var oldResManifestFile = OnLoadResourceFile();
            var path = PathResolver.GetCacheFileFullName(PathResolver.RESOURCES_MANIFEST_FILE_NAME);
            var newResManifestFile = PathResolver.LoadResourceManifestByPath(path);
            if(null == newResManifestFile)
            {
                Error(ErrorCode.loadNewResourcesManiFestFailed, "Can't Load new version ResourceManifest");
                yield break;
            }

            //载入MainManifest
            var oldManifest = PathResolver.LoadMainManifest();
            var newManifest = PathResolver.LoadCacheMainManifest();
            if(null == newManifest)
            {
                Error(ErrorCode.LoadNewMainManifestFailed, "can't load new version MainManifest");
                yield break;
            }

            var downFiles = new List<string>();
            var deleteFiles = new List<string>();
            CompareAsset(ref downFiles, ref deleteFiles, oldManifest, newManifest, oldResManifestFile, newResManifestFile);

            //删除废气文件
            if(deleteFiles.Count > 0)
            {
                for(int i = 0; i < deleteFiles.Count;++i)
                {
                    var fullName = PathResolver.GetFileFullName(deleteFiles[i]);
                    if(File.Exists(fullName))
                    {
                        File.Delete(fullName);
                        yield return 0;
                    }
                }
            }

            //更新下载资源
            m_Downloader = new AssetBundleDownloader(m_CurUrl);
            m_Downloader.Start(PathResolver.PATH, downFiles, newResManifestFile);
            while(!m_Downloader.isDone)
            {
                UpdateCompleteValue(m_Downloader.completedSize, m_Downloader.totalSize);
                yield return 0;
            }
            if(m_Downloader.isFailed)
            {
                Error(ErrorCode.DonwloadAssetBundleFailed);
                yield break;
            }
        }

        /// <summary>
        /// 拷贝缓存数据
        /// </summary>
        IEnumerator StartCopyCacheFile()
        {
            if (errorCode != ErrorCode.None)
                yield break;
            for (int i = 0; i < PathResolver.MAIN_CONFIG_NAME_ARRAY.Length; ++i)
            {
                var file = PathResolver.MAIN_CONFIG_NAME_ARRAY[i];
                var res = PathResolver.GetCacheFileFullName(file);
                var dest = PathResolver.GetFileFullName(file);
                UpdateCompleteValue(i, PathResolver.MAIN_CONFIG_NAME_ARRAY.Length);
                yield return FileHelper.CopyStreamingAssetsToFile(res, dest);
            }
        }

        private IEnumerator StartDispose()
        {
            UpdateCompleteValue(0f, 1f);
            if (errorCode != ErrorCode.None)
            {
                //缓存已经下载过的内容，方便下次检查
                SaveDownloadCacheData();
            }
            else
            {
                //删除缓存目录，重启资源管理器
                if (Directory.Exists(PathResolver.CACHE_PATH))
                    Directory.Delete(PathResolver.CACHE_PATH, true);
                if (AssetBundleManager.Instance != null)
                    AssetBundleManager.Instance.Relaunch();
            }
            UpdateCompleteValue(1f, 1f);
            yield return 0;
        }

        void Done()
        {
            isDone = true;
            OnDone?.Invoke(this);
        }
#endregion

#region Local
        private void Reset()
        {
            isDone = false;
            errorCode = ErrorCode.None;
            currentState = State.None;
            currentStateCompleteValue = 0;
            currentStateTotalValue = 0;
            m_CurUrl = null;
        }

        void OnUpdateState(State state)
        {
            currentState = state;
            OnUpdate?.Invoke(this);
        }

        void UpdateCompleteValue(float current)
        {
            UpdateCompleteValue(current, currentStateTotalValue);
        }

        void UpdateCompleteValue(float current,float total)
        {
            currentStateCompleteValue = current;
            currentStateTotalValue = total;
            OnUpdate?.Invoke(this);
        }

        void Error(ErrorCode code,string msg = null)
        {
            errorCode = code;
            var value = string.IsNullOrEmpty(msg) ? errorCode.ToString() : errorCode.ToString() + "-" + msg;
            Debug.LogError(value);
        }

        /// <summary>
        /// 写入下载缓存信息用于断点续传
        /// </summary>
       void SaveDownloadCacheData()
        {
            if (currentState < State.UpdateAssetBundle)
                return;
            if (!Directory.Exists(PathResolver.CACHE_PATH))
                return;
            var manifestPath = PathResolver.GetCacheFileFullName(PathResolver.MAIN_MANIFEST_FILE_NAME);
            var manifestFile = PathResolver.LoadMainManifestByPath(manifestPath);
            if (null == manifestFile)
                return;
            //加载缓存信息文件
            var cache = new DownloadCache();
            cache.Load(PathResolver.DOWNLOADCACHE_FILE_PATH);
            //获取下载器完成任务列表
            if (m_Downloader != null && m_Downloader.completeDownloads != null && m_Downloader.completeDownloads.Count > 0)
            {
                foreach(var name in m_Downloader.completeDownloads)
                {
                    var hashCode = manifestFile.GetAssetBundleHash(name);
                    if(hashCode.isValid && !cache.data.assetBundles.ContainsKey(name))
                    {
                        var elem = new DownloadCacheData.AssetBundle() { assetbundleName = name, hash = hashCode.ToString() };
                        Debug.Log(cache.data.assetBundles.Count + " - Cache Add:" + name);
                        cache.data.assetBundles.Add(name, elem);
                    }
                }
            }

            if (!cache.IsEmpty())
                cache.Save(PathResolver.DOWNLOADCACHE_FILE_PATH);
        }

        IEnumerator OnCheckInitialFile()
        {
            //创建资源根目录
            if (!Directory.Exists(PathResolver.PATH))
                Directory.CreateDirectory(PathResolver.PATH);
            //判断主文件是否存在，不存在则拷贝本地备份
            var initialCopy = false;
            var resManifestFile = PathResolver.GetFileFullName(PathResolver.RESOURCES_MANIFEST_FILE_NAME);
            if(!File.Exists(resManifestFile))
            {
                initialCopy = true;
            }
            else
            {
                var fullName = PathResolver.GetFileFullName(PathResolver.RESOURCES_MANIFEST_FILE_NAME);
                var initialFullName = PathResolver.GetInitialFileFullName(PathResolver.RESOURCES_MANIFEST_FILE_NAME);
                var cacheFullName = PathResolver.GetCacheFileFullName(PathResolver.RESOURCES_MANIFEST_FILE_NAME);
                yield return FileHelper.CopyStreamingAssetsToFile(initialFullName, cacheFullName);

                //判断包内初始目录是否完整
                var initialFile = PathResolver.LoadResourceManifestByPath(initialFullName);
                if(null == initialFile)
                {
                    Error(ErrorCode.LoadResourcesMainFestFailed, "Initial path don't contains" + PathResolver.RESOURCES_MANIFEST_FILE_NAME + "!");
                    yield break;
                }
                //全局路径是否需要拷贝
                var current = PathResolver.LoadResourceManifestByPath(fullName);
                if (null == current)
                    initialCopy = true;
                else if (current.data.version < initialFile.data.version)
                    initialCopy = true;

                //删除缓存文件
                if (File.Exists(cacheFullName))
                    File.Delete(cacheFullName);
            }

            if(initialCopy)
            {
                yield return CopyAllInitialFiles();
            }
        }
        IEnumerator CopyAllInitialFiles()
        {
            //拷贝配置文件
            foreach (var file in PathResolver.MAIN_CONFIG_NAME_ARRAY)
                yield return PathResolver.CopyInitialFileFile(file);
            //拷贝AB文件
            var resManifest = PathResolver.LoadResourceManifest();
            if(null == resManifest)
            {
                Error(ErrorCode.PreprocessError, "Can't load ResourcesManifes File");
                yield break;
            }
            var itor = resManifest.data.assetbundles.GetEnumerator();
            while(itor.MoveNext())
            {
                if(itor.Current.Value.isNative)
                {
                    var assetName = itor.Current.Value.assetBundleName;
                    var dest = PathResolver.GetFileFullName(assetName);
                    var dir = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    //copy
                    yield return PathResolver.CopyInitialFileFile(assetName);
                }
            }
            itor.Dispose();
        }

        ResourcesManifest OnLoadResourceFile()
        {
            return PathResolver.LoadResourceManifest();
        }

        void CompareAsset(
            ref List<string> downFiles,
            ref List<string> deleteFiles,
            AssetBundleManifest oldManifest,
            AssetBundleManifest newManifest,
            ResourcesManifest oldResManifest,
            ResourcesManifest newResManifest)
        {
            if (downFiles != null) downFiles.Clear();
            if (deleteFiles != null) deleteFiles.Clear();
            if (oldManifest == null)
            {
                Error(ErrorCode.LoadMainManifestFailed, "Local Manifest no Find");
                return;
            }
            if (newManifest == null)
            {
                Error(ErrorCode.LoadNewMainManifestFailed, "Load New MainManifest no Find");
                return;
            }
            if (newResManifest == null)
            {
                Error(ErrorCode.loadNewResourcesManiFestFailed, "Load New ResourceManifest no Find");
                return;
            }
            //标记位
            var old_ver_bit = 1<<0;//存在旧资源
            var new_ver_bit = 1 << 1;//存在新资源
            var old_ver_native_bit = 1 << 2;//存在旧的本地资源
            var new_ver_native_bit = 1 << 3;//存在新的本地资源

            var tempDic = new Dictionary<string, int>();
            //标记旧资源
            var allAssetBundle = oldManifest.GetAllAssetBundles();
            foreach (var name in allAssetBundle)
                SetDictionaryBit(ref tempDic, name, old_ver_bit);
            //标记新资源
            var allNewAssetBundle = newManifest.GetAllAssetBundles();
            foreach (var name in allNewAssetBundle)
                SetDictionaryBit(ref tempDic, name, new_ver_bit);
            //标记旧本地资源
            if(oldResManifest.data != null && oldResManifest.data.assetbundles != null)
            {
                var itor = oldResManifest.data.assetbundles.GetEnumerator();
                while(itor.MoveNext())
                {
                    if(itor.Current.Value.isNative)
                    {
                        var name = itor.Current.Value.assetBundleName;
                        SetDictionaryBit(ref tempDic, name, old_ver_native_bit);
                    }
                }
            }
            //标记新的本地资源
            if(newResManifest.data != null && newResManifest.data.assetbundles != null)
            {
                var itor = newResManifest.data.assetbundles.GetEnumerator();
                while(itor.MoveNext())
                {
                    if(itor.Current.Value.isNative)
                    {
                        var name = itor.Current.Value.assetBundleName;
                        SetDictionaryBit(ref tempDic, name, new_ver_native_bit);
                    }
                }
            }

            //优先级：both>add>delete
            //both: 第0位和第1位标记的
            //delete : 第0位标记
            //add:第2位未标记，第3位标记的
            int both = old_ver_bit | new_ver_bit;//2个版本都存在的资源
            var addFiles = new List<string>();
            var bothFiles = new List<string>();
            using (var itor = tempDic.GetEnumerator())
            {
                while (itor.MoveNext())
                {
                    var name = itor.Current.Key;
                    var mask = itor.Current.Value;
                    if ((mask & new_ver_native_bit) == new_ver_native_bit
                        && (mask & old_ver_native_bit) == 0)
                        addFiles.Add(name);
                    else if ((mask & both) == both)
                        bothFiles.Add(name);
                    else if ((mask & old_ver_bit) == old_ver_bit)
                        deleteFiles.Add(name);
                }
                itor.Dispose();
            }

            //加载下载缓存数据
            var download = new DownloadCache();
            download.Load(PathResolver.DOWNLOADCACHE_FILE_PATH);
            if (download.IsEmpty())
                download = null;

            //记录需要下载的文件
            {
                //加入新增文件
                downFiles.AddRange(addFiles);
                //判断同时存在文件的哈希
                foreach(var name in bothFiles)
                {
                    var fullName = PathResolver.GetFileFullName(name);
                    if(File.Exists(fullName))
                    {
                        //判断哈希是否相同
                        var oldHash = oldManifest.GetAssetBundleHash(name).ToString();
                        var newHash = newManifest.GetAssetBundleHash(name).ToString();
                        if (oldHash.CompareTo(newHash) == 0)
                            continue;
                        downFiles.Add(name);
                    }
                }

                //过滤已下载的文件
                if(null != download)
                {
                    var itor = download.data.assetBundles.GetEnumerator();
                    while(itor.MoveNext())
                    {
                        var elem = itor.Current.Value;
                        var name = elem.assetbundleName;
                        var fullName = PathResolver.GetFileFullName(name);
                        if(File.Exists(fullName))
                        {
                            var cacheHash = elem.hash;
                            var newHash = newManifest.GetAssetBundleHash(name).ToString();
                            if (!string.IsNullOrEmpty(cacheHash) && cacheHash.CompareTo(newHash) == 0)
                                downFiles.Remove(name);
                        }
                    }
                }
            }
        }

        static void SetDictionaryBit(ref Dictionary<string,int> dir,string name,int bit)
        {
            if (dir.ContainsKey(name))
                dir[name] |= bit;
            else
                dir.Add(name, bit);
        }


#endregion
    }
}
