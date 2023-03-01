using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace ZionGame
{
    public class ABLoadingStateManifest : ABILoadingState
    {
        private Dictionary<string, ABIData> _configData = null;
        private UnityWebRequest _request = null;
        private bool _isDone = false;
        private string _manifestFilename;
        private string _manifestFullPath;
        private bool _failedToParse = false;
        private string _parseErrorString = "";
        private float _lastProgress = 0.0f;
        private string _overrideManifestPath;
        private string _bundleFolderPrefix;

        /// <summary>
        /// 版本号
        /// </summary>
        private string _bundleVersion = "";

        public event EventHandler OnError;

        // Bundle folder prefix is only used when trying to load self-contained bundles.
        public ABLoadingStateManifest(string overrideManifestPath, string bundleFolderPrefix, EventHandler eventHandler)
        {
            if (File.Exists(overrideManifestPath))
            {
                _overrideManifestPath = overrideManifestPath;
            }
            _bundleFolderPrefix = bundleFolderPrefix;
            OnError = eventHandler;
        }

        public void Begin(Dictionary<string, ABIData> configData)
        {
            _configData = configData;
            _isDone = false;
            if (configData.ContainsKey("Bootstrap") == false)
                Debug.LogError("<color=#ff8080>Bootstrap is null</color>");
            ABBootstrap bootstrap = configData["Bootstrap"] as ABBootstrap;

            // load up the version number this player was built with.
            if (configData.ContainsKey("BuildData") == false)
                Debug.LogError("<color=#ff8080>BuildData is null</color>");
            ABBuild buildData = _configData["BuildData"] as ABBuild;

            //ABLoader配置的版本号
            _bundleVersion = buildData.buildVersion;

            _manifestFilename = "manifest-" + buildData.buildVersion + ".json";
            _manifestFullPath = ABUtilities.GetRuntimeCacheFolder(buildData.buildVersion)+ _manifestFilename;
            // If we already have this version of the manifest cached,
            // just jump to the completion callback, since it's going to load it from disk anyway.
            bool doDownload = true;
            if (File.Exists(_manifestFullPath))
            {
                Debug.Log("Reading cached manifest: " + _manifestFullPath);
                try
                {
                    ParseManifest(null);
                    doDownload = false;
                }
                catch (Exception)
                {
                    File.Delete(_manifestFullPath);  // cached file is garbage, try reloading from the web
                    _failedToParse = false;
                    doDownload = true;
                }
            }

            if (doDownload)
            {
                Uri manifestUrl = new Uri(ABUtilities.RemoveDoubleSlashes(_bundleFolderPrefix + bootstrap.cdnBundleUrl + "/" + _manifestFilename));
                _request = UnityWebRequest.Get(manifestUrl);
                Debug.Log("Downloading manifest: " + manifestUrl);
                if (_request == null) Debug.LogError("<color=#ff8080>UnityWebRequest for manifest url is null.</color>");

                _request.downloadHandler = new DownloadHandlerFile(_manifestFullPath);  // write this contents to the disk directly
                UnityWebRequestAsyncOperation requestAsyncOp = _request.SendWebRequest();
                requestAsyncOp.completed += ParseManifest;
            }
        }

        public void End()
        {
            if (_request != null)
                _request.Dispose();
            _configData = null;
            _request = null;
            _failedToParse = false;
            _parseErrorString = "";
        }

        public string GetStateText()
        {
            if (_failedToParse)
                return _parseErrorString;
#if UNITY_2020_1_OR_NEWER
            if (_request != null && (_request.result != UnityWebRequest.Result.InProgress && _request.result != UnityWebRequest.Result.Success))
#else
				if (_request!=null && (_request.isHttpError || _request.isNetworkError || (_request.downloadProgress < 1.0f && _request.isDone)))
#endif
                return _request.error;
            return "Loading Asset Manifest...";
        }

        public bool IsDone()
        {
            return _isDone;
        }

        public float GetProgress()
        {
            if (_request != null)
            {
                _lastProgress = _request.downloadProgress;
            }
            return _lastProgress;
        }

        public void Retry()
        {
            Begin(_configData);
        }

        private void ParseManifest(AsyncOperation asyncOp)
        {
            try
            {
                if (asyncOp != null && (!asyncOp.isDone || asyncOp.progress < 1.0f))
                    Debug.LogError("<color=#ff8080>AsyncOp is not done but called onComplete.</color>");

                if (_request != null && _request.responseCode != 200)  // null request just means it's cached, that's ok.
                {
                    _failedToParse = true;
                    _parseErrorString = $"<color=#ff8080>Failed to load {_request.url}   Response code: {_request.responseCode}</color>";
                }
                else
                {
                    // Load it from local storage, parse it as JSON
                    byte[] fileContents = File.ReadAllBytes(_manifestFullPath);
                    string jsonString = ABUtilities.GetStringFromUTF8File(fileContents);
                    ABManifest manifest = JsonUtility.FromJson<ABManifest>(jsonString);  // convert json to our data structure
                    if (manifest == null)
                    {
                        _failedToParse = true;
                        _parseErrorString = "<color=#ff8080>Bad request. Manifest==null</color>";
                    }
                    else
                    {
                        // Make the runtime type array from known types
                        manifest.GenerateRuntimeTypes();

                        //_overrideManifestPath这个路径为空，所以这一段肯定是不执行的
                        //// This is where override bundle manifest gets loaded.  It's not currently setup to be loaded via URL, so it's not really useful outside the editor.
                        //// But if it were needed, this would be where that code would go.
                        //#if UNITY_EDITOR
                        //							// Wedge in the override manifest before we load asset bundles
                        //							if (string.IsNullOrEmpty(_overrideManifestPath)==false)
                        //							{
                        //								Debug.Log("Merging in override manifest: "+_overrideManifestPath);

                        //								// Handle loading the override bundle manifest synchronously
                        //								byte[] fileContents2 = File.ReadAllBytes(_overrideManifestPath);
                        //								string jsonString2 = ABUtilities.GetStringFromUTF8File(fileContents2);
                        //								ABManifest overrideManifest = JsonUtility.FromJson<ABManifest>(jsonString2);  // convert json to our data structure

                        //								// Merge our overrides into the main manifest now that we're done loading them
                        //								manifest.MergeManifest(overrideManifest);
                        //							}
                        //#endif

                        _configData.Add("Manifest", manifest);

                        // Now, use the manifest to prune the cache of unneeded files.
                        PruneCache(manifest.assetBundles);

                        // We are done only if we successfully completed this state.
                        _isDone = true;
                    }
                }
            }
            catch (Exception jsonException)
            {
                _failedToParse = true;
                if (_request == null)
                {
                    _parseErrorString = "<color=#ff8080>Parse manifest: null request</color>";
                }
                else
                {
                    _parseErrorString = "<color=#ff8080>Parse manifest: " + jsonException.Message + "</color>";
                }
            }
            if (_failedToParse)
            {
                Debug.LogError(_parseErrorString);
                if (Directory.Exists(ABUtilities.GetRuntimeCacheFolder(_bundleVersion)))
                {
                    Directory.Delete(ABUtilities.GetRuntimeCacheFolder(_bundleVersion), true);
                }
                if (OnError != null)
                {
                    Debug.Log("ParseManifest OnError");
                    OnError(this, null);
                }
            }
        }

        // Remove everything except the manifest file and the bundles it describes
        private void PruneCache(ABManifest.ABAssetBundleInfo[] bundles)
        {
            string path = ABUtilities.GetRuntimeCacheFolder(_bundleVersion);

            // remove any manifests that are out of date
            foreach (string filename in Directory.GetFiles(path, "manifest-*.json", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetFileName(filename) != _manifestFilename)
                {
                    Debug.Log("Deleting obsolete manifest: " + Path.GetFileName(filename));
                    File.Delete(filename);
                }
            }

            // remove any assetbundle files that are out of date too
            foreach (string filename in Directory.GetFiles(path, "*.assetbundle", SearchOption.TopDirectoryOnly))
            {
                string fn = Path.GetFileName(filename);
                bool found = false;
                foreach (ABManifest.ABAssetBundleInfo info in bundles)
                {
                    if (fn == info.filename)
                        found = true;
                }

                // do NOT delete any override-*.assetbundle, ever.  It's special.
                if (fn.StartsWith("override-"))
                    found = true;

                if (!found)
                {
                    Debug.Log("Deleting obsolete bundle: " + Path.GetFileName(filename));
                    File.Delete(filename);
                }
            }
        }


    }
}