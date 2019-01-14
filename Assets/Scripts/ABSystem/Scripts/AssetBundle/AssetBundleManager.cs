#if !AB_MODE && UNITY_EDITOR
#else
#define _AB_MODE_
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// https://github.com/tangzx/ABSystem
/// </summary>

namespace game.Assets
{
    public enum LoadState
    {
        State_None = 0,
        State_Loading = 1,
        State_Error = 2,
        State_Complete = 3
    }

    public class AssetBundleManager : MonoBehaviour
    {
        public static AssetBundleManager Instance;
        public static string NAME = "AssetBundleManager";
        public static bool enableLog
#if UNITY_EDITOR
            = true;
#else
            = false;
#endif
        public ErrorCode errorCode { get; private set; }

        public delegate void LoadAssetCompleteHandler(AssetBundleInfo info);
        public delegate void LoaderCompleteHandler(AssetBundleLoader info);
        public delegate void LoadProgressHandler(AssetBundleLoadProgress progress);

        /// <summary>
        /// 同时最大的加载数
        /// </summary>
        private const int MAX_REQUEST = 100;
        /// <summary>
        /// 可再次申请的加载数
        /// </summary>
        private int _requestRemain = MAX_REQUEST;
        /// <summary>
        /// 当前申请要加载的队列
        /// </summary>
        private List<AssetBundleLoader> _requestQueue = new List<AssetBundleLoader>();
        /// <summary>
        /// 加载队列
        /// </summary>
        private List<AssetBundleLoader> _currentLoadQueue = new List<AssetBundleLoader>();
        /// <summary>
        /// 未完成的
        /// </summary>
        private HashSet<AssetBundleLoader> _nonCompleteLoaderSet = new HashSet<AssetBundleLoader>();
        /// <summary>
        /// 此时加载的所有Loader记录，(用于在全加载完成之后设置 minLifeTime)
        /// </summary>
        private HashSet<AssetBundleLoader> _thisTimeLoaderSet = new HashSet<AssetBundleLoader>();
        /// <summary>
        /// 已加载完成的缓存列表
        /// </summary>
        private Dictionary<string, AssetBundleInfo> _loadedAssetBundle = new Dictionary<string, AssetBundleInfo>();
        /// <summary>
        /// 已创建的所有Loader列表(包括加载完成和未完成的)
        /// </summary>
        private Dictionary<string, AssetBundleLoader> _loaderCache = new Dictionary<string, AssetBundleLoader>();
        /// <summary>
        /// 当前是否还在加载，如果加载，则暂时不回收
        /// </summary>
        private bool _isCurrentLoading;

        private AssetBundleLoadProgress _progress = new AssetBundleLoadProgress();
        /// <summary>
        /// 进度
        /// </summary>
        public LoadProgressHandler onProgress;

        private AssetBundleDataReader _depInfoReader;

        private Action _initCallback;

        public bool isReady{ get; private set; }

        public AssetBundleManager()
        {
            Instance = this;
            isReady = false;
        }

        public AssetBundleDataReader depInfoReader { get { return _depInfoReader; } }

        protected void Awake()
        {
			InvokeRepeating("CheckUnusedBundle", 0, 5);
        }

        void Update()
        {
            if (_isCurrentLoading)
            {
                CheckNewLoaders();
                CheckQueue();
            }
        }

        public void Init(Action callback)
        {
          
        }

        public void Init(Stream depStream, Action callback)
        {
            if (depStream.Length > 4)
            {
                BinaryReader br = new BinaryReader(depStream);
                if (br.ReadChar() == 'A' && br.ReadChar() == 'B' && br.ReadChar() == 'D')
                {
                    if (br.ReadChar() == 'T')
                        _depInfoReader = new AssetBundleDataReader();
                    else
                        _depInfoReader = new AssetBundleDataBinaryReader();

                    depStream.Position = 0;
                    _depInfoReader.Read(depStream);
                }
            }

            depStream.Close();

            if (callback != null)
                callback();
        }

        void InitComplete()
        {
            if (_initCallback != null)
                _initCallback();
            _initCallback = null;
            isReady = true;
        }

