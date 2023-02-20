using System;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Net;

namespace Asgard
{
    public class ABResWWW
    {
        private static bool ifActivite = true;
        private static bool ifRun = true;
        public Thread downLoadThread;

        public enum DownLoadState
        {
            downLoading,
            finish,
        }
        private bool ifNeedWriteFile = true;
        public Byte[] bytes;
        private const int BUFF_SIZE = 0xffff;
        public string destPath = "";
        private DownLoadState downLoadState = DownLoadState.finish;

        private string url = "";
        public string error = null;
        public ABResWWW(string url, string destPath, bool ifNeedWriteFile = true)
        {
            this.url = url;
            this.destPath = destPath;
            this.ifNeedWriteFile = ifNeedWriteFile;
            this.error = null;
            this.isDone = false;
            this.bytes = null;
            downLoadThread = new Thread(this.StartDownLoad);
            downLoadThread.Start();

            //this.StartDownLoad();
            this.downLoadState = DownLoadState.downLoading;
        }

        public void Dispose()
        {
            this.url = "";
            this.destPath = "";
            this.error = null;
            this.isDone = false;
            this.ifNeedWriteFile = true;
            this.bytes = null;
            if (this.downLoadThread != null)
            {
                downLoadThread.Abort();
                downLoadThread = null;
            }
        }

        public bool isDone = false;

        private void StartDownLoad()
        {
#if UNITY_EDITOR && !USECDN
            StartLocalDownLoad(this.url, this.destPath);
#else
            StartHttpDownLoad(this.url, this.destPath);
#endif
            this.downLoadState = DownLoadState.finish;
        }

        private string StartLocalDownLoad(string url, string destPath)
        {
            try
            {
                FileStream inStream = new FileStream(url, FileMode.Open);

                if (ifNeedWriteFile)
                {
                    string tempPath = destPath + ".temp";
                    string dic = destPath.Substring(0, destPath.LastIndexOf("/"));
                    if (!Directory.Exists(dic)) { Directory.CreateDirectory(dic); };
                    if (File.Exists(destPath)) { File.Delete(destPath); }
                    FileStream outStream = new FileStream(tempPath, FileMode.Create);

                    byte[] buff = new byte[BUFF_SIZE];
                    int len = 0;
                    long totals = Math.Max(1, inStream.Length);
                    long pos = 0;
                    while ((len = inStream.Read(buff, 0, BUFF_SIZE)) != 0)
                    {
                        outStream.Write(buff, 0, len);
                        pos += len;
                    }
                    inStream.Close();
                    outStream.Close();
                    inStream.Dispose();
                    outStream.Dispose();
                    if (pos != totals)
                    {
                        this.isDone = true;
                        this.error = "下载文件长度不对报错!!" + this.destPath;
                        return this.error; ;
                    }
                    File.Move(tempPath, destPath);
                }
                else
                {
                    bytes = new byte[inStream.Length];
                    byte[] buff = new byte[BUFF_SIZE];
                    int len = 0;
                    long totals = Math.Max(1, inStream.Length);
                    long pos = 0;
                    while ((len = inStream.Read(buff, 0, BUFF_SIZE)) != 0)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            bytes[pos++] = buff[i];
                        }
                    }

                    inStream.Close();
                    inStream.Dispose();
                    if (pos != totals)
                    {
                        this.isDone = true;
                        this.error = "下载文件长度不对报错!!" + this.destPath;
                        return this.error; ;
                    }
                    else
                    {
                        Debug.Log("读取字节成功");
                    }

                    //测试写文件
                    //if (File.Exists(destPath)) File.Delete(destPath);
                    //File.WriteAllBytes(destPath, bytes);
                    //Console.WriteLine("写入文件成功！！");
                }
            }
            catch (Exception e)
            {
                //Debug.LogError(e.ToString());
                this.isDone = true;
                this.error = e.ToString();
                return this.error;
            }

            this.isDone = true;
            this.error = null;
            return this.error;
        }

        private string StartHttpDownLoad(string url, string destPath)
        {
            try
            {
                url += string.Format("?t={0}", System.DateTime.Now.Ticks);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                //request.ReadWriteTimeout = 10000;
                //request.Timeout = 6000;
                WebResponse response = request.GetResponse();
                Stream inStream = response.GetResponseStream();

                if (ifNeedWriteFile)
                {
                    string tempPath = destPath + ".temp";
                    string dic = destPath.Substring(0, destPath.LastIndexOf("/"));
                    if (!Directory.Exists(dic)) { Directory.CreateDirectory(dic); };
                    if (File.Exists(destPath)) { File.Delete(destPath); }
                    FileStream outStream = new FileStream(tempPath, FileMode.Create);

                    byte[] buff = new byte[BUFF_SIZE];
                    int len = 0;
                    long totals = Math.Max(1, response.ContentLength);
                    long pos = 0;
                    while ((len = inStream.Read(buff, 0, BUFF_SIZE)) != 0)
                    {
                        outStream.Write(buff, 0, len);
                        pos += len;
                    }
                    inStream.Close();
                    outStream.Close();
                    inStream.Dispose();
                    outStream.Dispose();
                    response.Close();
                    if (pos != totals)
                    {
                        this.isDone = true;
                        this.error = "下载文件长度不对报错!!" + this.destPath;
                        return this.error; ;
                    }
                    File.Move(tempPath, destPath);
                }
                else
                {
                    bytes = new byte[response.ContentLength];
                    byte[] buff = new byte[BUFF_SIZE];
                    int len = 0;
                    long totals = Math.Max(1, response.ContentLength);
                    long pos = 0;
                    while ((len = inStream.Read(buff, 0, BUFF_SIZE)) != 0)
                    {
                        for (int i = 0; i < len; i++)
                        {
                            bytes[pos++] = buff[i];
                        }
                    }

                    inStream.Close();
                    inStream.Dispose();
                    response.Close();
                    if (pos != totals)
                    {
                        this.isDone = true;
                        this.error = "下载文件长度不对报错!!" + this.destPath;
                        return this.error; ;
                    }
                    else
                    {
                        Debug.Log("读取字节成功");
                    }

                    //测试写文件
                    //if (File.Exists(destPath)) File.Delete(destPath);
                    //File.WriteAllBytes(destPath, bytes);
                    //Console.WriteLine("写入文件成功！！");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                this.isDone = true;
                this.error = e.ToString();
                return this.error;
            }

            this.isDone = true;
            this.error = null;
            return this.error;
        }


    }



}
