namespace game.Assets
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Threading;
    using System.Net;
    using System.IO;

    internal class DownloadContent
    {
        public enum State
        {
            Downloading,//正在下载
            Canceling,//正在取消
            Completed,//已完成
            Failed,//已失败
        }
        /// <summary>
        /// 下载文件缓存的Last-Modified字符串大小
        /// </summary>
        public const int FILE_LAST_MODIFIED_SIZE = 32;
        /// <summary>
        /// 缓存大小
        /// </summary>
        public const int m_Buffer_SIZE = 1024;
        /// <summary>
        /// 下载文件中间名
        /// </summary>
        public const string TEMP_EXTENSION_NAME = ".download";
        /// <summary>
        /// 当前状态
        /// </summary>
        public State state { get; private set; }
        /// <summary>
        /// 文件名
        /// </summary>
        public string fileFullName { get; private set; }
        /// <summary>
        /// 上次已下载大小
        /// </summary>
        public long lastTimeCompletedLength { get; private set; }
        /// <summary>
        /// 数据缓存
        /// </summary>
        public byte[] buffer { get; private set; }
        /// <summary>
        /// 最后一次通信时间戳
        /// </summary>
        public DateTime lastModified;

        public FileStream fileStream;
        /// <summary>
        /// 返回的数据流
        /// </summary>
        public Stream responseStream { get; private set; }
        /// <summary>
        /// Http接收器
        /// </summary>
        private HttpWebResponse m_Response;
        public HttpWebResponse webResponse
        {
            get { return m_Response; }
            set
            {
                m_Response = value;
                responseStream = m_Response.GetResponseStream();
            }
        }
        /// <summary>
        /// 临时文件名字
        /// </summary>
        public string tempFileFullName { get { return fileFullName + TEMP_EXTENSION_NAME; } }


        public DownloadContent(string fileName, bool newFile = true)
        {
            fileFullName = fileName;
            state = State.Downloading;
            buffer = new byte[m_Buffer_SIZE];
            OnOpenFile(newFile);
        }
        /// <summary>
        /// 关闭读取
        /// </summary>
        public void Close()
        {
            if (m_Response != null)
                OnCloseFile(m_Response.LastModified);
            else
                OnCloseFile();

            if (responseStream != null)
            {
                responseStream.Close();
                responseStream = null;
            }

            if (m_Response != null)
            {
                m_Response.Close();
                m_Response = null;
            }
        }

        public void Cancel()
        {
            state = State.Canceling;
        }

        public void Complete()
        {
            state = State.Completed;
        }

        public void Failed()
        {
            state = State.Failed;
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="newFile"></param>
        public void OnOpenFile(bool newFile)
        {
            try
            {
                //创建保存文件路径
                string parent = Path.GetDirectoryName(fileFullName);
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                //写入到临时文件
                if (newFile || !File.Exists(tempFileFullName))
                {
                    fileStream = new FileStream(tempFileFullName, FileMode.Create, FileAccess.ReadWrite);
                    lastTimeCompletedLength = 0;
                    lastModified = DateTime.MinValue;
                }
                else
                {
                    //断点续传
                    fileStream = new FileStream(tempFileFullName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    lastTimeCompletedLength = fileStream.Length;
                    if (lastTimeCompletedLength > FILE_LAST_MODIFIED_SIZE && OnReadLastModified(ref lastModified))
                    {
                        fileStream.Seek(lastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE, SeekOrigin.Begin);
                        lastTimeCompletedLength -= FILE_LAST_MODIFIED_SIZE;
                    }
                    else
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        lastTimeCompletedLength = 0;
                        lastModified = DateTime.MinValue;
                    }
                }
                return;
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("[Assets] File Load Error : {0}", fileFullName));
            }

            if (fileStream != null)
            {
                fileStream.Close();
                fileStream = null;
            }
        }
        /// <summary>
        /// 关闭文件
        /// </summary>
        public void OnCloseFile()
        {
            if (null != fileStream)
            {
                fileStream.Close();
                fileStream = null;
            }

            if (File.Exists(tempFileFullName))
            {
                if (state == State.Completed)//如果下载完成则修改文件名
                {
                    if (File.Exists(tempFileFullName))
                        File.Delete(tempFileFullName);
                    File.Move(tempFileFullName, fileFullName);
                }
                else
                {
                    File.Delete(tempFileFullName);
                }
            }
        }
        /// <summary>
        /// 关闭文件写入Modified
        /// </summary>
        void OnCloseFile(DateTime lastModified)
        {
            if (state == State.Failed)
                OnWriteLastModified(lastModified);

            if (null != fileStream)
            {
                fileStream.Close();
                fileStream = null;
            }

            if (File.Exists(tempFileFullName))
            {
                if (state == State.Completed)//如果下载完成则修改文件名
                {
                    if (File.Exists(fileFullName))
                        File.Delete(fileFullName);
                    File.Move(tempFileFullName, fileFullName);
                }
            }
        }

        bool OnWriteLastModified(DateTime lastModified)
        {
            if (null != fileStream)
            {
                var str = lastModified.Ticks.ToString("d" + FILE_LAST_MODIFIED_SIZE);
                var bytes = System.Text.Encoding.UTF8.GetBytes(str);
                fileStream.Write(bytes, 0, bytes.Length);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 读取Modified
        /// </summary>
        bool OnReadLastModified(ref DateTime lastModified)
        {
            if (fileStream != null && fileStream.Length > FILE_LAST_MODIFIED_SIZE)
            {
                var bytes = new byte[FILE_LAST_MODIFIED_SIZE];
                fileStream.Seek(lastTimeCompletedLength - FILE_LAST_MODIFIED_SIZE, SeekOrigin.Begin);
                fileStream.Read(bytes, 0, FILE_LAST_MODIFIED_SIZE);
                var ticks = long.Parse(System.Text.Encoding.Default.GetString(bytes));
                lastModified = new DateTime(ticks);
                return true;
            }
            return false;
        }
    }

}

