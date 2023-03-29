using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using ZionGame;

namespace Baidu.Meta.ComponentsBundleLoader.Runtime
{
    /// <summary>
    /// 只下载，不加载Bundle文件
    /// </summary>
    public class ABLoadingStateBundleNetwork_New : ABILoadingState
    {
        private Dictionary<string, ABIData> configData = null;
        private ABBootstrap bootstrap = null;
        private ABManifest manifest = null;
        private string curBundleFullPath;
        private int curAssetIndex = 0;
        private float lastProgress = 0.0f;  // used when _request is null, which could be confusing whether it's starting or ending		
        private string localCacheFolder;

        public event EventHandler OnError;
        public Action OnEnd;
        public Action<float> OnProgressUpdate;
        float progress;
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="_localCacheFolder">本地缓存文件夹</param>
        /// <param name="eventHandler"></param>
        /// <param name="_onEnd">全部结束事件</param>
        public ABLoadingStateBundleNetwork_New(string _localCacheFolder, EventHandler _onError,Action<float>_onProgressUpdate,Action _onEnd)
        {
            OnProgressUpdate=_onProgressUpdate;
            localCacheFolder = _localCacheFolder;
            OnEnd = _onEnd;
            OnError = _onError;
        }

        public void Begin(Dictionary<string, ABIData> _configData)
        {
            this.configData = _configData;
            if (this.configData.ContainsKey("Bootstrap") == false)
                Debug.LogError("<color=#ff8080>Bootstrap is null.</color>");
            bootstrap = this.configData["Bootstrap"] as ABBootstrap;
            if (this.configData.ContainsKey("Manifest") == false)
                Debug.LogError("<color=#ff8080>Manifest is null.</color>");
            manifest = configData["Manifest"] as ABManifest;

            StartNextBundle(0);
        }

        public void End()
        {
            ////这里完成了所有的事情，开始下一步
            //ABAssetLoader.Initialize(configData);
            if (OnEnd!=null)
            {
                OnEnd();
            }
            configData = null;
            manifest = null;
            bootstrap = null;
            progress = 0;
        }

        public string GetStateText()
        {
            if (curAssetIndex >= manifest.assetBundles.Length)
                return "Bundles Loaded";
            return "Loading " + manifest.assetBundles[curAssetIndex].bundleName;
        }

        public bool IsDone()
        {
            return curAssetIndex >= manifest.assetBundles.Length ;
        }

        public float GetProgress()
        {
            if (writer != null && response != null)
            {
                // smooth interpolation across the whole set of files, assuming each one was the same size (which they aren't)
                float minValue = curAssetIndex / (float)manifest.assetBundles.Length;
                float maxValue = (curAssetIndex + 1) / (float)manifest.assetBundles.Length;
                lastProgress = Mathf.Lerp(minValue, maxValue, progress);
            }
            return Mathf.Clamp(lastProgress, 0, 1);
        }

        public void Retry()
        {
            Debug.Log("Retry: retry download...");
            progress = 0;
            retryCnt = 0;
            Debug.Log($"failed retry download,{manifest.assetBundles[curAssetIndex].filename}");

            string url = RemoveDoubleSlashes(
               bootstrap.cdnBundleUrl + "/" + manifest.assetBundles[curAssetIndex].filename);
            Debug.Log("retry Downloading bundle: " + url);
            LoomDownload(url, curBundleFullPath, OnBundleProgreeUpdate, OnBundleComplete);

            retryCnt++;
        }

