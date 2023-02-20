//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		public class ABLoadingStateAssetBundles : ABILoadingState
		{
			private UnityWebRequest                    _request = null;
			private Dictionary<string, ABIData>        _configData = null;
			private ABBootstrap                        _bootstrap = null;
			private ABManifest                         _manifest = null;
			private string                             _bundleFullPath;
			private int                                _assetIndex = 0;
			private float                              _lastProgress = 0.0f;  // used when _request is null, which could be confusing whether it's starting or ending		
			private string                             _bundleFolderPrefix;
	
			public ABLoadingStateAssetBundles(string bundleFolderPrefix)
			{
				_bundleFolderPrefix = bundleFolderPrefix;
			}

			public void Begin(Dictionary<string, ABIData> configData)
			{
				_configData = configData;
				if (_configData.ContainsKey("Bootstrap")==false) 
					Debug.LogError("<color=#ff8080>Bootstrap is null.</color>");
				_bootstrap = _configData["Bootstrap"] as ABBootstrap;
				if (_configData.ContainsKey("Manifest")==false) 
					Debug.LogError("<color=#ff8080>Manifest is null.</color>");
				_manifest = configData["Manifest"] as ABManifest;

				StartNextBundle(0);
			}

			public void End()
			{
				if (_request != null) 
					Debug.LogError("<color=#ff8080>_request should be null at End()</color>");
				_configData = null;
				_manifest = null;
				_bootstrap = null;
			}

			public string GetStateText()
			{
#if UNITY_2020_1_OR_NEWER
				if (_request!=null && (_request.result!=UnityWebRequest.Result.InProgress && _request.result!=UnityWebRequest.Result.Success))
#else
				if (_request!=null && (_request.isHttpError || _request.isNetworkError || (_request.downloadProgress < 1.0f && _request.isDone)))
#endif
					return _request.error;
				if (_assetIndex>=_manifest.assetBundles.Length)
					return "Bundles Loaded";
				return "Loading "+_manifest.assetBundles[_assetIndex].bundleName;
			}

			public bool IsDone()
			{
				return _assetIndex >= _manifest.assetBundles.Length && _request==null;
			}

			public float GetProgress()
			{
				if (_request!=null)
				{
					// smooth interpolation across the whole set of files, assuming each one was the same size (which they aren't)
					float minValue = _assetIndex / (float)_manifest.assetBundles.Length;
					float maxValue = (_assetIndex+1) / (float)_manifest.assetBundles.Length;
					_lastProgress = Mathf.Lerp(minValue, maxValue, _request.downloadProgress);
				}	
				return _lastProgress;
			}

			private void StartNextBundle(int index)
			{
				_assetIndex = index;
				if (_assetIndex>=_manifest.assetBundles.Length)
				{
					if (_request!=null)
						_request.Dispose();
					_request = null;
				}
				else
				{
					// If we already have the asset bundle, just jump to the completion callback directly.
					_bundleFullPath = ABUtilities.GetRuntimeCacheFolder() + "/" + _manifest.assetBundles[_assetIndex].filename;
					bool doDownload = true;
					if (File.Exists(_bundleFullPath))
					{
						Debug.Log("Reading cached bundle: "+_manifest.assetBundles[_assetIndex].bundleName);
						try
						{
							BundleCompleted(null);  // attempt to open the local file.  If it's valid, we use it.  If not, redownload it.
							doDownload = false;
						}
						catch (Exception)
						{
							doDownload = true;
							File.Delete(_bundleFullPath);  // nuke the bad file and grab it again
							Debug.Log("Retrying with download.");
						}
					}
				
					if (doDownload)
					{
						Uri bundleUrl = new Uri(ABUtilities.RemoveDoubleSlashes(_bundleFolderPrefix + _bootstrap.cdnBundleUrl + "/" + _manifest.assetBundles[_assetIndex].filename));
						Debug.Log("Downloading bundle: "+bundleUrl.ToString());
					
						_request = UnityWebRequest.Get(bundleUrl);
						if (_request==null) 
							Debug.LogError("<color=#ff8080>UnityWebRequest for bundle url is null.</color>");

						_request.downloadHandler = new DownloadHandlerFile(_bundleFullPath);  // write this contents to the disk directly
						UnityWebRequestAsyncOperation requestAsyncOp = _request.SendWebRequest();
						requestAsyncOp.completed += BundleCompleted;
					}
				}
			}

			private void BundleCompleted(AsyncOperation asyncOp)  // asyncOp may be null
			{
				try
				{
					if (asyncOp!=null && (!asyncOp.isDone || asyncOp.progress<1.0f)) 
						Debug.LogError("<color=#ff8080>AsyncOp is not done but called onComplete.</color>");

					if (_request!=null && _request.responseCode!=200)  // null request just means it's cached, that's ok.
					{
						Debug.LogError($"<color=#ff8080>Failed to load {_request.url}   Response code: {_request.responseCode}</color>");
					}

					AssetBundleCreateRequest abcr = AssetBundle.LoadFromFileAsync(_bundleFullPath);
					abcr.completed += BundleLoaded;
				}
				catch (Exception e)
				{
					Debug.LogError("Failed to download bundle "+e);
					throw;  // rethrow and retain stack trace
				}
			}

			private void BundleLoaded(AsyncOperation asyncOp)
			{
				try
				{
					AssetBundleCreateRequest abcr = asyncOp as AssetBundleCreateRequest;
					if (abcr==null) 
						Debug.LogError("<color=#ff8080>Wrong type of AsyncOp in BundleLoaded.</color>");
					AssetBundle assetBundle = abcr.assetBundle;
					if (assetBundle==null) 
						Debug.LogError("<color=#ff8080>AssetBundle was null: "+_manifest.assetBundles[_assetIndex].bundleName+"</color>");

					_manifest.assetBundles[_assetIndex].assetBundle = assetBundle;  // fill out the asset bundle field so we have it for later

					StartNextBundle(_assetIndex+1);

#if false
					// Helpful to debug what is being loaded from where
					foreach (string assetInBundle in assetBundle.GetAllAssetNames())
					{
						Debug.Log("Bundle: "+assetBundle.name+" Asset: "+assetInBundle);
					}
#endif
				}
				catch (Exception e)
				{
					Debug.LogError("Failed to open bundle "+e);
					throw;  // rethrow and retain stack trace
				}
			}
		}
	}
}