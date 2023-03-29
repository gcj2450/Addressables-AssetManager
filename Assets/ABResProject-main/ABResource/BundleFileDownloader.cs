using System;
using System.Net;
using System.Threading;
using System.IO;
using UnityEngine;
using System.Security.Cryptography;

namespace ZionGame
{
    public class BundleFileDownloader
    {
        private event Action<Exception> OnError;

        private SynchronizationContext mainThreadSyncContext;
        private int threadCount = 1;
        private static object errorlock = new object();
        private bool isDownloading = false;

        public BundleFileDownloader(int _threadCount = 1)
        {
            if (_threadCount > 8)
            {
                //限制不超过8个
                _threadCount = 8;
            }
            mainThreadSyncContext = SynchronizationContext.Current;
            //Http协议的并发连接数限制
            System.Net.ServicePointManager.DefaultConnectionLimit = 512;
            threadCount = _threadCount;
        }

        /// <summary>
        /// 多线程下载文件至本地
        /// </summary>
        /// <param name="filePath">保存文件路径</param>
        /// <param name="onDownloading">下载过程回调（已下载文件大小、总文件大小）</param>
        /// <param name="onComplete">下载完毕回调（下载文件数据）</param>
        public void DownloadToFilePath(string _url, string _filePath, Action<long, long> _onDownloading = null, Action<string, string> _onComplete = null, Action<Exception> _onError = null)
        {
            if (File.Exists(_filePath))
            {
                Debug.Log("file already exists");
                if (_onComplete != null)
                    _onComplete(_url, _filePath);
                return;
            }
            isDownloading = true;
            OnError = _onError;
            long csize = 0; //已下载大小
            int ocnt = 0;   //完成线程数


            // 下载逻辑
            GetFileSizeAsyn(_url, (size) =>
            {
                if (size == -1) return;
                // 准备工作
                var tempFilePaths = new string[threadCount];
                var tempFileFileStreams = new FileStream[threadCount];
                var dirPath = Path.GetDirectoryName(_filePath);
                var fileName = Path.GetFileName(_filePath);
                // 下载根目录不存在则创建
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                // 查看下载临时文件是否可以继续断点续传
                var fileInfos = new DirectoryInfo(dirPath).GetFiles(fileName + "*.temp");
                if (fileInfos.Length != threadCount)
                {
                    // 下载临时文件数量不相同，则清理
                    foreach (var info in fileInfos)
                    {
                        info.Delete();
                    }
                }
                // 创建下载临时文件，并创建文件流
                for (int i = 0; i < threadCount; i++)
                {
                    tempFilePaths[i] = _filePath + i + ".temp";
                    if (!File.Exists(tempFilePaths[i]))
                    {
                        File.Create(tempFilePaths[i]).Dispose();
                    }
                    tempFileFileStreams[i] = File.OpenWrite(tempFilePaths[i]);
                    tempFileFileStreams[i].Seek(tempFileFileStreams[i].Length, System.IO.SeekOrigin.Current);

                    csize += tempFileFileStreams[i].Length;
                }
                // 单线程下载过程回调函数
                Action<int, long, byte[], byte[]> t_onDownloading = (index, rsize, rbytes, data) =>
                {
                    csize += rsize;
                    tempFileFileStreams[index].Write(rbytes, 0, (int)rsize);
                    PostMainThreadAction<long, long>(_onDownloading, csize, size);
                };
                // 单线程下载完毕回调函数
                Action<int, byte[]> t_onTrigger = (index, data) =>
                {
                    // 关闭文件流
                    tempFileFileStreams[index].Close();
                    ocnt++;
                    if (ocnt >= threadCount)
                    {
                        // 将临时文件转为下载文件
                        if (!File.Exists(_filePath))
                        {
                            File.Create(_filePath).Dispose();
                        }
                        else
                        {
                            File.WriteAllBytes(_filePath, new byte[] { });
                        }
                        FileStream fs = File.OpenWrite(_filePath);
                        fs.Seek(fs.Length, System.IO.SeekOrigin.Current);
                        foreach (var tempPath in tempFilePaths)
                        {
                            var tempData = File.ReadAllBytes(tempPath);
                            fs.Write(tempData, 0, tempData.Length);
                            File.Delete(tempPath);
                        }
                        fs.Close();
                        PostMainThreadAction<string, string>(_onComplete, _url, _filePath);
                    }
                };
                // 分割文件尺寸，多线程下载
                long[] sizes = SplitFileSize(size, threadCount);
                for (int i = 0; i < sizes.Length; i = i + 2)
                {
                    long from = sizes[i];
                    long to = sizes[i + 1];

                    // 断点续传
                    from += tempFileFileStreams[i / 2].Length;
                    if (from >= to)
                    {
                        t_onTrigger(i / 2, null);
                        continue;
                    }

                    _threadDownload(_url, i / 2, from, to, t_onDownloading, t_onTrigger);
                }
            });
        }

