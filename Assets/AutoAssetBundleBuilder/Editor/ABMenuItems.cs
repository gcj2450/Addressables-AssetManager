//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		// This class manages the ReachableGames menu.
		public class ABMenuItems
		{
			const string kBuildAllEnabledPlatformsToPublish   = "Tools/ReachableGames/AutoBuilder/Build [All Enabled Platforms] to Publish";
			const string kBuildCurrentPlatformForEditor       = "Tools/ReachableGames/AutoBuilder/Build [Current] for Editor";
			const string kBuildCurrentPlatformUpdateForEditor = "Tools/ReachableGames/AutoBuilder/Build [Current] Update and Play #%u";
			const string kOpenPublishBundleFolder             = "Tools/ReachableGames/AutoBuilder/Open Folder: Bundles to Publish";
			const string kOpenEditorBundleFolder              = "Tools/ReachableGames/AutoBuilder/Open Folder: Bundles for Editor";
			const string kClearEditorBundleCache              = "Tools/ReachableGames/AutoBuilder/Clear Cached Bundles for Editor";
			const string kRevealBuildConfigs                  = "Tools/ReachableGames/AutoBuilder/Reveal Build Configs";

			//-------------------
			// Build all the platforms you have enabled, drop all the bundles in a single folder hierarchy, and report the results.
			[MenuItem(kBuildAllEnabledPlatformsToPublish, false, 1)]
			static void DoBuildAllEnabledPlatformsToPublish()
			{
				if (EditorApplication.isPlaying==false && EditorApplication.isCompiling==false)
				{
					ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
					string buildVersion = ABBuildVersion.GetUpdatedVersion();
					Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTargets(buildVersion, buildConfig);  // get the list of build platforms
					ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets);
				}
				else
				{
					UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
				}
			}

			//-------------------
			// Build the CURRENT platform defined in ABEditorConfig, and drop the results in the runtime folder.
			[MenuItem(kBuildCurrentPlatformForEditor, false, 101)]
			static void DoBuildCurrentPlatformForEditor()
			{
				if (EditorApplication.isPlaying==false && EditorApplication.isCompiling==false)
				{
					ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
					ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kEditorConfigPath);
					if (fakeBuild==null) 
						Debug.LogError("Please create an ABEditorConfig asset for setting platform and version on play (ReachableGames->AutoBuilder->Reveal Build Configs).");
					Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTargets(fakeBuild.buildVersion, buildConfig);  // get the list of build platforms
					if (allTargets.ContainsKey(fakeBuild.platform)==false) 
						Debug.LogError("Current platform set in ABEditorConfig is ["+fakeBuild.platform+"] but is not enabled in ABBuildConfig");

					// We want to build directly into the Cache folder, which ends up being Application.persistentDataPath+"/Bundles" typically
					ABUtilities.ConfigureCache();

					// Build all the bundles for this specific platform directly into a runnable place.
					string outputPath = ABUtilities.GetRuntimeCacheFolder() + "/";
					ABBuildInfo realBuildInfo = allTargets[fakeBuild.platform];
					ABBundleBuilder.DoBuildBundles(realBuildInfo.SrcBundleFolder, fakeBuild.buildVersion, outputPath, realBuildInfo.BundleOptions, realBuildInfo.Target, fakeBuild.platform, realBuildInfo.Logging, realBuildInfo.IgnoreEndsWith, realBuildInfo.IgnoreContains, realBuildInfo.IgnoreExact);
					UnityEngine.Debug.Log("<color=#8080ff>Current Platform bundles built successfully for editor.</color>");
				}
				else
				{
					UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
				}
			}

			//-------------------
			// Build the CURRENT platform defined in ABEditorConfig
			[MenuItem(kBuildCurrentPlatformUpdateForEditor, false, 102)]
			static void DoBuildCurrentPlatformUpdateForEditor()
			{
				if (EditorApplication.isPlaying==false && EditorApplication.isCompiling==false)
				{
					// Force the project to save.  If I don't do this, small changes do NOT get stored into the asset database, and do not get built into the override bundle.  Annoying.
					AssetDatabase.SaveAssets();

					ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
					ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kEditorConfigPath);
					if (fakeBuild==null) Debug.LogError("Please create an ABEditorConfig asset for setting platform and version on play (ReachableGames->AutoBuilder->Reveal Build Configs).");
					Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTargets(fakeBuild.buildVersion, buildConfig);  // get the list of build platforms
					if (allTargets.ContainsKey(fakeBuild.platform)==false) Debug.LogError("Current platform ["+fakeBuild.platform+"] not enabled");
			
					// Build the override bundle
					ABBundleBuilder.DoOverrideBundleBuild(allTargets[fakeBuild.platform], fakeBuild);
					UnityEngine.Debug.Log("<color=#8080ff>Override bundle built successfully, entering Play mode.</color>");

					//EditorApplication.isPlaying = true;
				}
				else
				{
					UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
				}
			}

			//-------------------
			// This tries to open the exact folder you are currently using in the editor, so it is easy to access the bundles for current builds.
			[MenuItem(kOpenPublishBundleFolder, false, 201)]
			static private void DoOpenPublishBundleFolder()
			{
				string folder = ABEditorUtilities.GetBuildRootFolder();
				ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kEditorConfigPath);
				if (fakeBuild!=null)
					folder = ABEditorUtilities.GetBundleBuildFolder(fakeBuild.platform);
				
				if (Directory.Exists(folder)==false)
					Directory.CreateDirectory(folder);
				System.Diagnostics.Process.Start(folder);
			}

			//-------------------
			// This opens the folder where the editor will look when running.  If the bundles are already present with the correct version, they will be used rather than downloaded.
			// This lets you test things locally before publishing if you like.
			[MenuItem(kOpenEditorBundleFolder, false, 202)]
			static private void DoOpenEditorBundleFolder()
			{
				string folder = ABUtilities.GetRuntimeCacheFolder();
				if (Directory.Exists(folder)==false)
					Directory.CreateDirectory(folder);
				System.Diagnostics.Process.Start(folder);
			}

			//-------------------
			// Clear the current cached bundles.
			[MenuItem(kClearEditorBundleCache, false, 203)]
			static public void DoClearCachedBundlesForEditor()
			{
				Directory.Delete(ABUtilities.GetRuntimeCacheFolder(), true);
			}

			//-------------------

			[MenuItem(kRevealBuildConfigs, false, 204)]
			static public void RevealBuildConfigs()
			{
				// Change this if you prefer to move all 3rdParty software someplace other than the root of your project.  Most people do.
				string kInstallFolder = ABEditorUtilities.GetInstallFolder();
				string kAssetDBInstallFolder = "Assets" + kInstallFolder + "/Editor/Resources/";

				if (Directory.Exists(Application.dataPath + kInstallFolder + "/Editor/Resources")==false)
				{
					Directory.CreateDirectory(Application.dataPath + kInstallFolder + "/Editor/Resources");
					AssetDatabase.Refresh();
				}
		
				string fullBuildConfigPath = kAssetDBInstallFolder+"/"+ABBuildConfig.kBuildConfigPath+".asset";
				string fullBuildVersionPath = kAssetDBInstallFolder+"/"+ABBuildVersion.kVersionPath+".asset";
				string fullFakeBuildDataPath = kAssetDBInstallFolder+"/"+ABBuild.kEditorConfigPath+".asset";
		
				ABBuildConfig config = Resources.Load<ABBuildConfig>(ABBuildConfig.kBuildConfigPath);
				if (config==null)
				{
					config = new ABBuildConfig();
					ABEditorUtilities.CreateScriptableObjectAsset(config, fullBuildConfigPath);
				}

				ABBuildVersion version = Resources.Load<ABBuildVersion>(ABBuildVersion.kVersionPath);
				if (version==null)
				{
					version = new ABBuildVersion();
					ABEditorUtilities.CreateScriptableObjectAsset(version, fullBuildVersionPath);
				}

				// Make a fake build resouce so you can select the specific platform and version of bundles to run with when you hit play in Editor.
				ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kEditorConfigPath);
				if (fakeBuild==null)
				{
					fakeBuild = ScriptableObject.CreateInstance<ABBuild>();
					fakeBuild.platform = ABEditorUtilities.GetPlatform(EditorUserBuildSettings.activeBuildTarget);
					fakeBuild.buildVersion = version.ToString();
					ABEditorUtilities.CreateScriptableObjectAsset(fakeBuild, fullFakeBuildDataPath);
				}

				AssetDatabase.Refresh();
				Selection.activeObject = config;
			}
		}
	}
}