namespace game.Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public enum ErrorCode
    {
        None = 0,
        ParameterError,//参数错误
        TimeOut ,//超时
        PreprocessError,//预处理错误

        LoadMainManifestFailed = 101,
        LoadResourcesMainFestFailed = 102,
        LoadResourcesPackagesFailed = 103,
        LoadNewMainManifestFailed = 104,
        loadNewResourcesManiFestFailed = 105,

        NotFindAssetBundle = 201,//未找到有效AB

        InvalidURL = 1001,//未能识别url服务器
        ServerNoResponse = 1002,//服务器未响应
        DownloadFailed = 1003,//下载失败
        DownloadMainConfigFileFailed = 1004,//主配置文件下载失败
        DonwloadAssetBundleFailed = 1005,//AB下载失败
    }

    public class AssetBundleDownloader
    {
        /// <summary>
        /// 并发下载
        /// </summary>
        public const int CONCURRENCE_DOWNLOAD_MAX = 1;

        /// <summary>
        /// URL
        /// </summary>
        public string url;
        /// <summary>
        /// 下载根路径
        /// </summary>
        public string path;
        /// <summary>
        /// 下载完成标志
        /// </summary>
        public bool isDone { get; private set; }

        public ErrorCode errorCode { get; private set; }
        public bool isFailed { get { return errorCode != ErrorCode.None; } }

        public long completedSize { get; private set; }
        public long totalSize { get; private set; }

        public List<string> unCompleteDownloads { get; private set; }//需要下载的资源列表

        public List<string> completeDownloads { get; private set; }//已下载资源

        public List<string> failedDownloads { get; private set; }//下载失败的资源

        private List<HttpAsyDownload> m_Downloads = new List<HttpAsyDownload>();//
        /// <summary>
        /// 资源描述集合
        /// </summary>
        private ResourcesManifest m_Manifest;
        /// <summary>
        /// 锁对象
        /// </summary>
        private object m_Lock = new object();

        public AssetBundleDownloader(string url)
        {
            this.url = url;
            isDone = false;
            errorCode = ErrorCode.None;
            completedSize = 0;
            totalSize = 0;
            unCompleteDownloads = new List<string>();
            completeDownloads = new List<string>();
            failedDownloads = new List<string>();

            System.Net.ServicePointManager.DefaultConnectionLimit = CONCURRENCE_DOWNLOAD_MAX;
        }

        public bool Start(string path,List<string> assetbundles,ResourcesManifest manifest)
        {
            Abort();

            if(manifest == null)
            {
                isDone = true;
                errorCode = ErrorCode.ParameterError;
                return false;
            }

            OnInitializeDownload(path, assetbundles, manifest);
            OnUpdateState();
            OnDownloadAll();
            return true;
        }

        public void Cancel()
        {
            foreach (var item in m_Downloads)
                item.Cancel();
        }

        public void Abort()
        {
            foreach (var item in m_Downloads)
                item.Abort();
        }

        void OnInitializeDownload(string path,List<string> assetbundles,ResourcesManifest manifest)
        {
            this.path = path;
            unCompleteDownloads = assetbundles;
            m_Manifest = manifest;

            isDone = false;
            errorCode = ErrorCode.None;
            completeDownloads.Clear();
            failedDownloads.Clear();

            if (unCompleteDownloads == null) unCompleteDownloads = new List<string>();

            totalSize = 0;
            completedSize = 0;

            foreach(var item in unCompleteDownloads)
            {
                var ab = manifest.Find(item);
                if (ab == null) continue;
                totalSize += ab.size;
            }
        }

        public bool IsDownLoading(string fileName)
        {
            var ab = m_Downloads.Find((d) => { return d.localName == fileName; });
            return ab != null;
        }

        HttpAsyDownload GetFreeDownload()
        {
            lock(m_Lock)
            {
                foreach (var item in m_Downloads)
                    if (item.isDone)
                        return item;

                if(m_Downloads.Count < System.Net.ServicePointManager.DefaultConnectionLimit)
                {
                    var item = new HttpAsyDownload(url);
                    m_Downloads.Add(item);
                    return item;
                }
                return null;
            }
        }
        /// <summary>
        /// 下载所有资源
        /// </summary>
        void OnDownloadAll()
        {
            lock(m_Lock)
            {
                foreach (var item in unCompleteDownloads)
                    if (!OnDownload(item))
                        break;
            }
        }
        /// <summary>
        /// 更新状态
        /// </summary>
        void OnUpdateState()
        {
            isDone = unCompleteDownloads.Count == 0;
            errorCode = failedDownloads.Count > 0 ? ErrorCode.DownloadFailed : errorCode;
        }

        /// <summary>
        /// 启动下载
        /// </summary>
        bool OnDownload(string assetbundleName)
        {
            lock(m_Lock)
            {
                var ab = m_Manifest.Find(assetbundleName);
                if(null == ab)
                {
                    Debug.LogWarning(string.Format("AssetBundleName is invalid : {0}", assetbundleName));
                    return true;
                }

                var fileName = assetbundleName;
                if(!IsDownLoading(fileName))
                {
                    var d = GetFreeDownload();
                    if (null == d)
                        return false;
                    d.Start(path, fileName, OnDownloadNotify, OnDownloadError);
                }
                return true;
            }
        }

        void OnDownloadSuccess(string fileName)
        {
            lock(m_Lock)
            {
                if (unCompleteDownloads.Contains(fileName))
                    unCompleteDownloads.Remove(fileName);
                completeDownloads.Add(fileName);
            }
        }

        void OnDownloadNotify(HttpAsyDownload d,long size)
        {
            lock(m_Lock)
            {
                if(d.isDone)
                {
                    OnDownloadSuccess(d.localName);
                    OnDownloadAll();
                }

                completedSize += size;
                OnUpdateState();
            }
        }

        void OnDownloadError(HttpAsyDownload d)
        {
            lock(m_Lock)
            {
                if (unCompleteDownloads.Contains(d.localName))
                    unCompleteDownloads.Remove(d.localName);
                failedDownloads.Add(d.localName);
                OnDownloadAll();
                OnUpdateState();
            }
        }
    }
}