        private void StartNextBundle(int index)
        {
            //Debug.Log(" manifest.assetBundles.Length: " + manifest.assetBundles.Length);
            curAssetIndex = index;
            if (curAssetIndex >= manifest.assetBundles.Length)
            {
                //全部下载完成了
                End();
                //if (_request != null)
                //    _request.Dispose();
                //_request = null;
                Debug.Log("curAssetIndex >= manifest.assetBundles.Length");
            }
            else
            {
                if (!localCacheFolder.EndsWith("/"))
                    localCacheFolder = localCacheFolder + "/";
                curBundleFullPath = localCacheFolder + manifest.assetBundles[curAssetIndex].filename;
                bool doDownload = true;

                if (File.Exists(curBundleFullPath))
                {
                    FileInfo fileInfo = new FileInfo(curBundleFullPath);
                    string fileMd5 = GetFileMD5(curBundleFullPath);
                    Debug.Log(fileInfo.Length + "__" + manifest.assetBundles[curAssetIndex].length + "___" +
                        fileMd5 + "__" + manifest.assetBundles[curAssetIndex].md5);
                    if (fileInfo.Length != manifest.assetBundles[curAssetIndex].length ||
                        fileMd5 != manifest.assetBundles[curAssetIndex].md5)
                    {
                        Debug.Log("File length is not same delete local file and redownload...");
                        doDownload = true;
                        File.Delete(curBundleFullPath);
                        //文件大小不一致，删除这个文件重新下载
                    }
                    else
                    {
                        Debug.Log("Find cached bundle and file size is same try read...");
                        Debug.Log("Reading cached bundle: " + manifest.assetBundles[curAssetIndex].bundleName);
                        try
                        {
                            // attempt to open the local file.  If it's valid, we use it.  If not, redownload it.
                            OnBundleComplete(true, "Already Exists");
                            doDownload = false;
                        }
                        catch (Exception)
                        {
                            doDownload = true;
                            File.Delete(curBundleFullPath);  // nuke the bad file and grab it again
                            Debug.Log("Retrying with download.");
                        }
                    }
                }
                else
                    Debug.Log("file not exist...");

                if (doDownload)
                {
                    string url = RemoveDoubleSlashes(
                        bootstrap.cdnBundleUrl + "/" + manifest.assetBundles[curAssetIndex].filename);
                    Uri bundleUrl = new Uri(url);

                    Debug.Log("Downloading bundle: " + bundleUrl.ToString());
                    LoomDownload(url, curBundleFullPath, OnBundleProgreeUpdate, OnBundleComplete);
                }
            }
        }

        int retryCnt = 0;

