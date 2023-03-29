using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Net;
using System.Threading;

namespace ZionGame
{
    /// <summary>
    /// 错误参数
    /// </summary>
    //public class ErrorArgs : EventArgs
    //{
    //    public string Reason = "";

    //    public ErrorArgs(string _reason)
    //    {
    //        Reason = _reason;
    //    }
    //}
    public class ABLoadingStateBundleNetwork : ABILoadingState
    {
        //private UnityWebRequest unityWebRqst = null;
        private Dictionary<string, ABIData> _configData = null;
        private ABBootstrap _bootstrap = null;
        private ABManifest _manifest = null;
        private string _bundleFullPath;
        private int _assetIndex = 0;
        private float _lastProgress = 0.0f;  // used when unityWebRqst is null, which could be confusing whether it's starting or ending		
        private string _bundleFolderPrefix;

        private string _bundleVersion = "";

        public event EventHandler OnError;

        string errorStr = "";
        float progress = 0;
        public ABLoadingStateBundleNetwork(string bundleVersion, string bundleFolderPrefix, EventHandler eventHandler)
        {
            _bundleVersion = bundleVersion;
            _bundleFolderPrefix = bundleFolderPrefix;
            OnError = eventHandler;
        }

        public void Begin(Dictionary<string, ABIData> configData)
        {
            _configData = configData;
            if (_configData.ContainsKey("Bootstrap") == false)
                Debug.LogError("<color=#ff8080>Bootstrap is null.</color>");
            _bootstrap = _configData["Bootstrap"] as ABBootstrap;
            if (_configData.ContainsKey("Manifest") == false)
                Debug.LogError("<color=#ff8080>Manifest is null.</color>");
            _manifest = configData["Manifest"] as ABManifest;

            StartNextBundle(0);
        }

        public void End()
        {
            //Debug.Log("AAAAAAAAAA");
            ////这里完成了所有的事情，开始下一步
            //ABAssetLoader.Initialize(configData);

            if (!string.IsNullOrEmpty(errorStr))
                Debug.LogError("<color=#ff8080>unityWebRqst should be null at End()</color>");
            _configData = null;
            _manifest = null;
            _bootstrap = null;
            errorStr = "";
            progress = 0;
        }

        public string GetStateText()
        {
            if (!string.IsNullOrEmpty(errorStr))
            {
                return errorStr;
            }
            if (_assetIndex >= _manifest.assetBundles.Length)
                return "Bundles Loaded";
            return "Loading " + _manifest.assetBundles[_assetIndex].bundleName;
        }

        public bool IsDone()
        {
            return _assetIndex >= _manifest.assetBundles.Length && string.IsNullOrEmpty(errorStr);
        }

        public float GetProgress()
        {
            if (writer != null && response != null)
            {
                // smooth interpolation across the whole set of files, assuming each one was the same size (which they aren't)
                float minValue = _assetIndex / (float)_manifest.assetBundles.Length;
                float maxValue = (_assetIndex + 1) / (float)_manifest.assetBundles.Length;
                _lastProgress = Mathf.Lerp(minValue, maxValue, progress);
            }
            //Debug.Log($"lastProgress {lastProgress}, curAssetIndex:{curAssetIndex}, manifest.assetBundles.Length:{manifest.assetBundles.Length} ");
            return Mathf.Clamp(_lastProgress, 0, 1);
        }

        public void Retry()
        {
            Debug.Log("Retry: retry download...");
            errorStr = "";
            progress = 0;
            retryCnt = 0;
            Debug.Log($"failed retry download,{_manifest.assetBundles[_assetIndex].filename}");

            string url = ABUtilities.RemoveDoubleSlashes(
               _bundleFolderPrefix + _bootstrap.cdnBundleUrl + "/" + _manifest.assetBundles[_assetIndex].filename);
            Debug.Log("retry Downloading bundle: " + url);
            LoomDownload(url, _bundleFullPath, OnBundleProgreeUpdate, OnBundleComplete);

            retryCnt++;
        }