        public void DownloadToFileBytes(string _url, string _filePath, Action<long, long> _onDownloading = null, Action<string, byte[]> _onComplete = null, Action<Exception> _onError = null)
        {
            if (File.Exists(_filePath))
            {
                Debug.Log("file already exists");
                if (_onComplete != null)
                    _onComplete(_url, File.ReadAllBytes(_filePath));
                return;
            }
            isDownloading = true;
            OnError = _onError;
            long csize = 0; //已下载大小
            int ocnt = 0;   //完成线程数


            // 下载逻辑
            GetFileSizeAsyn(_url, (size) =>
            {
                if (size == -1) return;
                // 准备工作
                var tempFilePaths = new string[threadCount];
                var tempFileFileStreams = new FileStream[threadCount];
                var dirPath = Path.GetDirectoryName(_filePath);
                var fileName = Path.GetFileName(_filePath);
                // 下载根目录不存在则创建
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                // 查看下载临时文件是否可以继续断点续传
                var fileInfos = new DirectoryInfo(dirPath).GetFiles(fileName + "*.temp");
                if (fileInfos.Length != threadCount)
                {
                    // 下载临时文件数量不相同，则清理
                    foreach (var info in fileInfos)
                    {
                        info.Delete();
                    }
                }
                // 创建下载临时文件，并创建文件流
                for (int i = 0; i < threadCount; i++)
                {
                    tempFilePaths[i] = _filePath + i + ".temp";
                    if (!File.Exists(tempFilePaths[i]))
                    {
                        File.Create(tempFilePaths[i]).Dispose();
                    }
                    tempFileFileStreams[i] = File.OpenWrite(tempFilePaths[i]);
                    tempFileFileStreams[i].Seek(tempFileFileStreams[i].Length, System.IO.SeekOrigin.Current);

                    csize += tempFileFileStreams[i].Length;
                }
                // 单线程下载过程回调函数
                Action<int, long, byte[], byte[]> t_onDownloading = (index, rsize, rbytes, data) =>
                {
                    csize += rsize;
                    tempFileFileStreams[index].Write(rbytes, 0, (int)rsize);
                    PostMainThreadAction<long, long>(_onDownloading, csize, size);
                };
                // 单线程下载完毕回调函数
                Action<int, byte[]> t_onTrigger = (index, data) =>
                {
                    // 关闭文件流
                    tempFileFileStreams[index].Close();
                    ocnt++;
                    if (ocnt >= threadCount)
                    {
                        // 将临时文件转为下载文件
                        if (!File.Exists(_filePath))
                        {
                            File.Create(_filePath).Dispose();
                        }
                        else
                        {
                            File.WriteAllBytes(_filePath, new byte[] { });
                        }
                        FileStream fs = File.OpenWrite(_filePath);
                        fs.Seek(fs.Length, System.IO.SeekOrigin.Current);
                        foreach (var tempPath in tempFilePaths)
                        {
                            var tempData = File.ReadAllBytes(tempPath);
                            fs.Write(tempData, 0, tempData.Length);
                            File.Delete(tempPath);
                        }
                        fs.Close();
                        PostMainThreadAction<string, byte[]>(_onComplete, _url, File.ReadAllBytes(_filePath));
                    }
                };
                // 分割文件尺寸，多线程下载
                long[] sizes = SplitFileSize(size, threadCount);
                for (int i = 0; i < sizes.Length; i = i + 2)
                {
                    long from = sizes[i];
                    long to = sizes[i + 1];

                    // 断点续传
                    from += tempFileFileStreams[i / 2].Length;
                    if (from >= to)
                    {
                        t_onTrigger(i / 2, null);
                        continue;
                    }

                    _threadDownload(_url, i / 2, from, to, t_onDownloading, t_onTrigger);
                }
            });
        }