        private void OnBundleComplete(bool success, string arg2)
        {
            Debug.Log($"OnBundleComplete: {success}: {arg2}");
            if (success)
            {
                Loom.QueueOnMainThread(() =>
                {
                    //不加载了，直接把下载完的路径赋值过去
                    manifest.assetBundles[curAssetIndex].BundleFullPath = curBundleFullPath;
                    retryCnt = 0;
                    StartNextBundle(curAssetIndex + 1);
                });
            }
            else
            {
                //重试三次
                if (retryCnt < 2)
                {
                    progress = 0;
                    Debug.Log($"failed retry download,{manifest.assetBundles[curAssetIndex].filename}");

                    string url = RemoveDoubleSlashes(
                       bootstrap.cdnBundleUrl + "/" + manifest.assetBundles[curAssetIndex].filename);
                    Debug.Log("retry Downloading bundle: " + url);
                    LoomDownload(url, curBundleFullPath, OnBundleProgreeUpdate, OnBundleComplete);

                    retryCnt++;
                }
                else if (retryCnt == 2)
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        //三次不行弹窗
                        if (OnError != null)
                        {
                            Debug.Log("BundleCompleted OnError");
                            OnError(this, new ErrorArgs("Retry more than three times"));
                        }
                    });
                }
            }
        }

        private void OnBundleProgreeUpdate(float obj)
        {
            progress = obj;
            Loom.QueueOnMainThread(() =>
            {
                if (OnProgressUpdate!=null)
                {
                    OnProgressUpdate(GetProgress());
                }
            });
        }

        static HttpWebRequest getWebRequest(string url, int lStartPos)
        {
            HttpWebRequest request = null;
            try
            {
                request = (System.Net.HttpWebRequest)HttpWebRequest.Create(url);
                request.AddRange(lStartPos); //设置Range值
            }
            catch (Exception ex)
            {
                Debug.Log("create request error: " + ex.Message);
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

        static string RemoveDoubleSlashes(string url)
        {
            int schemeIndex = url.IndexOf("://");
            if (schemeIndex != -1)
            {
                string scheme = url.Substring(0, schemeIndex + 3);
                string remainder = url.Substring(schemeIndex + 3);
                string result = scheme + remainder.Replace("//", "/");
                return result;
            }
            else  // very simple, no scheme to worry about, so no place there SHOULD be double slashes we need to preserve.
            {
                return url.Replace("//", "/");
            }
        }

        public void LoomDownload(string url, string savePath, Action<float> onProgreeUpdate, Action<bool, string> onComplete)
        {
            //Run the action on a new thread
            Loom.RunAsync(() =>
            {
                simpleDownload(url, savePath, onProgreeUpdate, onComplete);
            });
        }

        public static string PERSIST_EXP = ".cdel";
        FileStream writer = null;
        WebResponse response = null;
        public void simpleDownload(string url, string savePath, Action<float> onProgreeUpdate, Action<bool, string> onComplete)
        {
            if (File.Exists(savePath))
            {
                FileInfo fileInfo = new FileInfo(savePath);
                string fileMd5 = GetFileMD5(savePath);
                if (fileInfo.Length == manifest.assetBundles[curAssetIndex].length &&
                    fileMd5 == manifest.assetBundles[curAssetIndex].md5)
                {
                    Debug.Log("文件己存在！");
                    if (onComplete != null)
                    {
                        onComplete(true, "Already Exist!");
                    }
                    return;
                }
                else
                {
                    Debug.Log("File not match delete redownload");
                    File.Delete(savePath);
                    simpleDownload(url, savePath, onProgreeUpdate, onComplete);
                }
            }
            else
            {
                savePath = savePath + PERSIST_EXP;


                try
                {
                    writer = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write);
                }
                catch (Exception ex)
                {
                    Debug.Log("writer = new FileStream error");
                    if (writer != null)
                    {
                        writer.Close();
                        writer.Dispose();
                    }
                    if (onComplete != null)
                    {
                        onComplete(false, ex.Message);
                    }
                    return;
                }
                HttpWebRequest request;
                long lStartPos = writer.Length; ;//当前文件大小
                long currentLength = 0;
                long totalLength = 0;//总大小
                if (File.Exists(savePath))//断点续传
                {
                    try
                    {
                        request = (HttpWebRequest)HttpWebRequest.Create(url);
                        request.Method = "HEAD";
                        response = (HttpWebResponse)request.GetResponse();
                        //改为上面的只请求Head
                        //response = request.GetResponse();
                    }
                    catch (Exception ex)
                    {
                        //失败,文件找不到 404 错误会在这里出现
                        Debug.Log($"AA: response = request.GetResponse() {ex.Message}, response==null? {response == null}");
                        //文件找不到 404 这里会空
                        if (response == null)
                        {
                            if (writer != null)
                            {
                                writer.Close();
                                writer.Dispose();
                            }
                        }
                        if (onComplete != null)
                        {
                            onComplete(false, ex.Message);
                        }
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
                            string realPath = savePath.Replace(PERSIST_EXP, "");
                            File.Move(savePath, realPath);
                            Debug.Log("下载完成!");

                            FileInfo fileInfo = new FileInfo(realPath);
                            string fileMd5 = GetFileMD5(realPath);
                            if (fileInfo.Length == manifest.assetBundles[curAssetIndex].length &&
                                fileMd5 == manifest.assetBundles[curAssetIndex].md5)
                            {
                                //if (onProgreeUpdate != null)
                                //{
                                //    onProgreeUpdate(1);
                                //}
                                if (onComplete != null)
                                {
                                    onComplete(true, "Download success!");
                                }
                                Debug.Log("下载完成!");

                            }
                            else
                            {
                                if (onComplete != null)
                                {
                                    onComplete(false, "file length or md5 not match!");
                                }
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
                            File.Delete(savePath);
                            simpleDownload(url, savePath, onProgreeUpdate, onComplete);
                            return;
                        }
                        request = getWebRequest(url, (int)lStartPos);
                        writer.Seek(lStartPos, SeekOrigin.Begin);
                        response = request.GetResponse(); //这要重新get 一次response，上一次只有head
                        totalLength = response.ContentLength + lStartPos; //
                        currentLength = lStartPos; //
                    }
                }
                if (response == null)
                {
                    Debug.Log("response = =null ");
                    if (writer != null)
                    {
                        writer.Close();
                        writer.Dispose();
                    }
                    if (onComplete != null)
                    {
                        onComplete(false, "response ==null");
                    }
                    return;
                }
                Stream reader = response.GetResponseStream();
                byte[] buff = new byte[1024];
                int c = 0; //实际读取的字节数
                while ((c = reader.Read(buff, 0, buff.Length)) > 0)
                {
                    currentLength += c;
                    writer.Write(buff, 0, c);
                    float curL = currentLength;
                    float totalL = totalLength;
                    float progressPercent = (float)(curL / totalL);
                    if (onProgreeUpdate != null)
                    {
                        onProgreeUpdate(progressPercent);
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
                    string realPath = savePath.Replace(PERSIST_EXP, "");
                    File.Move(savePath, realPath);

                    FileInfo fileInfo = new FileInfo(realPath);
                    string fileMd5 = GetFileMD5(realPath);
                    if (fileInfo.Length == manifest.assetBundles[curAssetIndex].length &&
                        fileMd5 == manifest.assetBundles[curAssetIndex].md5)
                    {
                        if (onComplete != null)
                        {
                            onComplete(true, "Download success!");
                        }
                        Debug.Log("下载完成!");

                    }
                    else
                    {
                        Debug.Log("file length or md5 not match!");
                        if (onComplete != null)
                        {
                            onComplete(false, "file length or md5 not match!");
                        }
                    }
                }


                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    response.Close();
                }
            }
        }

    }
}