using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEditor.VersionControl;
using static ZionGame.ABManifest;
using ZionGame;

namespace Baidu.Meta.ComponentsBundleLoader.Runtime
{
    public class BundlesDownloader
    {
        private UnityWebRequest _request = null;
        public ABAssetBundleInfo[] assetBundles;
        private string _bundleFullPath;
        private int _assetIndex = 0;
        private float _lastProgress = 0.0f;  // used when _request is null, which could be confusing whether it's starting or ending		
        private string _bundleFolderPrefix;

        private string _bundleVersion = "";

        string bosBucket = "";

        public event EventHandler OnError;

        public BundlesDownloader(string bundleVersion, string bundleFolderPrefix, EventHandler eventHandler)
        {
            _bundleVersion = bundleVersion;
            _bundleFolderPrefix = bundleFolderPrefix;
            OnError = eventHandler;
        }

        public void Begin(ABAssetBundleInfo[] _assetBundles,string _bosBucket)
        {
            assetBundles = _assetBundles;
            bosBucket = _bosBucket;
            StartNextBundle(0);
        }

        public void End()
        {
            ////这里完成了所有的事情，开始下一步
            //ABAssetLoader.Initialize(configData);
            if (_request != null)
                Debug.LogError("<color=#ff8080>_request should be null at End()</color>");
        }

        public string GetStateText()
        {
#if UNITY_2020_1_OR_NEWER
            if (_request != null && (_request.result != UnityWebRequest.Result.InProgress && _request.result != UnityWebRequest.Result.Success))
#else
				if (_request!=null && (_request.isHttpError || _request.isNetworkError || (_request.downloadProgress < 1.0f && _request.isDone)))
#endif
                return _request.error;
            if (_assetIndex >= assetBundles.Length)
                return "Bundles Loaded";
            return "Loading " + assetBundles[_assetIndex].bundleName;
        }

        public bool IsDone()
        {
            return _assetIndex >= assetBundles.Length && _request == null;
        }

        public float GetProgress()
        {
            if (_request != null)
            {
                // smooth interpolation across the whole set of files, assuming each one was the same size (which they aren't)
                float minValue = _assetIndex / (float)assetBundles.Length;
                float maxValue = (_assetIndex + 1) / (float)assetBundles.Length;
                _lastProgress = Mathf.Lerp(minValue, maxValue, _request.downloadProgress);
            }
            return _lastProgress;
        }

        public void Retry()
        {
            StartNextBundle(_assetIndex);
        }

        private void StartNextBundle(int index)
        {
            //Debug.Log(" manifest.assetBundles.Length: " + manifest.assetBundles.Length);
            _assetIndex = index;
            if (_assetIndex >= assetBundles.Length)
            {
                if (_request != null)
                    _request.Dispose();
                _request = null;
            }
            else
            {
                // If we already have the asset bundle, just jump to the completion callback directly.
                _bundleFullPath = ABUtilities.GetRuntimeCacheFolder(_bundleVersion) + "/" + assetBundles[_assetIndex].filename;
                bool doDownload = true;
                
                if (File.Exists(_bundleFullPath))
                {
                    FileInfo fileInfo = new FileInfo(_bundleFullPath);
                    string fileMd5 = ABUtilities.GetFileMD5(_bundleFullPath);
                    if (fileInfo.Length != assetBundles[_assetIndex].length ||
                        fileMd5 != assetBundles[_assetIndex].md5)
                    {
                        Debug.Log("File length is not same delete local file and redownload...");
                        doDownload = true;
                        File.Delete(_bundleFullPath);
                        //文件大小不一致，删除这个文件重新下载
                    }
                    else
                    {
                        Debug.Log("Find cached bundle and file size is same try read...");
                        Debug.Log("Reading cached bundle: " + assetBundles[_assetIndex].bundleName);
                        try
                        {
                            // attempt to open the local file.  If it's valid, we use it.  If not, redownload it.
                            BundleCompleted(null);
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
                    //这个地址不对，还需要重写额==============================
                    Uri bundleUrl = new Uri(ABUtilities.RemoveDoubleSlashes(
                        _bundleFolderPrefix + bosBucket + "/" + assetBundles[_assetIndex].filename));

                    Debug.Log("Downloading bundle: " + bundleUrl.ToString());

                    _request = UnityWebRequest.Get(bundleUrl);
                    if (_request == null)
                        Debug.LogError("<color=#ff8080>UnityWebRequest for bundle url is null.</color>");

                    // write this contents to the disk directly
                    _request.downloadHandler = new DownloadHandlerFile(_bundleFullPath);
                    UnityWebRequestAsyncOperation requestAsyncOp = _request.SendWebRequest();
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

                if (_request != null && _request.responseCode != 200)  // null request just means it's cached, that's ok.
                {
                    Debug.LogError($"<color=#ff8080>Failed to load {_request.url}   Response code: {_request.responseCode}</color>");
                    if (OnError != null)
                    {
                        Debug.Log("BundleCompleted OnError");
                        OnError(this, new ErrorArgs($"request error:{_request.responseCode}"));
                    }
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(_bundleFullPath);
                    string fileMd5 = ABUtilities.GetFileMD5(_bundleFullPath);
                    if (fileInfo.Length == assetBundles[_assetIndex].length&&
                        fileMd5 == assetBundles[_assetIndex].md5)
                    {
                        //manifest.assetBundles[curAssetIndex].assetBundleRef =
                        //    new ABBundleReference(curBundleFullPath, manifest.assetBundles[curAssetIndex].assetNames);
                        //在这里的时候可以不用加载，使用上面的一句就行

                        AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(_bundleFullPath);
                        abcr.completed += BundleLoaded;
                    }
                    else
                    {
                        Debug.Log($"{assetBundles[_assetIndex].filename} md5 or file length not match BundleCompleted OnError");
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

        private void BundleLoaded(AsyncOperation asyncOp)
        {
            try
            {
                AssetBundleCreateRequest abcr = asyncOp as AssetBundleCreateRequest;
                if (abcr == null)
                {
                    Debug.Log("Wrong type of AsyncOp in BundleLoaded.");
                    if (OnError != null)
                    {
                        Debug.Log("BundleCompleted OnError");
                        OnError(this, new ErrorArgs("Wrong type of AsyncOp in BundleLoaded"));
                    }
                }
                else
                {
                    //这里是下载并且加载了bundle
                    AssetBundle assetBundle = abcr.assetBundle;
                    if (assetBundle == null)
                        Debug.LogError("<color=#ff8080>AssetBundle was null: " + assetBundles[_assetIndex].bundleName + "</color>");
                    // fill out the asset bundle field so we have it for later
                    assetBundles[_assetIndex].assetBundle = assetBundle;

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
                Debug.Log("Failed to open bundle " + e);
                if (OnError != null)
                {
                    OnError(this, new ErrorArgs($"Failed to open bundle {e.Message}"));
                }
                //throw;  // rethrow and retain stack trace
            }
        }

    }
}