        IEnumerator LoadDepInfo()
        {
            string depFile = string.Format("{0}/{1}", PathResolver.BundleCacheDir, PathResolver.DependFileName);
#if UNITY_EDITOR
            //编辑器模式下测试AB_MODE，直接读取
            depFile = PathResolver.GetFileSourceFile(PathResolver.DependFileName, false);
            if (File.Exists(depFile))
            {
                FileStream fs = new FileStream(depFile, FileMode.Open, FileAccess.Read);
                Init(fs, null);
                fs.Close();
            }
            else
#endif
            {
                string srcURL = PathResolver.GetFileSourceFile(PathResolver.DependFileName);
                WWW w = new WWW(srcURL);
                yield return w;

                if (w.error == null)
                {
                    Init(new MemoryStream(w.bytes), null);
                    File.WriteAllBytes(depFile, w.bytes);
                }
                else
                {
                    Debug.LogError(string.Format("{0} not exist!", depFile));
                }
            }
            this.InitComplete();
        }

        IEnumerator LoadData(AssetDataReader reader,string filename)
        {
            var path = string.Format("{0}/{1}", PathResolver.BundleCacheDir, filename);
#if UNITY_EDITOR
            path = PathResolver.GetFileSourceFile(filename, false);
            if (File.Exists(path))
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                reader.Read(fs);
                fs.Close();
            }
            else
#endif
            {
                string srcURL = PathResolver.GetFileSourceFile(filename);
                WWW w = new WWW(srcURL);
                yield return w;

                if (w.error == null)
                {
                    reader.Read(new MemoryStream(w.bytes));
                    File.WriteAllBytes(path, w.bytes);
                }
                else
                {
                    Debug.LogError(string.Format("{0} not exist!", path));
                }
            }
        }

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public IEnumerator Launch()
        {
#if ASSET_UPDATER
            yield return OnCheckInitialFile();
#endif
            yield return OnLaunch();
        }

        public IEnumerator Relaunch()
        {
            OnShutDown();
            yield return OnLaunch();
        }

        void OnShutDown()
        {
            this.RemoveAll();
            AssetsUtil.configs.Clear();
            AssetsUtil.atlas.Clear();
            AssetsUtil.prefabs.Clear();
        }

        IEnumerator OnLaunch()
        {
            isReady = false;
#if _AB_MODE_ || ASSET_UPDATER
            yield return LoadDepInfo();
#endif
            yield return LoadData(new AssetNameDataReader(), PathResolver.AssetFileName);
            yield return LoadData(new AssetSpriteDataReader(), PathResolver.SpriteFileName);

            this.InitComplete();
        }

        void OnDestroy()
        {
            this.RemoveAll();
        }

        /// <summary>
        /// 通过ShortName获取FullName
        /// </summary>
        /// <param name="shortFileName"></param>
        /// <returns></returns>
        public string GetAssetBundleFullName(string shortFileName)
        {
            return _depInfoReader.GetFullName(shortFileName);
        }

        /// <summary>
        /// 用默认优先级为0的值加载
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="handler">回调</param>
        /// <returns></returns>
        public AssetBundleLoader Load(string path, LoadAssetCompleteHandler handler = null)
        {
            return Load(path, 0, handler);
        } 

        /// <summary>
        /// 通过一个路径加载ab
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="prority">优先级</param>
        /// <param name="handler">回调</param>
        /// <returns></returns>
        public AssetBundleLoader Load(string path, int prority, LoadAssetCompleteHandler handler = null)
        {
#if _AB_MODE_ || ASSET_UPDATER
            AssetBundleLoader loader = this.CreateLoader(HashUtil.Get(path.ToLower()) + ".ab", path);
#else
            AssetBundleLoader loader = this.CreateLoader(path);
#endif
            loader.prority = prority;
            loader.onComplete += handler;

            _isCurrentLoading = true;
            _nonCompleteLoaderSet.Add(loader);
            _thisTimeLoaderSet.Add(loader);

            return loader;
        }

