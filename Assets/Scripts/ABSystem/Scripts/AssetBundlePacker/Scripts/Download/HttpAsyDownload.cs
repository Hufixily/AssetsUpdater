namespace game.Assets
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Threading;
    using System.Net;
    using System.IO;

    public class HttpAsyDownload
    {
        public enum ErrorCode
        {
            None,//
            Cancel,
            NoResponse,
            DownloadError,
            TimeOut,
            Abort,//强制关闭
        }
        /// <summary>
        /// 超时
        /// </summary>
        public const int TIMEOUT_TIME = 20000;
        /// <summary>
        /// 下载地址
        /// </summary>
        public string url { get; private set; }
        /// <summary>
        /// 存放根路径
        /// </summary>
        public string root { get; private set; }
        /// <summary>
        /// LocalName
        /// </summary>
        public string localName { get; private set; }
        /// <summary>
        /// FullName
        /// </summary>
        public string fullName
        {
            get { return string.IsNullOrEmpty(root) || string.IsNullOrEmpty(localName) ? null : string.Format("{0}/{1}", root, localName); }
        }
        /// <summary>
        /// 是否接受
        /// </summary>
        public bool isDone { get; private set; }
        /// <summary>
        /// 错误码
        /// </summary>
        public ErrorCode errorCode { get; private set; }
        /// <summary>
        /// 总下载大小
        /// </summary>
        public long length { get; private set; }
        /// <summary>
        /// 当前已下载大小
        /// </summary>
        public long completeLength { get; private set; }
        /// <summary>
        /// 下载回调
        /// </summary>
        private Action<HttpAsyDownload, long> m_NotifyCallback;
        /// <summary>
        /// 错误回调
        /// </summary>
        private Action<HttpAsyDownload> m_ErrorCallback;
        /// <summary>
        /// 
        /// </summary>
        private DownloadContent m_Content = null;
        /// <summary>
        /// Http请求
        /// </summary>
        private HttpWebRequest m_Request = null;
        /// <summary>
        /// 所对象，不是多线程无视
        /// </summary>
        private object m_Lock = new object();

        public HttpAsyDownload(string url)
        {
            this.url = url;
        }
        /// <summary>
        /// 开始下载
        /// </summary>
        public void Start(string root, string localFileName, Action<HttpAsyDownload, long> nofity = null, Action<HttpAsyDownload> error = null)
        {
            lock (m_Lock)
            {
                this.Abort();
                this.root = root;
                localName = localFileName;
                isDone = false;
                errorCode = ErrorCode.None;
                m_NotifyCallback = nofity;
                m_ErrorCallback = error;
                m_Content = new DownloadContent(fullName, false);
                length = 0;
                OnDownload();
            }
        }
        /// <summary>
        /// 取消下载
        /// </summary>
        public void Cancel()
        {
            lock (m_Lock)
            {
                if (null != m_Content && m_Content.state == DownloadContent.State.Downloading)
                {
                    m_Content.Cancel();
                }
                else
                {
                    isDone = true;
                }
            }
        }
        /// <summary>
        /// 终止下载
        /// </summary>
        public void Abort()
        {
            lock(m_Lock)
            {
                if(m_Content != null && m_Content.state == DownloadContent.State.Downloading)
                {
                    OnFailed(ErrorCode.Abort);
                }
            }
        }

        void OnFinish()
        {
            lock(m_Lock)
            {
                if(m_Content != null)
                {
                    m_Content.Complete();
                    m_Content.Close();
                    m_Content = null;
                }
                if(m_Request != null)
                {
                    m_Request.Abort();
                    m_Request = null;
                }
                isDone = true;
            }
        }

        private void OnFailed(ErrorCode code)
        {
            lock(m_Lock)
            {
                if(m_Content != null)
                {
                    m_Content.Failed();
                    m_Content.Close();
                    m_Content = null;
                }

                if(m_Request != null)
                {
                    m_Request.Abort();
                    m_Request = null;
                }

                isDone = false;
                errorCode = code;

                m_ErrorCallback?.Invoke(this);
            }
        }

        private void OnDownload()
        {
            try
            {
                lock(m_Lock)
                {
                    //尝试下载资源
                    m_Request = (HttpWebRequest)WebRequest.Create(url + localName);
                    m_Request.KeepAlive = false;
                    m_Request.Timeout = TIMEOUT_TIME;
                    m_Request.IfModifiedSince = m_Content.lastModified;
                    var result = (IAsyncResult)m_Request.BeginGetResponse(new AsyncCallback(OnResponseCallback), m_Request);
                    RegisterTimeOut(result.AsyncWaitHandle);
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("HttpAsyDownload -\"" + localName + "\"download failed!" + "\nMsg" + e.Message);
                UnregisterTimeOut();
                OnFailed(ErrorCode.NoResponse);
            }
        }

        private void OnResponseCallback(IAsyncResult ar)
        {
            try
            {
                UnregisterTimeOut();
                lock(m_Lock)
                {
                    var req = ar.AsyncState as HttpWebRequest;
                    if (null == req) return;
                    var response = req.BatterEndGetResponse(ar) as HttpWebResponse;
                    if(response.StatusCode == HttpStatusCode.OK)
                    {
                        length = response.ContentLength;
                        m_Content.webResponse = response;
                        OnBeginRead(new AsyncCallback(OnReadCallback));
                    }
                    else if(response.StatusCode == HttpStatusCode.NotModified)
                    {
                        if(m_Request != null)
                        {
                            m_Request.Abort();
                            m_Request = null;
                        }
                        OnPartialDownload();//断点续传
                        return;
                    }
                    else
                    {
                        response.Close();
                        OnFailed(ErrorCode.NoResponse);
                        return;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + localName + "\"download failed" + "\nMsg:" + e.Message);
                OnFailed(ErrorCode.DownloadError);
            }
        }

        void OnPartialDownload()
        {
            try
            {
                lock(m_Lock)
                {
                    m_Request = WebRequest.Create(url + localName) as HttpWebRequest;
                    m_Request.Timeout = TIMEOUT_TIME;
                    m_Request.KeepAlive = false;
                    m_Request.AddRange((int)m_Content.lastTimeCompletedLength);
                    var result = (IAsyncResult)m_Request.BeginGetResponse(new AsyncCallback(OnDownloadPartialResponseCallback), m_Request);
                    RegisterTimeOut(result.AsyncWaitHandle);
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + localName + "\"download failed" + "\nMsg:" + e.Message);
                UnregisterTimeOut();
                OnFailed(ErrorCode.NoResponse);
            }
        }

        private void OnDownloadPartialResponseCallback(IAsyncResult ar)
        {
            try
            {
                UnregisterTimeOut();

                lock(m_Lock)
                {
                    var req = ar.AsyncState as HttpWebRequest;
                    if (null == req) return;
                    var response = req.BatterEndGetResponse(ar) as HttpWebResponse;
                    if(response.StatusCode == HttpStatusCode.PartialContent)
                    {
                        length = m_Content.lastTimeCompletedLength + response.ContentLength;
                        m_Content.webResponse = response;
                        OnBeginRead(new AsyncCallback(OnReadCallback));
                    }
                    else if(response.StatusCode == HttpStatusCode.NotModified)
                    {
                        OnFailed(ErrorCode.Abort);
                        return;
                    }
                    else
                    {
                        response.Close();
                        OnFailed(ErrorCode.NoResponse);
                        return;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + localName + "\"download failed!" + "\nMsg:" + e.Message);
                OnFailed(ErrorCode.DownloadError);
            }
        }
        /// <summary>
        /// 开始读取
        /// </summary>
        public IAsyncResult OnBeginRead(AsyncCallback callback)
        {
            if (m_Content == null)
                return null;

            if(m_Content.state == DownloadContent.State.Canceling)
            {
                OnFailed(ErrorCode.Cancel);
                return null;
            }
            return m_Content.responseStream.BeginRead(m_Content.buffer, 0, DownloadContent.m_Buffer_SIZE, callback, m_Content);
        }

        /// <summary>
        /// 读取回调
        /// </summary>
        void OnReadCallback(IAsyncResult ar)
        {
            try
            {
                lock(m_Content)
                {
                    var rs = ar.AsyncState as DownloadContent;
                    if (rs.responseStream == null) return;
                    var read = rs.responseStream.EndRead(ar);
                    if(read > 0)
                    {
                        rs.fileStream.Write(rs.buffer, 0, read);
                        rs.fileStream.Flush();
                        completeLength += read;
                        m_NotifyCallback?.Invoke(this, (long)read);
                    }
                    else
                    {
                        OnFinish();
                        m_NotifyCallback?.Invoke(this, (long)read);
                        return;
                    }
                    OnBeginRead(new AsyncCallback(OnReadCallback));
                }
            }
            catch(WebException e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + localName + "\"download failed!" + "\nMsg:" + e.Message);
                OnFailed(ErrorCode.DownloadError);
            }
            catch(Exception e)
            {
                Debug.LogWarning("HttpAsyDownload - \"" + localName + "\"download failed!" + "\nMsg:" + e.Message);
                OnFailed(ErrorCode.DownloadError);
            }
        }
        #region TimeOut

        RegisteredWaitHandle m_RegisterWaitHandle;
        WaitHandle m_WaitHandle;
        private void RegisterTimeOut(WaitHandle handle)
        {
            m_WaitHandle = handle;
            m_RegisterWaitHandle = ThreadPool.RegisterWaitForSingleObject(handle, new WaitOrTimerCallback(OnTimeoutCallback), m_Request, TIMEOUT_TIME, true);
        }

        void UnregisterTimeOut()
        {
            if (m_RegisterWaitHandle != null && m_WaitHandle != null)
                m_RegisterWaitHandle.Unregister(m_WaitHandle);
        }

        void OnTimeoutCallback(object stat,bool timeout)
        {
            lock(m_Lock)
            {
                if(timeout)
                {
                    OnFailed(ErrorCode.TimeOut);
                }

                UnregisterTimeOut();
            }
        }
        #endregion
    }

}