        /// <summary>
        /// 多线程下载文件至内存
        /// </summary>
        /// <param name="onDownloading">下载过程回调（已下载文件大小、总文件大小）</param>
        /// <param name="onTrigger">下载完毕回调（下载文件数据）</param>
        public void DownloadToMemory(string _url, Action<long, long> _onDownloading = null, Action<string, byte[]> _onTrigger = null, Action<Exception> _onError = null)
        {
            isDownloading = true;
            OnError = _onError;
            long csize = 0; // 已下载大小
            int ocnt = 0;   // 完成线程数
            byte[] cdata;  // 已下载数据
                           // 下载逻辑
            GetFileSizeAsyn(_url, (size) =>
            {
                cdata = new byte[size];
                // 单线程下载过程回调函数
                Action<int, long, byte[], byte[]> t_onDownloading = (index, rsize, rbytes, data) =>
                {
                    csize += rsize;
                    PostMainThreadAction<long, long>(_onDownloading, csize, size);
                };
                // 单线程下载完毕回调函数
                Action<int, byte[]> t_onTrigger = (index, data) =>
                {
                    long dIndex = (long)Math.Ceiling((double)(size * index / threadCount));
                    Array.Copy(data, 0, cdata, dIndex, data.Length);

                    ocnt++;
                    if (ocnt >= threadCount)
                    {
                        PostMainThreadAction<string, byte[]>(_onTrigger, _url, cdata);
                    }
                };
                // 分割文件尺寸，多线程下载
                long[] sizes = SplitFileSize(size, threadCount);
                for (int i = 0; i < sizes.Length; i = i + 2)
                {
                    long from = sizes[i];
                    long to = sizes[i + 1];
                    _threadDownload(_url, i / 2, from, to, t_onDownloading, t_onTrigger);
                }
            });
        }

        /// <summary>
        /// 单线程下载
        /// </summary>
        /// <param name="index">线程ID</param>
        /// <param name="from">下载起始位置</param>
        /// <param name="to">下载结束位置</param>
        /// <param name="onDownloading">下载过程回调（线程ID、单次下载数据大小、单次下载数据缓存区、已下载文件数据）</param>
        /// <param name="onTrigger">下载完毕回调（线程ID、下载文件数据）</param>
        private void _threadDownload(string _url, int _threadIndex, long _from, long _to, Action<int, long, byte[], byte[]> _onDownloading = null, Action<int, byte[]> _onTrigger = null)
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    var request = (HttpWebRequest)HttpWebRequest.Create(new Uri(_url));
                    request.AddRange(_from, _to);

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream ns = response.GetResponseStream();

                    byte[] rbytes = new byte[8 * 1024];
                    int rSize = 0;
                    MemoryStream ms = new MemoryStream();
                    while (true)
                    {
                        if (!isDownloading) return;
                        rSize = ns.Read(rbytes, 0, rbytes.Length);
                        if (rSize <= 0) break;
                        ms.Write(rbytes, 0, rSize);
                        if (_onDownloading != null) _onDownloading(_threadIndex, rSize, rbytes, ms.ToArray());
                    }

                    ns.Close();
                    response.Close();
                    request.Abort();