        internal AssetBundleLoader CreateLoader(string abFileName, string oriName = null)
        {
            AssetBundleLoader loader = null;

            if (_loaderCache.ContainsKey(abFileName))
            {
                loader = _loaderCache[abFileName];
            }
            else
            {
#if _AB_MODE_ || ASSET_UPDATER
                AssetBundleData data = _depInfoReader.GetAssetBundleInfo(abFileName);
                if (data == null && oriName != null)
                {
                    var extension = Path.GetExtension(oriName);
                    var shortName = Path.GetFileNameWithoutExtension(oriName);
                    data = _depInfoReader.GetAssetBundleInfoByShortName((shortName + extension).ToLower());
                }
                if (data == null)
                {
                    MissAssetBundleLoader missLoader = new MissAssetBundleLoader();
                    missLoader.bundleManager = this;
                    return missLoader;
                }

                loader = this.CreateLoader();
                loader.bundleManager = this;
                loader.bundleData = data;
                loader.bundleName = data.fullName;
#else
                loader = this.CreateLoader();
                loader.bundleManager = this;
                loader.bundleName = abFileName;
#endif
                _loaderCache[abFileName] = loader;
            }

            return loader;
        }

        protected virtual AssetBundleLoader CreateLoader()
        {
#if ASSET_UPDATER
            return new MobileAssetBundleLoader();
#elif UNITY_EDITOR && UNITY_STANDALONE_WIN
            return new EditorModeAssetBundleLoader();
#elif UNITY_IOS
            return new IOSAssetBundleLoader();
#elif UNITY_ANDROID
            return new MobileAssetBundleLoader();
#else
            return new MobileAssetBundleLoader();
#endif
        }

        void CheckNewLoaders()
        {
            if (_nonCompleteLoaderSet.Count > 0)
            {
                List<AssetBundleLoader> loaders = ListPool<AssetBundleLoader>.Get();
                loaders.AddRange(_nonCompleteLoaderSet);
                _nonCompleteLoaderSet.Clear();

                var e = loaders.GetEnumerator();
                while (e.MoveNext())
                {
                    _currentLoadQueue.Add(e.Current);
                }

                _progress = new AssetBundleLoadProgress();
                _progress.total = _currentLoadQueue.Count;

                e = loaders.GetEnumerator();
                while (e.MoveNext())
                {
                    e.Current.Start();
                }
                ListPool<AssetBundleLoader>.Release(loaders);
            }
        }
        
        public void RemoveAll()
        {
            this.StopAllCoroutines();

            _currentLoadQueue.Clear();
            _requestQueue.Clear();
            foreach (AssetBundleInfo abi in _loadedAssetBundle.Values)
            {
                abi.Dispose();
            }
            _loadedAssetBundle.Clear();
            _loaderCache.Clear();
        }

        public AssetBundleInfo GetBundleInfo(string key)
        {
            key = key.ToLower();
#if _AB_MODE_ || ASSET_UPDATER
            key = HashUtil.Get(key) + ".ab";
#endif
            var e = _loadedAssetBundle.GetEnumerator();
            while (e.MoveNext())
            {
                AssetBundleInfo abi = e.Current.Value;
                if (abi.bundleName == key)
                    return abi;
            }
            return null;
        }

        /// <summary>
        /// 请求加载Bundle，这里统一分配加载时机，防止加载太卡
        /// </summary>
        /// <param name="loader"></param>
        internal void Enqueue(AssetBundleLoader loader)
        {
            if (_requestRemain < 0)
                _requestRemain = 0;
            _requestQueue.Add(loader);
        }

        void CheckQueue()
        {
            if (_requestRemain > 0 && _requestQueue.Count > 0)
                _requestQueue.Sort();

            while (_requestRemain > 0 && _requestQueue.Count > 0)
            {
                AssetBundleLoader loader = _requestQueue[0];
                _requestQueue.RemoveAt(0);
                LoadBundle(loader);
            }
        }

        void LoadBundle(AssetBundleLoader loader)
        {
            if (!loader.isComplete)
            {
                loader.LoadBundle();
                _requestRemain--;
            }
        }

        internal void LoadError(AssetBundleLoader loader)
        {
            Debug.LogWarning("Cant load AB : " + loader.bundleName, this);
            LoadComplete(loader);
        }

