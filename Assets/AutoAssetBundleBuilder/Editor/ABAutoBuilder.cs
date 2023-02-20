//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.Build.Reporting;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		public class ABAutoBuilder
		{
			// You can launch one or multiple builds from the command line like so:
			// d:\Unity\2018.4.1f1\Editor\unity.exe -quit -batchmode -projectPath d:\project\path\ -executeMethod ReachableGames.ABAutoBuilder.BuildFromCommandLine -target=win32 -target=osx -target=win64
			//
			// You can launch ALL enabled build platforms like so:
			// d:\Unity\2018.4.1f1\Editor\unity.exe -quit -batchmode -projectPath d:\project\path\ -executeMethod ReachableGames.ABAutoBuilder.BuildFromCommandLine
			static public void BuildFromCommandLine()
			{
				ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
				string buildVersion = ABBuildVersion.GetUpdatedVersion();
				Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTargets(buildVersion, buildConfig);  // get the list of build platforms
				List<string> targetsToBuild = new List<string>();
				foreach (string arg in Environment.GetCommandLineArgs())
				{
					if (arg.StartsWith("-target=", StringComparison.InvariantCulture))
					{
						string target = arg.Remove(0, 8);
						if (allTargets.ContainsKey(target)==false) 
							Debug.LogError("<color=#ff8080>Target ["+target+"] not recognized OR not enabled.</color>");
						targetsToBuild.Add(target);
					}
				}

				// If none were specified, assume we mean ALL targets.
				if (targetsToBuild.Count==0)
					targetsToBuild.AddRange(allTargets.Keys);

				int failed = DoBuilds(targetsToBuild, allTargets);
				EditorApplication.Exit(failed);  // if any builds failed, this will be non-zero, which is an error in most operating systems
			}

			// This handles the noisy business of actually doing builds, printing out results, etc.  Returns the number that FAILED.
			static public int DoBuilds(List<string> buildTargets, Dictionary<string, ABBuildInfo> allTargets)
			{
				int builds = 0;
				int failed = 0;
				foreach (string target in buildTargets)
				{
					builds++;
				
					BuildResult buildResult = DoBuild(allTargets[target]);  // try the build

					if (buildResult!=BuildResult.Succeeded)  // count failures
						failed++;

					if (buildResult==BuildResult.Cancelled)  // quit all builds immediately if one was canceled
						break;
				}
				Debug.Log(failed+" builds failed out of "+builds);
				return failed;
			}

			static private BuildResult DoBuild(ABBuildInfo buildInfo)
			{
				if (Directory.Exists(buildInfo.DestFolder))
					Directory.Delete(buildInfo.DestFolder, true);  // nuke the dest folder--if you make multiple builds in a row and *remove* something from the build, it will still be there.
				Directory.CreateDirectory(buildInfo.DestFolder);

				Directory.CreateDirectory(buildInfo.DestBundleFolder);
				Directory.CreateDirectory(buildInfo.DestConfigFolder);
				Directory.CreateDirectory(Application.streamingAssetsPath);
				
				string absEmbedPath = Application.streamingAssetsPath + buildInfo.EmbedBundleFolder;
				if (Directory.Exists(absEmbedPath))
					Directory.Delete(absEmbedPath, true);  // nuke anything that might be still sitting there since last build

				// Write out the config file
				string configJson = buildInfo.ConfigJson;  // convert structure over to json
				string configPath = buildInfo.DestConfigFolder + "/config_"+ buildInfo.TargetString +".json";
				string configEmbedPath = Application.streamingAssetsPath + "/config_"+ buildInfo.TargetString +".json";
				File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

				// Write out the version file (for easy scripting)
				string versionPath = buildInfo.DestConfigFolder + "/version.txt";
				File.WriteAllText(versionPath, buildInfo.BuildVersion);

				// Modify the player version.  I think this will cause PlayerSettings.asset to get changed, unfortunately.
				PlayerSettings.bundleVersion = buildInfo.BuildVersion;

				// Construct the build player options from the BuildInfo that was previously configured.
				BuildPlayerOptions options = new BuildPlayerOptions();
				options.locationPathName = buildInfo.DestFolder + "/" + buildInfo.ExeFilename;
				options.scenes = ABBuildConfig.GetSceneList(buildInfo.Target);
				options.options = buildInfo.PlayerOptions;
				options.target = buildInfo.Target;
				options.targetGroup = BuildPipeline.GetBuildTargetGroup(buildInfo.Target);

				//-------------------
				// Patch in the platform string into the ABVersion and save it so it is stored correctly in the built player
				ABBuild existingBuild = Resources.Load<ABBuild>(ABBuild.kBuildPath);
				if (existingBuild!=null)
				{
					string fullPath = AssetDatabase.GetAssetPath(existingBuild);
					AssetDatabase.DeleteAsset(fullPath); 
				}
		
				// This drops the ABBuild object in /Assets/ReachableGames/AutoBuilder/Resources/ABBuild by default.  It only exists during the build 
				// of the player, and only exists so the player knows exactly which platform and build number it was given, so it can find its asset bundles on launch.
				string kInstallFolder = ABEditorUtilities.GetInstallFolder() + "/Resources";
				string kAssetDBInstallFolder = "Assets" + kInstallFolder;

				if (Directory.Exists(Application.dataPath + kInstallFolder)==false)
				{
					Directory.CreateDirectory(Application.dataPath + kInstallFolder);
					AssetDatabase.Refresh();
				}

				// generate the scriptable object that will hold our Build data
				string assetPathForBuildAsset = kAssetDBInstallFolder+"/"+ABBuild.kBuildPath+".asset";
				ABBuild buildObj = ScriptableObject.CreateInstance<ABBuild>();
				buildObj.platform = buildInfo.TargetString;
				buildObj.buildVersion = buildInfo.BuildVersion;			
				buildObj.selfContained = buildInfo.SelfContainedBuild;
				ABEditorUtilities.CreateScriptableObjectAsset(buildObj, assetPathForBuildAsset);
				AssetDatabase.Refresh();

				// This requires bundles to be build BEFORE the app, because the bundles need to be IN the app.
				BuildResult result = BuildResult.Failed;
				if (buildInfo.SelfContainedBuild)
				{
					// Build asset bundles AFTER we build the player successfully.
					if (ABBundleBuilder.DoBuildBundles(buildInfo.SrcBundleFolder, buildInfo.BuildVersion, buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString, buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact))
					{
						// Copy in the bundles to the right place in /StreamingAssets/
						Directory.CreateDirectory(absEmbedPath);
						File.Copy(configPath, configEmbedPath, true);

						// Copy all bundles.  This only works because there are no subdirectories.
						foreach(var file in Directory.GetFiles(buildInfo.DestBundleFolder))
							File.Copy(file, Path.Combine(absEmbedPath, Path.GetFileName(file)), true);

						// do the player build first
						try
						{
							result = DoBuildPlayer(options, buildInfo.TargetString, buildInfo.WaitForDebugger, buildInfo.ServerBuild);
						}
						finally
						{
							AssetDatabase.DeleteAsset(assetPathForBuildAsset);  // get rid of the build asset.  We never want that to exist on disk except during the Player build

							// Nuke the folder, so we don't leave garbage behind for the next build.
							Directory.Delete(absEmbedPath, true);  // nuke anything that might be still sitting there since last build
							File.Delete(absEmbedPath.TrimEnd('/','\\') + ".meta");  // clean up the meta files we produce while making a build
							File.Delete(configEmbedPath);
							File.Delete(configEmbedPath + ".meta");
						}
					}
				}
				else
				{
					// do the player build first
					//result = DoBuildPlayer(options, buildInfo.TargetString, buildInfo.WaitForDebugger, buildInfo.ServerBuild);
					AssetDatabase.DeleteAsset(assetPathForBuildAsset);  // get rid of the build asset.  We never want that to exist on disk except during the Player build

					//if (result==BuildResult.Succeeded)
					{
                        // Build asset bundles AFTER we build the player successfully.
                        bool status=ABBundleBuilder.DoBuildBundles(buildInfo.SrcBundleFolder, buildInfo.BuildVersion, buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString, buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact);
						result = status ? BuildResult.Succeeded : BuildResult.Failed;
                    }
				}
				return result;
			}

			// Performs the actual build work.
			static private BuildResult DoBuildPlayer(BuildPlayerOptions opt, string platformString, bool waitForDebugger, bool serverBuild)
			{
				// This is kind of a pain to find in the UI, and also in code.
				EditorUserBuildSettings.waitForManagedDebugger = waitForDebugger;

#if UNITY_2021_3_OR_NEWER
				EditorUserBuildSettings.standaloneBuildSubtarget = serverBuild ? StandaloneBuildSubtarget.Server : StandaloneBuildSubtarget.Player;
#endif

				BuildReport report = BuildPipeline.BuildPlayer(opt);
				BuildSummary summary = report.summary;
				BuildResult result = summary.result;

				if (result == BuildResult.Succeeded)
				{
					Debug.Log("<color=#8080ff><b>BUILD PLAYER SUCCESS! (platform="+platformString+")</b></color>!");
				}
				else if (result == BuildResult.Failed)
				{
					Debug.Log("<color=#ff8080><b>BUILD PLAYER FAILED! (platform="+platformString+")</b></color>!");
				}
				else if (result == BuildResult.Cancelled)
				{
					Debug.LogError("<b>BUILD PLAYER CANCELED!</b>");
				}
				else Debug.LogError("Unknown Build Status.");
				return result;
			}
		}
	}
}
