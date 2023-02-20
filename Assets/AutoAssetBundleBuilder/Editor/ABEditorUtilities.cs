//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		public class ABEditorUtilities
		{
			// Clean up filenames so they are valid for the file system.
			static public string ReplaceInvalidFilenameChars(string filename)
			{
				string output = filename;
				foreach (char c in Path.GetInvalidFileNameChars())
				{
					string cstr = "" + c;
					output.Replace(cstr, "");
				}
				return output;
			}

			//-------------------
			// We can't get at the bits of the Hash128, annoyingly, and we want to save a crapload of space in the manifest, so just use
			// the string of the Hash128 and generate a simple hash from the string.
			static public int HashOfHash128(Hash128 h)
			{
				string s = h.ToString();
				int r = 0;
				for (int i=0; i<32; i++)
				{
					char c = s[i];
					int cv = (int)(c-'0');  // parse the hex string
					if (cv>9)
						cv -= 39;  // handle 'a'-'f'
					r = r ^ (cv << i);  // blend/xor the original values into a single 32 bit value
				}
				return r;
			}

			//-------------------
			// This lets us get the build target string, which should show up as the matching string above at runtime.
			static public string GetPlatform(BuildTarget bt)
			{
				switch (bt)
				{
					case BuildTarget.StandaloneWindows:
						return "win32";
					case BuildTarget.StandaloneWindows64:
						return "win64";
					case BuildTarget.WSAPlayer:
						return "winstore";
					case BuildTarget.StandaloneOSX:
						return "osx";
					case BuildTarget.StandaloneLinux64:
						return "linux64";
					case BuildTarget.Android:
						return "android";
					case BuildTarget.iOS:
						return "ios";
					case BuildTarget.tvOS:
						return "tvos";
					case BuildTarget.WebGL:
						return "webgl";
					case BuildTarget.PS4:
						return "ps4";
					case BuildTarget.Switch:
						return "switch";
					case BuildTarget.XboxOne:
						return "xboxone";
					case BuildTarget.Lumin:
						return "magicleap";
#if !UNITY_2019_2_OR_NEWER
					case BuildTarget.StandaloneLinux:
						return "linux32";
					case BuildTarget.StandaloneLinuxUniversal:
						return "linuxuniv";
#endif
#if UNITY_2019_3_OR_NEWER
					case BuildTarget.Stadia:
						return "stadia";
#endif
#if UNITY_2019_4_OR_NEWER
					case BuildTarget.PS5:
						return "ps5";
					case BuildTarget.CloudRendering:
						return "cloud";
					case BuildTarget.GameCoreXboxOne:
						return "gamecorexboxone";
					case BuildTarget.GameCoreXboxSeries:
						return "gamecorexboxseries";
#endif
				}
				Debug.Assert(false, "Requested platform is unknown: "+bt);
				return "unknown";
			}

			//-------------------
			// Here's how you get the list of scenes from BuildSettings window.
			static public void GetBuildSettingsSceneList(List<string> scenes)
			{
				for (int i=0; i<SceneManager.sceneCountInBuildSettings; i++)
				{
					string path = SceneUtility.GetScenePathByBuildIndex(i);
					scenes.Add(path);
				}
			}

			//-------------------
			// Making a scriptable object requires specific steps.
			static public void CreateScriptableObjectAsset(ScriptableObject asset, string path)
			{
				AssetDatabase.CreateAsset(asset, path);
				AssetDatabase.SaveAssets();
			}

			//-------------------
			// Easier to configure stuff if it's broken out in an obvious place like this.
			static public string GetInstallFolder()
			{
				// Allow user to move things around without having to change the code
				string[] autobuilderFolder = Directory.GetDirectories(Application.dataPath, "AutoBuilder", SearchOption.AllDirectories);
				if (autobuilderFolder.Length == 0)
					return "/ReachableGames/AutoBuilder";  // If you rename the folder, we can't find it.  You can change this to explicitly set where you want Resources to go.
				string installFolder = autobuilderFolder[0].Replace(Application.dataPath, "");
				installFolder = installFolder.Replace('\\', '/');
				return installFolder;
			}

			// The build root is NEXT TO /Assets, in /Build
			static public string GetBuildRootFolder()
			{
				string buildRoot = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')) + "/Build/";
				return buildRoot;
			}

			// 
			static public string GetPlayerBuildFolder(string platformStr)
			{
				string playerPath = GetBuildRootFolder() + platformStr;
				return playerPath;
			}

			// config_platform.json files are typically written out in their own folder parallel to the builds, for easy uploading to a web server
			static public string GetConfigBuildFolder()
			{
				string configPath = GetBuildRootFolder();
				return configPath;
			}

			static public string GetEmbedBundlesFolder()
			{
				string embedPath = "/bundles/";
				return embedPath;
			}

			// Output bundles are usually in a subdirectory of where the player is built, to keep it all together.
			static public string GetBundleBuildFolder(string platformStr)
			{
				string bundlePath = GetBuildRootFolder() + "/bundles_" + platformStr;
				return bundlePath;
			}

			// This appends appropriate suffixes or prefixes to a filename to make it work for each platform.
			static public string CreateExecutableName(BuildTarget platform)
			{
				string productName = ReplaceInvalidFilenameChars(Application.productName);
				switch (platform)
				{
					case BuildTarget.WSAPlayer:
					case BuildTarget.StandaloneWindows:
					case BuildTarget.StandaloneWindows64:
						return productName + ".exe";
					case BuildTarget.Android:
						return productName + ".apk";
					default:
						return productName;  // nothing special
				}
			}
		}
	}
}
