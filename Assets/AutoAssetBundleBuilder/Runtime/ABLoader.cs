//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReachableGames
{
	namespace AutoBuilder
	{
		public class ABLoader : MonoBehaviour 
		{
			private List<ABILoadingState>       _states = new List<ABILoadingState>();
			private Dictionary<string, ABIData> _configData = new Dictionary<string, ABIData>();
			private int                         _currentState = 0;

			[Tooltip("Full URL to the config file, for example https://mycdn.example.com/config_{PLATFORM}.json\nThis can be left blank while testing in the Editor as long as you build your own bundles.\nIf building a self-contained executable with bundles embedded, leave this blank as well.")]
			public string BootstrapURL = "";
			[Tooltip("This is the NAME of the scene, not a path or filename, like: ABFirstScene")]
			public string NextSceneToLoad;

			public Image LoadingBar = null;
			public Text  LoadingText = null;

			void Start ()
			{
				// Figure out our built platform string and version, and stuff it into the config dictionary.
#if UNITY_EDITOR
				ABBuild build = Resources.Load<ABBuild>(ABBuild.kEditorConfigPath);
				if (build==null) 
					Debug.LogError("<color=#ff8080>Playing in the editor requires a specific platform to test.  To generate, go to: Tools->ReachableGames->AutoBuilder->RevealBuildConfigs and configure ABEditorConfig</color>");
#else
				// Actual builds must have build info to load bundles and work properly.
				ABBuild build = Resources.Load<ABBuild>(ABBuild.kBuildPath);
				if (build==null) 
					Debug.LogError("<color=#ff8080>No ABBuild object found in build.</color>");
#endif

				// go ahead and load the asset bundles off the web
				_configData.Add("BuildData", build);
			
				// Configure the bundle cache
				ABUtilities.ConfigureCache();

				// Figure out the protocol prefix.  For normal web fetching, it's baked into the BootstrapURL already.  For self-contained, it depends on the platform.  For editor running, we always use whatever is cached locally (built, normally, but sometimes fetched from web).
				string configURL = BootstrapURL;
				string bundleFolderPrefix = string.Empty;

				// Different load scheme for local-only.  Easier to separate it than bury it into the rest of the code path.
				if (build.selfContained)
				{
					// Android already comes with "jar:file://", but everything else is just raw path.
					bundleFolderPrefix = Application.streamingAssetsPath;
#if !UNITY_ANDROID
					bundleFolderPrefix = "file://" + bundleFolderPrefix;
#endif
					bundleFolderPrefix.TrimEnd('/');  // guarantee this does not end in a slash.

					configURL = bundleFolderPrefix + "/config_" + build.platform + ".json";
				}

				// Load the config_{PLATFORM}.json file first.  This may pull it from the /StreamingAssets/ folder if self-contained, or from the web.  
				// Inside it is the path to the CDN where the manifest and bundles are stored.
				_states.Add(new ABLoadingStateBootstrap(configURL));

				string overrideManifestPath = "";
#if UNITY_EDITOR
				// If we want to run with override bundles, go ahead and load it last.  If it's missing, ignore it.
				overrideManifestPath = ABUtilities.GetRuntimeCacheFolder() + "/override-"+build.buildVersion+".json";
#endif
				_states.Add(new ABLoadingStateManifest(overrideManifestPath, bundleFolderPrefix));
				_states.Add(new ABLoadingStateAssetBundles(bundleFolderPrefix));
				_states.Add(new ABLoadingStateNextScene(NextSceneToLoad));

				// Start the first state here
				_currentState = 0;
				_states[_currentState].Begin(_configData);
			}
	
			void Update ()
			{
				try
				{
					if (_currentState < _states.Count)  // if there's more to do here, keep doing it.  The final state loads the next scene and kills this one.
					{
						ABILoadingState state = _states[_currentState];

						// Update the loading bar (note, this is scaling a black bar from 1.0 down to 0.0, aligned to the right side, so it looks like a fancy bar is growing.
						LoadingText.text = state.GetStateText();
						float progressMinForState = _currentState/(float)_states.Count;
						float progressMaxForState = (_currentState+1)/(float)_states.Count;
						float currentProgress = Mathf.Lerp(progressMinForState, progressMaxForState, state.GetProgress());
					
						LoadingBar.fillAmount = currentProgress;  // let the image fill handle the loading bar

						if (state.IsDone())  // if we succeeded, end the current state and start the next one, or launch the next scene if we're done.
						{
							state.End();
							_currentState++;
							if (_currentState < _states.Count)  // when we are done starting states, this update will just spin forever.  That's ok, the last state kills this scene.
							{
								_states[_currentState].Begin(_configData);
							}
						}
					}
				}
				catch (Exception e)
				{
					Debug.Log("Exception caught: " + e);
					Application.Quit();
#if UNITY_EDITOR
					EditorApplication.isPlaying = false;
#endif
				}
			}
		}
	}
}