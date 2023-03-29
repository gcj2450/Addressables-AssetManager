using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace ZionGame
{
    /// <summary>
    /// 只下载，不加载Bundle文件
    /// </summary>
    public class ABLoadingStateBundleMultiNetwork : ABILoadingState
    {
        private Dictionary<string, ABIData> configData = null;
        private ABBootstrap bootstrap = null;
        private ABManifest manifest = null;
        private string curBundleFullPath;
        private int curAssetIndex = 0;
        private float lastProgress = 0.0f;  // used when unityWebRqst is null, which could be confusing whether it's starting or ending		
        private string localCacheFolder;

        public event EventHandler OnError;
        public Action OnEnd;
        public Action<float> OnProgressUpdate;
        float progress;

        BundleFileDownloader fileDownloader;
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="_localCacheFolder">本地缓存文件夹</param>
        /// <param name="eventHandler"></param>
        /// <param name="_onEnd">全部结束事件</param>
        public ABLoadingStateBundleMultiNetwork(string _localCacheFolder, EventHandler _onError,Action<float>_onProgressUpdate,Action _onEnd)
        {
            OnProgressUpdate=_onProgressUpdate;
            localCacheFolder = _localCacheFolder;
            OnEnd = _onEnd;
            OnError = _onError;
            fileDownloader = new BundleFileDownloader(1);
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
            // smooth interpolation across the whole set of files, assuming each one was the same size (which they aren't)
            float minValue = curAssetIndex / (float)manifest.assetBundles.Length;
            float maxValue = (curAssetIndex + 1) / (float)manifest.assetBundles.Length;
            lastProgress = Mathf.Lerp(minValue, maxValue, progress);
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
            fileDownloader.SingleThreadDownload(url, curBundleFullPath, 
                OnBundleProgreeUpdate, OnBundleComplete, OnBundleError
                );

            retryCnt++;
        }

        private void StartNextBundle(int index)
        {
            //Debug.Log(" manifest.assetBundles.Length: " + manifest.assetBundles.Length);
            curAssetIndex = index;
            if (curAssetIndex >= manifest.assetBundles.Length)
            {
                //全部下载完成了
                //if (unityWebRqst != null)
                //    unityWebRqst.Dispose();
                //unityWebRqst = null;
                Debug.Log("curAssetIndex >= manifest.assetBundles.Length");
                End();

            }
            else
            {
                if (!localCacheFolder.EndsWith("/"))
                    localCacheFolder = localCacheFolder + "/";
                curBundleFullPath = localCacheFolder + manifest.assetBundles[curAssetIndex].filename;
                string url = RemoveDoubleSlashes(
                        bootstrap.cdnBundleUrl + "/" + manifest.assetBundles[curAssetIndex].filename);

                bool doDownload = true;

                if (File.Exists(curBundleFullPath))
                {
                    FileInfo fileInfo = new FileInfo(curBundleFullPath);
                    string fileMd5 = GetFileMD5(curBundleFullPath);
                    //Debug.Log(fileInfo.Length + "__" + manifest.assetBundles[curAssetIndex].length + "___" +
                    //    fileMd5 + "__" + manifest.assetBundles[curAssetIndex].md5);
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
                            OnBundleComplete(url, curBundleFullPath);
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
                    Uri bundleUrl = new Uri(url);

                    Debug.Log("Downloading bundle: " + bundleUrl.ToString());
                    fileDownloader.SingleThreadDownload(url, curBundleFullPath, 
                        OnBundleProgreeUpdate, OnBundleComplete, OnBundleError
                        );
                }
            }
        }

        private void OnBundleError(Exception obj)
        {
            //重试三次
            if (retryCnt < 2)
            {
                progress = 0;
                Debug.Log($"failed retry download,{manifest.assetBundles[curAssetIndex].filename}");

                string url = RemoveDoubleSlashes(
                   bootstrap.cdnBundleUrl + "/" + manifest.assetBundles[curAssetIndex].filename);
                Debug.Log("retry Downloading bundle: " + url);
                fileDownloader.SingleThreadDownload(url, curBundleFullPath, 
                    OnBundleProgreeUpdate, OnBundleComplete, OnBundleError
                    );

                retryCnt++;
            }
            else if (retryCnt == 2)
            {
                //Loom.QueueOnMainThread(() =>
                //{
                //三次不行弹窗
                if (OnError != null)
                {
                    Debug.Log("BundleCompleted OnError");
                    OnError(this, new ErrorArgs("Retry more than three times"));
                }
                //});
            }
        }

        int retryCnt = 0;

        private void OnBundleComplete(string url, string arg2)
        {
            //Loom.QueueOnMainThread(() =>
            //{
            //不加载了，直接把下载完的路径赋值过去

            FileInfo fileInfo = new FileInfo(curBundleFullPath);
            string fileMd5 = GetFileMD5(curBundleFullPath);
            if (fileInfo.Length == manifest.assetBundles[curAssetIndex].length &&
                fileMd5 == manifest.assetBundles[curAssetIndex].md5)
            {
                manifest.assetBundles[curAssetIndex].BundleFullPath = curBundleFullPath;
                retryCnt = 0;
                StartNextBundle(curAssetIndex + 1);
            }
            else
            {
                File.Delete(curBundleFullPath);

                Uri bundleUrl = new Uri(url);

                Debug.Log("Downloading bundle: " + bundleUrl.ToString());
                fileDownloader.SingleThreadDownload(url, curBundleFullPath,
                    OnBundleProgreeUpdate, OnBundleComplete, OnBundleError
                    );
            }

           
            //});
        }

        private void OnBundleProgreeUpdate(long curbytes,long totalbytes)
        {
            progress = (float)curbytes /totalbytes;
            //Loom.QueueOnMainThread(() =>
            //{
                if (OnProgressUpdate!=null)
                {
                    OnProgressUpdate(GetProgress());
                }
            //});
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


    }
}