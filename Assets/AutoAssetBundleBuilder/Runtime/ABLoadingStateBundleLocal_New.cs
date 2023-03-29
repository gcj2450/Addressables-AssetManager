using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace ZionGame
{
    public class ABLoadingStateBundleLocal_New : ABILoadingState
    {
        private UnityWebRequest unityWebRqst = null;
        private Dictionary<string, ABIData> configData = null;
        private ABBootstrap bootstrap = null;
        private ABManifest manifest = null;
        private string curBundleFullPath;
        private int curAssetIndex = 0;
        private float lastProgress = 0.0f;  // used when unityWebRqst is null, which could be confusing whether it's starting or ending		
        private string localCacheFolder;

        public Action OnEnd;
        public Action<float> OnProgressUpdate;
        public event EventHandler OnError;

        public ABLoadingStateBundleLocal_New(string _localCacheFolder, EventHandler _onError, Action<float> _onProgressUpdate, Action _onEnd)
        {
            localCacheFolder = _localCacheFolder;
            OnError = _onError;
            OnProgressUpdate = _onProgressUpdate;
            OnEnd = _onEnd;
        }

        public void Begin(Dictionary<string, ABIData> configData)
        {
            this.configData = configData;
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
            //Debug.Log("AAAAAAAAAA");
            ////这里完成了所有的事情，开始下一步
            //ABAssetLoader.Initialize(configData);
            if (unityWebRqst != null)
                Debug.LogError("<color=#ff8080>unityWebRqst should be null at End()</color>");
            if (OnEnd != null)
            {
                OnEnd();
            }
            configData = null;
            manifest = null;
            bootstrap = null;
           
        }

        public string GetStateText()
        {
#if UNITY_2020_1_OR_NEWER
            if (unityWebRqst != null && (unityWebRqst.result != UnityWebRequest.Result.InProgress && unityWebRqst.result != UnityWebRequest.Result.Success))
#else
				if (unityWebRqst != null && (unityWebRqst.isHttpError || unityWebRqst.isNetworkError || (unityWebRqst.downloadProgress < 1.0f && unityWebRqst.isDone)))
#endif
                return unityWebRqst.error;
            if (curAssetIndex >= manifest.assetBundles.Length)
                return "Bundles Loaded";
            return "Loading " + manifest.assetBundles[curAssetIndex].bundleName;
        }

        public bool IsDone()
        {
            return curAssetIndex >= manifest.assetBundles.Length && unityWebRqst == null;
        }

        public float GetProgress()
        {
            if (unityWebRqst != null)
            {
                // smooth interpolation across the whole set of files, assuming each one was the same size (which they aren't)
                float minValue = curAssetIndex / (float)manifest.assetBundles.Length;
                float maxValue = (curAssetIndex + 1) / (float)manifest.assetBundles.Length;
                lastProgress = Mathf.Lerp(minValue, maxValue, unityWebRqst.downloadProgress);
            }
            return lastProgress;
        }

        public void Retry()
        {
            StartNextBundle(curAssetIndex);
        }

        private void StartNextBundle(int index)
        {
            //Debug.Log(" manifest.assetBundles.Length: " + manifest.assetBundles.Length);
            curAssetIndex = index;
            if (curAssetIndex >= manifest.assetBundles.Length)
            {
                if (unityWebRqst != null)
                    unityWebRqst.Dispose();
                unityWebRqst = null;
                End();
            }
            else
            {
                if (!localCacheFolder.EndsWith("/"))
                    localCacheFolder = localCacheFolder + "/";
                // If we already have the asset bundle, just jump to the completion callback directly.
                curBundleFullPath = localCacheFolder + manifest.assetBundles[curAssetIndex].filename;
                bool doDownload = true;
                
                if (File.Exists(curBundleFullPath))
                {
                    FileInfo fileInfo = new FileInfo(curBundleFullPath);
                    string fileMd5 = ABUtilities.GetFileMD5(curBundleFullPath);
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
                            BundleCompleted(null);
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
                    Uri bundleUrl = new Uri(ABUtilities.RemoveDoubleSlashes(
                        bootstrap.cdnBundleUrl + "/" + manifest.assetBundles[curAssetIndex].filename));

                    Debug.Log("Downloading bundle: " + bundleUrl.ToString());

                    unityWebRqst = UnityWebRequest.Get(bundleUrl);
                    if (unityWebRqst == null)
                        Debug.LogError("<color=#ff8080>UnityWebRequest for bundle url is null.</color>");

                    // write this contents to the disk directly
                    unityWebRqst.downloadHandler = new DownloadHandlerFile(curBundleFullPath);
                    UnityWebRequestAsyncOperation requestAsyncOp = unityWebRqst.SendWebRequest();
                    while (!unityWebRqst.isDone)
                    {
                        if(OnProgressUpdate!=null)
                        {
                            OnProgressUpdate(GetProgress());
                        }
                    }
                    requestAsyncOp.completed += BundleCompleted;
                }
            }
        }

        // asyncOp may be null
        private void BundleCompleted(AsyncOperation asyncOp)
        {
            try
            {
                if (asyncOp != null && (!asyncOp.isDone || asyncOp.progress < 1.0f))
                    Debug.LogError("<color=#ff8080>AsyncOp is not done but called onComplete.</color>");

                if (unityWebRqst != null && unityWebRqst.responseCode != 200)  // null request just means it's cached, that's ok.
                {
                    Debug.LogError($"<color=#ff8080>Failed to load {unityWebRqst.url}   Response code: {unityWebRqst.responseCode}</color>");
                    if (OnError != null)
                    {
                        Debug.Log("BundleCompleted OnError");
                        OnError(this, new ErrorArgs($"request error:{unityWebRqst.responseCode}"));
                    }
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(curBundleFullPath);
                    string fileMd5 = ABUtilities.GetFileMD5(curBundleFullPath);
                    if (fileInfo.Length == manifest.assetBundles[curAssetIndex].length&&
                        fileMd5 == manifest.assetBundles[curAssetIndex].md5)
                    {
                        manifest.assetBundles[curAssetIndex].BundleFullPath = curBundleFullPath;

                        StartNextBundle(curAssetIndex + 1);
                    }
                    else
                    {
                        Debug.Log($"{manifest.assetBundles[curAssetIndex].filename} md5 or file length not match BundleCompleted OnError");
                        if (OnError != null)
                        {
                            OnError(this, new ErrorArgs("md5 or file length not match"));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Failed to download bundle " + e);
                if (OnError != null)
                {
                    OnError(this, new ErrorArgs(e.Message));
                }
                //throw;  // rethrow and retain stack trace
            }
        }

        //private void BundleLoaded(AsyncOperation asyncOp)
        //{
        //    try
        //    {
        //        AssetBundleCreateRequest abcr = asyncOp as AssetBundleCreateRequest;
        //        if (abcr == null)
        //        {
        //            Debug.Log("Wrong type of AsyncOp in BundleLoaded.");
        //            if (OnError != null)
        //            {
        //                Debug.Log("BundleCompleted OnError");
        //                OnError(this, new ErrorArgs("Wrong type of AsyncOp in BundleLoaded"));
        //            }
        //        }
        //        else
        //        {
        //            //这里是下载并且加载了bundle
        //            AssetBundle assetBundle = abcr.assetBundle;
        //            if (assetBundle == null)
        //                Debug.LogError("<color=#ff8080>AssetBundle was null: " + manifest.assetBundles[curAssetIndex].bundleName + "</color>");
        //            // fill out the asset bundle field so we have it for later
        //            manifest.assetBundles[curAssetIndex].Bundle = assetBundle;

        //            StartNextBundle(curAssetIndex + 1);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Log("Failed to open bundle " + e);
        //        if (OnError != null)
        //        {
        //            OnError(this, new ErrorArgs($"Failed to open bundle {e.Message}"));
        //        }
        //        //throw;  // rethrow and retain stack trace
        //    }
        //}

    }
}