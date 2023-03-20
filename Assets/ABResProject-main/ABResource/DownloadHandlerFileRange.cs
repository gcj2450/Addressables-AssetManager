using UnityEngine.Networking;
using System.IO;
using System;

namespace Baidu.Meta.ComponentsTool.Runtime
{

    /// <summary>
    /// 使用方式:
    /// UnityWebRequest unityWebRequest = new UnityWebRequest("url");
    /// unityWebRequest.downloadHandler = new DownloadHandlerFileRange("文件保存的路径", unityWebRequest);
    /// unityWebRequest.SendWebRequest();
    /// </summary>
    public class DownloadHandlerFileRange : DownloadHandlerScript
    {
        /// <summary>
        /// 文件正式开始下载事件,此事件触发以后即可获取到文件的总大小
        /// </summary>
        public event System.Action StartDownloadEvent;
        public event System.Action<ulong> DownloadedSizeUpdateEvent;
        private string Path;//文件保存的路径
        private FileStream FileStream;
        private UnityWebRequest UnityWebRequest;
        private ulong LocalFileSize = 0;//本地已经下载的文件的大小
        private ulong TotalFileSize = 0;//文件的总大小
        private ulong CurFileSize = 0;//当前的文件大小
        private float LastTime = 0;//用作下载速度的时间统计
        private float LastDataSize = 0;//用来作为下载速度的大小统计
        private float DownloadSpeed = 0;//下载速度,单位:Byte/S

        /// <summary>
        /// 下载速度,单位:KB/S 保留两位小数
        /// </summary>
        public float Speed
        {
            get
            {
                return ((int)(DownloadSpeed / 1024 * 100)) / 100.0f;
            }
        }

        /// <summary>
        /// 文件的总大小
        /// </summary>
        public ulong FileSize
        {
            get
            {
                return TotalFileSize;
            }
        }

        /// <summary>
        /// 下载进度[0,1]
        /// </summary>
        public float DownloadProgress
        {
            get
            {
                return GetProgress();
            }
        }

        /// <summary>
        /// 使用1MB的缓存,在补丁2017.2.1p1中对DownloadHandlerScript的优化中,目前最大传入数据量也仅仅是1024*1024,再多也没用
        /// </summary>
        /// <param name="path">文件保存的路径</param>
        /// <param name="request">UnityWebRequest对象,用来获文件大小,设置断点续传的请求头信息</param>
        public DownloadHandlerFileRange(string path, UnityWebRequest request) : base(new byte[1024 * 1024])
        {
            Path = path;
            FileStream = new FileStream(Path, FileMode.Append, FileAccess.Write);
            UnityWebRequest = request;
            if (File.Exists(path))
            {
                LocalFileSize = (ulong)new System.IO.FileInfo(path).Length;
            }
            CurFileSize = LocalFileSize;
            UnityWebRequest.SetRequestHeader("Range", "bytes=" + LocalFileSize + "-");
        }

        /// <summary>
        /// 清理资源,该方法没办法重写,只能隐藏,如果想要强制中止下载,并清理资源(UnityWebRequest.Dispose()),该方法并不会被调用,这让人很苦恼
        /// </summary>
        new public void Dispose()
        {
            Clean();
        }

        /// <summary>
        /// 关闭文件流
        /// </summary>
        private void Clean()
        {
            DownloadSpeed = 0.0f;
            if (FileStream != null)
            {
                FileStream.Flush();
                FileStream.Dispose();
                FileStream = null;
            }
        }

        /// <summary>
        /// 下载完成后清理资源
        /// </summary>
        protected override void CompleteContent()
        {
            base.CompleteContent();
            Clean();
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadHandler.data属性时,将会调用该方法,用于以byte[]的方式返回下载的数据,目前总是返回null
        /// </summary>
        /// <returns></returns>
        protected override byte[] GetData()
        {
            return null;
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadProgress属性时,将会调用该方法,用于返回下载进度
        /// </summary>
        /// <returns></returns>
        protected override float GetProgress()
        {
            return TotalFileSize == 0 ? 0 : ((float)CurFileSize) / TotalFileSize;
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadHandler.text属性时,将会调用该方法,用于以string的方式返回下载的数据,目前总是返回null
        /// </summary>
        /// <returns></returns>
        protected override string GetText()
        {
            return null;
        }

        //Note:当下载的文件数据大于2G时,该int类型的参数将会数据溢出,所以先自己通过响应头来获取长度,获取不到再使用参数的方式
        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            string contentLengthStr = UnityWebRequest.GetResponseHeader("Content-Length");
            if (!string.IsNullOrEmpty(contentLengthStr))
            {
                try
                {
                    TotalFileSize = ulong.Parse(contentLengthStr);
                }
                catch (System.FormatException e)
                {
                    UnityEngine.Debug.Log("获取文件长度失败,contentLengthStr:" + contentLengthStr + "," + e.Message);
                    TotalFileSize = contentLength;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.Log("获取文件长度失败,contentLengthStr:" + contentLengthStr + "," + e.Message);
                    TotalFileSize = contentLength;
                }
            }
            else
            {
                TotalFileSize = contentLength;
            }
            //这里拿到的下载大小是待下载的文件大小,需要加上本地已下载文件的大小才等于总大小
            TotalFileSize += LocalFileSize;
            LastTime = UnityEngine.Time.time;
            LastDataSize = CurFileSize;
            if (StartDownloadEvent != null)
            {
                StartDownloadEvent();
            }
        }

        //在2017.3.0(包括该版本)以下的正式版本中存在一个性能上的问题
        //该回调方法有性能上的问题,每次传入的数据量最大不会超过65536(2^16)个字节,不论缓存区有多大
        //在下载速度中的体现,大约相当于每秒下载速度不会超过3.8MB/S
        //这个问题在 "补丁2017.2.1p1" 版本中被优化(2017.12.21发布)(https://unity3d.com/cn/unity/qa/patch-releases/2017.2.1p1)
        //(965165) - Web: UnityWebRequest: improve performance for DownloadHandlerScript.
        //优化后,每次传入数据量最大不会超过1048576(2^20)个字节(1MB),基本满足下载使用
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0 || UnityWebRequest.responseCode > 400)
            {
                return false;
            }
            FileStream.Write(data, 0, dataLength);
            CurFileSize += (ulong)dataLength;
            if (DownloadedSizeUpdateEvent != null)
                DownloadedSizeUpdateEvent(CurFileSize);
            //统计下载速度
            if (UnityEngine.Time.time - LastTime >= 1.0f)
            {
                DownloadSpeed = (CurFileSize - LastDataSize) / (UnityEngine.Time.time - LastTime);
                LastTime = UnityEngine.Time.time;
                LastDataSize = CurFileSize;
            }
            return true;
        }

        ~DownloadHandlerFileRange()
        {
            Clean();
        }


    }
}