        private void StartNextBundle(int index)
        {
            //Debug.Log(" manifest.assetBundles.Length: " + manifest.assetBundles.Length);
            _assetIndex = index;
            if (_assetIndex >= _manifest.assetBundles.Length)
            {
                //if (unityWebRqst != null)
                //    unityWebRqst.Dispose();
                //unityWebRqst = null;
                Debug.Log("curAssetIndex >= manifest.assetBundles.Length");
            }
            else
            {
                // If we already have the asset bundle, just jump to the completion callback directly.
                _bundleFullPath = ABUtilities.GetRuntimeCacheFolder(_bundleVersion) + "/" + _manifest.assetBundles[_assetIndex].filename;
                bool doDownload = true;

                if (File.Exists(_bundleFullPath))
                {
                    FileInfo fileInfo = new FileInfo(_bundleFullPath);
                    string fileMd5 = ABUtilities.GetFileMD5(_bundleFullPath);
                    Debug.Log(fileInfo.Length + "__" + _manifest.assetBundles[_assetIndex].length + "___" +
                        fileMd5 + "__" + _manifest.assetBundles[_assetIndex].md5);
                    if (fileInfo.Length != _manifest.assetBundles[_assetIndex].length ||
                        fileMd5 != _manifest.assetBundles[_assetIndex].md5)
                    {
                        Debug.Log("File length is not same delete local file and redownload...");
                        doDownload = true;
                        File.Delete(_bundleFullPath);
                        //文件大小不一致，删除这个文件重新下载
                    }
                    else
                    {
                        Debug.Log("Find cached bundle and file size is same try read...");
                        Debug.Log("Reading cached bundle: " + _manifest.assetBundles[_assetIndex].bundleName);
                        try
                        {
                            // attempt to open the local file.  If it's valid, we use it.  If not, redownload it.
                            OnBundleComplete(true, "Already Exists");
                            doDownload = false;
                        }
                        catch (Exception)
                        {
                            doDownload = true;
                            File.Delete(_bundleFullPath);  // nuke the bad file and grab it again
                            Debug.Log("Retrying with download.");
                        }
                    }
                }
                else
                    Debug.Log("file not exist...");

                if (doDownload)
                {
                    string url = ABUtilities.RemoveDoubleSlashes(
                        _bundleFolderPrefix + _bootstrap.cdnBundleUrl + "/" + _manifest.assetBundles[_assetIndex].filename);
                    Uri bundleUrl = new Uri(url);

                    Debug.Log("Downloading bundle: " + bundleUrl.ToString());
                    LoomDownload(url, _bundleFullPath, OnBundleProgreeUpdate, OnBundleComplete);
                    //unityWebRqst = UnityWebRequest.Get(bundleUrl);
                    //if (unityWebRqst == null)
                    //    Debug.LogError("<color=#ff8080>UnityWebRequest for bundle url is null.</color>");

                    //// write this contents to the disk directly
                    //unityWebRqst.downloadHandler = new DownloadHandlerFile(curBundleFullPath);
                    //UnityWebRequestAsyncOperation requestAsyncOp = unityWebRqst.SendWebRequest();
                    //requestAsyncOp.completed += BundleCompleted;
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

                    //manifest.assetBundles[curAssetIndex].assetBundleRef =
                    //    new ABBundleReference(curBundleFullPath, manifest.assetBundles[curAssetIndex].assetNames);
                    //在这里的时候可以不用加载，使用上面的一句就行

                    AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(_bundleFullPath);

                    abcr.completed += BundleLoaded;

                });
            }
            else
            {
                //重试三次
                if (retryCnt < 2)
                {
                    errorStr = "";
                    progress = 0;
                    Debug.Log($"failed retry download,{_manifest.assetBundles[_assetIndex].filename}");

                    string url = ABUtilities.RemoveDoubleSlashes(
                       _bundleFolderPrefix + _bootstrap.cdnBundleUrl + "/" + _manifest.assetBundles[_assetIndex].filename);
                    Debug.Log("retry Downloading bundle: " + url);
                    LoomDownload(url, _bundleFullPath, OnBundleProgreeUpdate, OnBundleComplete);

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
                string fileMd5 = ABUtilities.GetFileMD5(savePath);
                if (fileInfo.Length == _manifest.assetBundles[_assetIndex].length &&
                    fileMd5 == _manifest.assetBundles[_assetIndex].md5)
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
                            string fileMd5 = ABUtilities.GetFileMD5(realPath);
                            if (fileInfo.Length == _manifest.assetBundles[_assetIndex].length &&
                                fileMd5 == _manifest.assetBundles[_assetIndex].md5)
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
                    string fileMd5 = ABUtilities.GetFileMD5(realPath);
                    if (fileInfo.Length == _manifest.assetBundles[_assetIndex].length &&
                        fileMd5 == _manifest.assetBundles[_assetIndex].md5)
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

        private void BundleLoaded(AsyncOperation asyncOp)
        {
            try
            {
                AssetBundleCreateRequest abcr = asyncOp as AssetBundleCreateRequest;
                if (abcr == null)
                {
                    Debug.LogError("<color=#ff8080>Wrong type of AsyncOp in BundleLoaded.</color>");
                    if (OnError != null)
                    {
                        Debug.Log("BundleCompleted OnError Wrong type of AsyncOp in BundleLoaded");
                        OnError(this, new ErrorArgs("Wrong type of AsyncOp in BundleLoaded"));
                    }
                }
                else
                {
                    AssetBundle assetBundle = abcr.assetBundle;
                    if (assetBundle == null)
                        Debug.LogError("<color=#ff8080>AssetBundle was null: " + _manifest.assetBundles[_assetIndex].bundleName + "</color>");
                    // fill out the asset bundle field so we have it for later
                    _manifest.assetBundles[_assetIndex].assetBundle = assetBundle;
                        //new ABBundleReference(, manifest.assetBundles[curAssetIndex].assetNames);
                    retryCnt = 0;
                    StartNextBundle(_assetIndex + 1);

#if false
					// Helpful to debug what is being loaded from where
					foreach (string assetInBundle in assetBundle.GetAllAssetNames())
					{
						Debug.Log("Bundle: "+assetBundle.name+" Asset: "+assetInBundle);
					}
#endif
                }
            }
            catch (Exception e)
            {
                if (OnError != null)
                {
                    Debug.Log("Failed to open bundle " + e.Message);
                    OnError(this, new ErrorArgs(e.Message));
                }
                //throw;  // rethrow and retain stack trace
            }
        }
    }
}