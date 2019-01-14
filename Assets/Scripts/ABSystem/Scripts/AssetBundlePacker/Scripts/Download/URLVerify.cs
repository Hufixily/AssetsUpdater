namespace game.Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.Networking;

    //URL验证
    public class URLVerify
    {
        public bool isDone { get; private set; }
        public string curURL { get; private set; }
        private List<string> m_UrlGroup;
        private Thread m_Thread;

        public URLVerify(List<string> list)
        {
            isDone = false;
            curURL = null;
            m_UrlGroup = list;
        }

        public void Start()
        {
            if (null == m_UrlGroup || m_UrlGroup.Count == 0)
            {
                curURL = null;
                isDone = false;
                return;
            }

            if(null == m_Thread)
            {
                m_Thread = new Thread(new ThreadStart(OnVerifyURLGroup));
                m_Thread.Start();
            }
        }

        public void Abort()
        {
            if(m_Thread != null)
            {
                m_Thread.Abort();
                m_Thread = null;
            }

            curURL = null;
            isDone = true;
        }

        /// <summary>
        /// 校验地址
        /// </summary>
        void OnVerifyURLGroup()
        {
            isDone = false;
            curURL = null;
            if(m_UrlGroup != null)
            {
                foreach(var item in m_UrlGroup)
                {
                    if(OnVerify(item))
                    {
                        curURL = item;
                        break;
                    }
                }
            }
            isDone = true;
        }

        static bool OnVerify(string url)
        {
            bool result = false;
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Method = "HEAD";
                request.Timeout = 5000;
                request.AllowAutoRedirect = false;
                request.UseDefaultCredentials = true;
                response = request.GetResponse() as HttpWebResponse;
                result = response.StatusCode == HttpStatusCode.OK;
            }
            catch(System.Net.WebException e)
            {
                result = false;
            }
            catch(System.Exception e)
            {
                result = false;
            }
            finally
            {
                if(response != null)
                {
                    response.Close();
                    response = null;
                }
                if(request != null)
                {
                    request.Abort();
                    request = null;
                }
            }
            return result;
        }
        
    }
}