        internal void LoadComplete(AssetBundleLoader loader)
        {
            _requestRemain++;
            _currentLoadQueue.Remove(loader);

            if (onProgress != null)
            {
                _progress.loader = loader;
                _progress.complete = _progress.total - _currentLoadQueue.Count;
                _progress.percent = _progress.complete / _progress.total;
                onProgress(_progress);
            }

            //all complete
            if (_currentLoadQueue.Count == 0 && _nonCompleteLoaderSet.Count == 0)
            {
                _isCurrentLoading = false;

                var e = _thisTimeLoaderSet.GetEnumerator();
                while (e.MoveNext())
                {
                    AssetBundleLoader cur = e.Current;
                    if (cur.bundleInfo != null)
                        cur.bundleInfo.ResetLifeTime();
                }
                _thisTimeLoaderSet.Clear();
            }
        }

        internal AssetBundleInfo CreateBundleInfo(AssetBundleLoader loader, AssetBundleInfo abi = null, AssetBundle assetBundle = null)
        {
            if (abi == null)
                abi = new AssetBundleInfo();
            abi.bundleName = loader.bundleName.ToLower();
            abi.bundle = assetBundle;
            abi.data = loader.bundleData;

            _loadedAssetBundle[abi.bundleName] = abi;
            return abi;
        }

        internal void RemoveBundleInfo(AssetBundleInfo abi)
        {
            abi.Dispose();
            _loadedAssetBundle.Remove(abi.bundleName);
        }

        /// <summary>
        /// 当前是否在加载状态
        /// </summary>
        public bool isCurrentLoading { get { return _isCurrentLoading; } }

		void CheckUnusedBundle()
		{
			this.UnloadUnusedBundle();
		}

        /// <summary>
        /// 卸载不用的
        /// </summary>
        public void UnloadUnusedBundle(bool force = false)
        {
            if (_isCurrentLoading == false || force)
            {
                List<string> keys = ListPool<string>.Get();
                keys.AddRange(_loadedAssetBundle.Keys);

                bool hasUnusedBundle = false;
                //一次最多卸载的个数，防止卸载过多太卡
                int unloadLimit = 20;
                int unloadCount = 0;

                do
                {
                    hasUnusedBundle = false;
                    for (int i = 0; i < keys.Count && !_isCurrentLoading && unloadCount < unloadLimit; i++)
                    {
                        if (_isCurrentLoading && !force)
                            break;

                        string key = keys[i];
                        AssetBundleInfo abi = _loadedAssetBundle[key];
                        if (abi.isUnused)
                        {
                            hasUnusedBundle = true;
                            unloadCount++;

                            this.RemoveBundleInfo(abi);
                            if(enableLog)
                                Debug.Log("[Assets]===>> Unload : " + abi.bundleName);
                            keys.RemoveAt(i);
                            i--;
                        }
                    }
                } while (hasUnusedBundle && !_isCurrentLoading && unloadCount < unloadLimit);

                ListPool<string>.Release(keys);

                if (unloadCount > 0 && enableLog)
                {
                    Debug.Log("[Assets]===>> Unload Count: " + unloadCount);
                }
            }
        }

        public void RemoveBundle(string key)
        {
            AssetBundleInfo abi = this.GetBundleInfo(key);
            if (abi != null)
            {
                this.RemoveBundleInfo(abi);
            }
        }
        
#region 包内资源操作
        void Error(ErrorCode code, string msg = null)
        {
            errorCode = code;
            var value = string.IsNullOrEmpty(msg) ? errorCode.ToString() : errorCode.ToString() + "-" + msg;
            Debug.LogError(value);
        }

        /// <summary>
        /// 拷贝包内资源
        /// </summary>
        /// <returns></returns>
        IEnumerator OnCheckInitialFile()
        {
            //创建资源根目录
            if (!Directory.Exists(PathResolver.PATH))
                Directory.CreateDirectory(PathResolver.PATH);
            //判断主文件是否存在，不存在则拷贝本地备份
            var initialCopy = false;
            var resManifestFile = PathResolver.GetFileFullName(PathResolver.RESOURCES_MANIFEST_FILE_NAME);
            if (!File.Exists(resManifestFile))
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
                if (null == initialFile)
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

            if (initialCopy)
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
            if (null == resManifest)
            {
                Error(ErrorCode.PreprocessError, "Can't load ResourcesManifes File");
                yield break;
            }
            var itor = resManifest.data.assetbundles.GetEnumerator();
            while (itor.MoveNext())
            {
                if (itor.Current.Value.isNative)
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
#endregion
    }
}