                    if (ms.Length == (_to - _from) + 1)
                    {
                        if (_onTrigger != null) _onTrigger(_threadIndex, ms.ToArray());
                    }
                    else
                    {
                        lock (errorlock)
                        {
                            if (isDownloading)
                            {
                                onError(new Exception("文件大小校验不通过"));
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    onError(ex);
                }


            }));
            thread.Start();
        }

        /// <summary>
        /// 查询文件大小
        /// </summary>
        /// <returns></returns>
        long GetFileSize(string _url)
        {
            HttpWebRequest request;
            HttpWebResponse response;
            try
            {
                request = (HttpWebRequest)HttpWebRequest.CreateHttp(new Uri(_url));
                request.Method = "HEAD";
                response = (HttpWebResponse)request.GetResponse();
                // 获得文件长度
                long contentLength = response.ContentLength;

                response.Close();
                request.Abort();

                return contentLength;
            }
            catch (Exception ex)
            {
                onError(ex);
                // throw;
                return -1;
            }
        }

        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="onTrigger"></param>
        void GetFileSizeAsyn(string _url, Action<long> _onTrigger = null)
        {
            ThreadStart threadStart = new ThreadStart(() =>
            {
                PostMainThreadAction<long>(_onTrigger, GetFileSize(_url));
            });
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        public void Close()
        {
            isDownloading = false;
        }

        /// <summary>
        /// 分割文件
        /// </summary>
        /// <returns></returns>
        private long[] SplitFileSize(long _size, int _count)
        {
            long[] result = new long[_count * 2];
            for (int i = 0; i < _count; i++)
            {
                long from = (long)Math.Ceiling((double)(_size * i / _count));
                long to = (long)Math.Ceiling((double)(_size * (i + 1) / _count)) - 1;
                result[i * 2] = from;
                result[i * 2 + 1] = to;
            }

            return result;
        }

        private void onError(Exception _ex)
        {
            Close();
            PostMainThreadAction<Exception>(OnError, _ex);
        }

        //=======================================
        #region SimpleDownloader

        static string PERSIST_EXP = ".cdel";

        /// <summary>
        /// 单线程下载，不回调OnError
        /// </summary>
        /// <param name="_savePath">保存地址</param>
        /// <param name="_onProgreeUpdate">进度更新</param>
        /// <param name="_onComplete">下载完成可能是失败</param>
        public void SingleThreadDownload(string _url, string _savePath, Action<long, long> _onDownloading, Action<string, string> _onComplete, Action<Exception> _onError = null)
        {
            if (File.Exists(_savePath))
            {
                Debug.Log("file already exists");
                if (_onComplete != null)
                    _onComplete(_url, _savePath);
                return;
            }

            isDownloading = true;
            long csize = 0;
            long tSize = 0;
            OnError = _onError;
            Action<long, long> t_onDownloading = (rsize, totalSize) =>
            {
                csize = rsize;
                tSize = totalSize;
                PostMainThreadAction<long, long>(_onDownloading, csize, tSize);
            };

            string rstatus = _url;
            string resultstr = _savePath;
            Action<string, string> t_onComplete = (_rstatus, _errorStr) =>
            {
                rstatus = _rstatus;
                resultstr = _errorStr;
                PostMainThreadAction<string, string>(_onComplete, rstatus, resultstr);
            };
            simpleDownload(_url, _savePath, t_onDownloading, t_onComplete);
        }

        void simpleDownload(string _url, string _savePath, Action<long, long> _onDownloading, Action<string, string> _onComplete)
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                if (File.Exists(_savePath))
                {
                    if (_onComplete != null)
                    {
                        _onComplete(_url, _savePath);
                    }
                }
                else
                {
                    _savePath = _savePath + PERSIST_EXP;
                    WebResponse response = null;
                    FileStream writer = null;
                    try
                    {
                        writer = new FileStream(_savePath, FileMode.OpenOrCreate, FileAccess.Write);

                        long lStartPos = writer.Length; ;//当前文件大小
                        long currentLength = 0;
                        long totalLength = 0;//总大小

                        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(_url);
                        request.Method = "HEAD";
                        response = (HttpWebResponse)request.GetResponse();
                        //文件找不到 404 这里会空
                        if (response == null)
                        {
                            Debug.Log("response == null");
                            if (writer != null)
                            {
                                writer.Close();
                                writer.Dispose();
                            }
                            onError(new Exception("remote file not found"));
                            return;
                        }

                        if (response != null)
                        {
                            long sTotal = response.ContentLength;
                            if (sTotal == lStartPos)
                            {
                                if (writer != null)
                                {
                                    writer.Close();
                                    writer.Dispose();
                                }
                                string realPath = _savePath.Replace(PERSIST_EXP, "");
                                File.Move(_savePath, realPath);
                                Debug.Log("下载完成!");

                                if (_onComplete != null)
                                {
                                    _onComplete(_url, realPath);
                                }

                                return;
                            }
                            if (lStartPos > sTotal)
                            {
                                if (writer != null)
                                {
                                    writer.Close();
                                    writer.Dispose();
                                }
                                //起始长度比总长还长，删除重下
                                Debug.Log("file length is illegal delete and redownload");
                                File.Delete(_savePath);
                                simpleDownload(_url, _savePath, _onDownloading, _onComplete);
                                return;
                            }
                            request = getWebRequest(_url, (int)lStartPos);
                            writer.Seek(lStartPos, SeekOrigin.Begin);
                            response = request.GetResponse(); //这要重新get 一次response，上一次只有head
                            totalLength = response.ContentLength + lStartPos; //
                            currentLength = lStartPos; //
                        }

                        if (response == null)
                        {
                            Debug.Log("response = =null ");
                            if (writer != null)
                            {
                                writer.Close();
                                writer.Dispose();
                            }
                            onError(new Exception("response = =null "));
                            return;
                        }
                        Stream reader = response.GetResponseStream();
                        byte[] buff = new byte[1024];
                        int c = 0; //实际读取的字节数
                        while ((c = reader.Read(buff, 0, buff.Length)) > 0)
                        {
                            currentLength += c;
                            writer.Write(buff, 0, c);
                            //float curL = currentLength;
                            //float totalL = totalLength;
                            //float progressPercent = (float)(curL / totalL);
                            if (_onDownloading != null)
                            {
                                _onDownloading(currentLength, totalLength);
                            }
                            writer.Flush();
                        }
                        if (writer != null)
                        {
                            writer.Close();
                            writer.Dispose();
                        }
                        //close(writer);
                        if (currentLength == totalLength)
                        {
                            string realPath = _savePath.Replace(PERSIST_EXP, "");
                            File.Move(_savePath, realPath);
                            if (_onComplete != null)
                            {
                                _onComplete(_url, realPath);
                            }
                            Debug.Log("下载完成!");
                        }


                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                            response.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("writer = new FileStream error");
                        if (writer != null)
                        {
                            writer.Close();
                            writer.Dispose();
                        }
                        onError(ex);
                        return;
                    }
                }
            }));
            thread.Start();
        }

        HttpWebRequest getWebRequest(string _url, int _iStartPos)
        {
            HttpWebRequest request = null;
            try
            {
                request = (System.Net.HttpWebRequest)HttpWebRequest.Create(_url);
                request.AddRange(_iStartPos); //设置Range值
            }
            catch (Exception ex)
            {
                Debug.Log("create request error: " + ex.Message);
                onError(ex);
            }

            return request;
        }

        static string GetFileMD5(string filepath)
        {
            var filestream = new FileStream(filepath, System.IO.FileMode.Open);
            if (filestream == null)
            {
                string V = "";
                return V;
            }
            MD5 md5 = MD5.Create();
            var fileMD5Bytes = md5.ComputeHash(filestream);
            filestream.Close();
            string filemd5 = System.BitConverter.ToString(fileMD5Bytes).Replace("-", "").ToLower();
            return filemd5;
        }

        #endregion
        //=======================================
        //=======================================
        #region PostToMainThread

        /// <summary>
        /// 通知主线程回调
        /// </summary>
        private void PostMainThreadAction(Action action)
        {
            mainThreadSyncContext.Post(new SendOrPostCallback((o) =>
            {
                Action e = (Action)o.GetType().GetProperty("action").GetValue(o);
                if (e != null) e();
            }), new { action = action });
        }
        private void PostMainThreadAction<T>(Action<T> action, T arg1)
        {
            mainThreadSyncContext.Post(new SendOrPostCallback((o) =>
            {
                Action<T> e = (Action<T>)o.GetType().GetProperty("action").GetValue(o);
                T t1 = (T)o.GetType().GetProperty("arg1").GetValue(o);
                if (e != null) e(t1);
            }), new { action = action, arg1 = arg1 });
        }
        public void PostMainThreadAction<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            mainThreadSyncContext.Post(new SendOrPostCallback((o) =>
            {
                Action<T1, T2> e = (Action<T1, T2>)o.GetType().GetProperty("action").GetValue(o);
                T1 t1 = (T1)o.GetType().GetProperty("arg1").GetValue(o);
                T2 t2 = (T2)o.GetType().GetProperty("arg2").GetValue(o);
                if (e != null) e(t1, t2);
            }), new { action = action, arg1 = arg1, arg2 = arg2 });
        }
        public void PostMainThreadAction<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            mainThreadSyncContext.Post(new SendOrPostCallback((o) =>
            {
                Action<T1, T2, T3> e = (Action<T1, T2, T3>)o.GetType().GetProperty("action").GetValue(o);
                T1 t1 = (T1)o.GetType().GetProperty("arg1").GetValue(o);
                T2 t2 = (T2)o.GetType().GetProperty("arg2").GetValue(o);
                T3 t3 = (T3)o.GetType().GetProperty("arg3").GetValue(o);
                if (e != null) e(t1, t2, t3);
            }), new { action = action, arg1 = arg1, arg2 = arg2, arg3 = arg3 });
        }
        public void PostMainThreadAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            mainThreadSyncContext.Post(new SendOrPostCallback((o) =>
            {
                Action<T1, T2, T3, T4> e = (Action<T1, T2, T3, T4>)o.GetType().GetProperty("action").GetValue(o);
                T1 t1 = (T1)o.GetType().GetProperty("arg1").GetValue(o);
                T2 t2 = (T2)o.GetType().GetProperty("arg2").GetValue(o);
                T3 t3 = (T3)o.GetType().GetProperty("arg3").GetValue(o);
                T4 t4 = (T4)o.GetType().GetProperty("arg4").GetValue(o);
                if (e != null) e(t1, t2, t3, t4);
            }), new { action = action, arg1 = arg1, arg2 = arg2, arg3 = arg3, arg4 = arg4 });
        }
        #endregion
        //=======================================
    }
}