//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		public class ABLoadingStateBootstrap : ABILoadingState
		{
			private string                           _bootstrapURL;
			private ABBuild                          _buildData;
			private UnityWebRequest                  _request = null;
			private bool                             _isDone = false;
			private Dictionary<string, ABIData>      _configData = null;
			private bool                             _failedToParse = false;
			private string                           _parseErrorString = "";
			private float                            _lastProgress = 0.0f;

			public ABLoadingStateBootstrap(string bootstrapURL)
			{
				_bootstrapURL = bootstrapURL;
			}
	
			public void Begin(Dictionary<string, ABIData> configData)
			{
				_configData = configData;
				_isDone = false;
	
				if (_configData.ContainsKey("BuildData")==false) 
					Debug.LogError("<color=#ff8080>BuildData is null</color>");
				_buildData = _configData["BuildData"] as ABBuild;

				// Produce a viable config URL with platform substituted into it.
				string platformConfigURL = _bootstrapURL.Replace("{PLATFORM}", _buildData.platform);

#if UNITY_EDITOR
				// Editor workflow where hosting the config.json hasn't been done yet, if the URL is empty,
				// generate the config locally.
				if (string.IsNullOrEmpty(_bootstrapURL))
				{
					GenerateEditorBootstrap();
					return;
				}
				else
#endif
				{
					Debug.Log($"Fetching {platformConfigURL}");
					Uri bootstrapURL = new Uri(platformConfigURL);
					_request = UnityWebRequest.Get(bootstrapURL);
					if (_request==null) 
						Debug.LogError("<color=#ff8080>Bootstrap request is null.</color>");
					UnityWebRequestAsyncOperation requestAsyncOp = _request.SendWebRequest();
					requestAsyncOp.completed += ParseConfig;
				}
			}

			public void End()
			{
				if (_request!=null)
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
				if (_request!=null && (_request.result!=UnityWebRequest.Result.InProgress && _request.result!=UnityWebRequest.Result.Success))
#else
				if (_request!=null && (_request.isHttpError || _request.isNetworkError || (_request.downloadProgress < 1.0f && _request.isDone)))
#endif
					return _request.error;
				return "Loading Config...";
			}

			public bool IsDone()
			{
				return _isDone;
			}

			public float GetProgress()
			{
				if (_request!=null)
				{
					_lastProgress = _request.downloadProgress;

				}
				return _lastProgress;
			}

			private void ParseConfig(AsyncOperation asyncOp)
			{
				try
				{
					if (_request==null || _request.responseCode!=200)
					{
						_parseErrorString = $"<color=#ff8080>Failed to load {_request.url}   Response code: {_request.responseCode}</color>";
						_failedToParse = true;

#if UNITY_EDITOR
						// Something went wrong, but we're in the editor, so just try to keep going.
						GenerateEditorBootstrap();
						return;
#endif
					}
					else
					{
						string jsonString = ABUtilities.GetStringFromUTF8File(_request.downloadHandler.data);
						ABBootstrap bootstrap = JsonUtility.FromJson<ABBootstrap>(jsonString);  // convert json to our data structure
						if (bootstrap==null) 
							Debug.LogError($"<color=#ff8080>Failued to load {_request.url}   Bootstrap is null</color>");

						// Substitute the platform string so we don't have to modify the scriptable object per-platform when we build.
						bootstrap.cdnBundleUrl = bootstrap.cdnBundleUrl.Replace("{PLATFORM}", _buildData.platform);

						_configData.Add("Bootstrap", bootstrap);
						_isDone = true;
					}
				}
				catch (Exception e)
				{
					_failedToParse = true;
					if (_request==null)
					{
						_parseErrorString = $"<color=#ff8080>Failued to load {_request.url}   Bootstrap: null request</color>";
					}
#if UNITY_2020_1_OR_NEWER
					else if (_request.result!=UnityWebRequest.Result.InProgress && _request.result!=UnityWebRequest.Result.Success)
#else
					else if (_request.isHttpError || _request.isNetworkError)
#endif
					{
						_parseErrorString = $"<color=#ff8080>Failued to load {_request.url}   Response code: {_request.responseCode}   Bootstrap: {_request.error}</color>";
					}
					else
					{
						_parseErrorString = $"<color=#ff8080>Failued to load {_request.url}   Response code: {_request.responseCode}   Bootstrap: {e.Message}</color>";
					}
#if UNITY_EDITOR
					// Something went wrong, but we're in the editor, so just try to keep going.
					GenerateEditorBootstrap();
					return;
#endif
				}
				if (_failedToParse) 
					Debug.LogError(_parseErrorString);
			}

#if UNITY_EDITOR
			// If running in the editor and the bootstrapURL is empty, we just point to the runtime cache folder and work offline.
			// If running in the editor and the bootstrapURL is NOT empty, we try to load from there, but if anything goes wrong at all, we just point at the cache folder and keep going.
			private void GenerateEditorBootstrap()
			{
				Debug.LogError("Skipping Bootstrap config in Editor: " + _parseErrorString);
				_configData.Add("Bootstrap", new ABBootstrap() { cdnBundleUrl = "Not Set" });
				_isDone = true;
			}
#endif
		}
	}
}