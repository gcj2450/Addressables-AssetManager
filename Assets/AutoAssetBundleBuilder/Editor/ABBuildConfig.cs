using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace ZionGame
{
    // Generally, a player is built with exactly matching data.  This is designed so that:
    // 1) Config file simply points to the right folders to load asset bundles from, so you can change CDNs if you like.
    // 2) Player will have a version number burned a scriptable object in its Resources folder that points to the manifest it should read.
    // 3) Manifest files have a version number in their filename,
    // so they fully describe the data files that went with that version (may be shared with old or new builds)
    // 4) Older and newer builds will function side-by-side with each other as long as you don't delete or rename any asset bundles or manifests.

    // Place an ABBuildConfig in an Editor/Resources folder.
    // It is really only there to make it easy to configure and generate builds--the asset is not used at runtime.
    // Name this asset "ABBuildConfig"
    public class ABBuildConfig : ScriptableObject
    {
        static public string kBuildConfigPath = "ABBuildConfig";

        [Header("Runtime Config")]
        [Tooltip("You can add fields to the ABBootstrap class if you want.  " +
            "This is the first file loaded on startup, which you can use to set URLs to server endpoints, for example.")]
        public ABBootstrap bootstrap = new ABBootstrap();

        [Header("Build Config")]
        [Tooltip("This indicates the root folder where you want each subfolder to become an asset bundle." +
            "\nFormat it with slashes before and after as such: /Bundles/\nThis will expand to Assets/Bundles/...")]
        public string sourceBundleFolder = "/Bundles/";

        [Tooltip("Turn this on to see asset bundle logging.")]
        public bool doLogging = true;

        public PlatformOptions[] platforms = new PlatformOptions[]
        {
                new PlatformOptions(BuildTarget.StandaloneWindows),
                new PlatformOptions(BuildTarget.StandaloneWindows64),
                new PlatformOptions(BuildTarget.WSAPlayer),
                new PlatformOptions(BuildTarget.StandaloneOSX),
                new PlatformOptions(BuildTarget.StandaloneLinux64),
                new PlatformOptions(BuildTarget.WebGL),
                new PlatformOptions(BuildTarget.Android),
                new PlatformOptions(BuildTarget.iOS),
                new PlatformOptions(BuildTarget.tvOS),
                new PlatformOptions(BuildTarget.PS4),
                new PlatformOptions(BuildTarget.XboxOne),
                new PlatformOptions(BuildTarget.Switch),
                new PlatformOptions(BuildTarget.Lumin),
                new PlatformOptions(BuildTarget.Stadia),
                new PlatformOptions(BuildTarget.CloudRendering),
                new PlatformOptions(BuildTarget.PS5),
                new PlatformOptions(BuildTarget.GameCoreXboxOne),
                new PlatformOptions(BuildTarget.GameCoreXboxSeries),
        };

        [Header("Ignoring Assets")]
        [Tooltip("Any assets whose full path EndsWith any entries here will NOT be added to an asset bundle.  " +
            "Only use lowercase and forward slashes.  Examples: .pdf or _ignored.asset")]
        public string[] ignoreEndsWith;

        [Tooltip("Any assets whose path that Contains any entries here will NOT be added to an asset bundle.  " +
            "Only use lowercase and forward slashes.  Examples: /ignore/ or _nobundle")]

        public string[] ignoreContains;
        [Tooltip("Explicit full path of an asset can be added here, but not generally recommended, " +
            "since you can't easily reorganize files in folders.  " +
            "Only use lowercase and forward slashes.  Example: assets/bundles/bundlex/filename.png")]

        public string[] ignoreExact;

        // Each platform gets its own unique space to set options.
        [Serializable]
        public class PlatformOptions
        {
            // This must come first to show up in the Inspector as the drop-down label.
            public string Name;
            public BuildTarget Platform;

            // This controls whether this platform is AVAILABLE, and if they someone does a Build All, or even an explicit Build xxxxx, 
            // if this is disabled in the ScriptableObject, it will not build.  So these are pretty important to set for your project.
            // If you really want to build everything, leave everything on.  Unity gives us no way to know what platforms you have installed, 
            // though, which is why it is done this way.
            [Tooltip("If enabled, this platform will be built.  You must have the appropriate SDK installed for this to work, obviously.")]
            public bool enabled = true;

            [Tooltip("If enabled, the asset bundles will be embedded in the build in /StreamingAssets/ " +
                "and the executable will NOT attempt to load anything from the internet.")]
            public bool selfContainedBuild = false;

            // This creates the default options for all platforms.  If you want to manually set options per-platform, do it in the code below explicitly.
            // Some of these are disabled because there's no real use in using them.
            [Space]
            [Header("Asset Bundles Options")]
            public bool Uncompressed = false;
            public bool ForceRebuild = false;
            public bool DisableWriteTypeTree = false;
            public bool IgnoreTypeTreeChanges = false;
            public bool ChunkBasedCompression = true;
            public bool AssetBundleStripUnityVersion = true;
            public bool StrictMode = true;

            // This feature is always enabled, according to the docs, so no need to expose it.
            //				public bool DeterministicAssetBundle = true;
            public bool DisableLoadAssetByFileName = true;
            public bool DisableLoadAssetByFileNameWithExtension = true;

            // This creates the default options for building the player for all platforms.
            // Again, if you want something specific for just one platform, do that in code below.
            [Space]
            [Header("Player Build Options")]
            public bool ExportProject = true;    // for android and ios to export without building

            [Space]
            [Header("Player Debugging")]
            [Tooltip("If this is off, AutoBuilder ignores most of the rest of these settings!")]
            public bool Development = true;
            public bool AllowDebugging = true;
            public bool ConnectWithProfiler = false;
            public bool EnableDeepProfiling = false;
            public bool EnableCodeCoverage = false;
            [Tooltip("This is confusingly named.  It waits for a networked player to connect to the process.")]
            public bool WaitForPlayerConnection = false;
            [Tooltip("When enabled, logging goes out to the Editor console, but you can't attach a debugger.")]
            public bool ConnectToEditor = false;
            [Tooltip("For this to work, enable Development and AllowDebugging.")]
            public bool WaitForDebuggerConnection = false;
            public bool EnableAssertions = false;
            public bool IncludeTestAssemblies = false;
            public bool ShaderLiveLink = false;

            [Space]
            [Header("Player Packaging")]
            public bool StrictPlayerMode = true;
            public bool DetailedBuildReport = true;
            [Tooltip("Only matters on iOS builds.")]
            public bool SymlinkLibraries = false;
            public bool UncompressedPlayerAssetBundle = false;  // faster iteration time, but larger download
            public bool CompressLz4 = false;
            public bool CompressLz4High = true;
            public bool NoPlayerGUID = true;
            public bool ComputeCRC = true;
            public bool ScriptsOnly = false;
            public bool ServerBuild = false;

            //-------------------
            // This combines the settings saved in the ABBuildConfig scriptable object to control the build of asset bundles.
            public BuildAssetBundleOptions GenerateBundleOptionsFromSettings()
            {
                BuildAssetBundleOptions options = BuildAssetBundleOptions.None;
                if (Uncompressed)
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                if (DisableWriteTypeTree)
                    options |= BuildAssetBundleOptions.DisableWriteTypeTree;
                //					if (DeterministicAssetBundle)
                //						options |= BuildAssetBundleOptions.DeterministicAssetBundle;
                if (ForceRebuild)
                    options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
                if (IgnoreTypeTreeChanges)
                    options |= BuildAssetBundleOptions.IgnoreTypeTreeChanges;
                if (ChunkBasedCompression)
                    options |= BuildAssetBundleOptions.ChunkBasedCompression;
                if (StrictMode)
                    options |= BuildAssetBundleOptions.StrictMode;
                if (DisableLoadAssetByFileName)
                    options |= BuildAssetBundleOptions.DisableLoadAssetByFileName;
                if (DisableLoadAssetByFileNameWithExtension)
                    options |= BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
                if (AssetBundleStripUnityVersion)
                    options |= BuildAssetBundleOptions.AssetBundleStripUnityVersion;
                return options;
            }

            //-------------------
            // Combines the flags saved in the ABBuildConfig scriptable object to control the build of the Player package.
            public BuildOptions GeneratePlayerOptionsFromSettings()
            {
                BuildOptions options = BuildOptions.None;
                if (Development)
                    options |= BuildOptions.Development;
                if (ExportProject)
                    options |= BuildOptions.AcceptExternalModificationsToPlayer;
                if (ConnectWithProfiler)
                    options |= BuildOptions.ConnectWithProfiler;
                if (AllowDebugging)
                    options |= BuildOptions.AllowDebugging;
                if (SymlinkLibraries)
#if UNITY_2021_3_OR_NEWER
                    options |= BuildOptions.SymlinkSources;
#else
						options |= BuildOptions.SymlinkLibraries;
#endif
                if (UncompressedPlayerAssetBundle)
                    options |= BuildOptions.UncompressedAssetBundle;
                if (ConnectToEditor)
                    options |= BuildOptions.ConnectToHost;
#if !UNITY_2021_3_OR_NEWER
					if (ServerBuild)
						options |= BuildOptions.EnableHeadlessMode;
#endif
                if (ScriptsOnly)
                    options |= BuildOptions.BuildScriptsOnly;
                if (EnableAssertions)
                    options |= BuildOptions.ForceEnableAssertions;
                if (CompressLz4 && !CompressLz4High)
                    options |= BuildOptions.CompressWithLz4;
                if (CompressLz4High)
                    options |= BuildOptions.CompressWithLz4HC;
                if (StrictPlayerMode)
                    options |= BuildOptions.StrictMode;
                if (NoPlayerGUID)
                    options |= BuildOptions.NoUniqueIdentifier;
                if (ComputeCRC)
                    options |= BuildOptions.ComputeCRC;
                if (IncludeTestAssemblies)
                    options |= BuildOptions.IncludeTestAssemblies;
                if (ShaderLiveLink)
                    options |= BuildOptions.ShaderLivelinkSupport;
                if (WaitForPlayerConnection)
                    options |= BuildOptions.WaitForPlayerConnection;
                if (EnableCodeCoverage)
                    options |= BuildOptions.EnableCodeCoverage;
                if (EnableDeepProfiling)
                    options |= BuildOptions.EnableDeepProfilingSupport;
                if (DetailedBuildReport)
                    options |= BuildOptions.DetailedBuildReport;
                return options;
            }

            // Constructor to set name properly.
            public PlatformOptions(BuildTarget platform)
            {
                Platform = platform;
                Name = ABEditorUtilities.GetPlatform(platform);
            }
        }

        //-------------------

        /// <summary>
        /// 指定一个单独的平台打嵌入程序的包，不管配置表中激活与否
        /// </summary>
        /// <param name="_platformStr">传win64,android,ios其中一个</param>
        /// <param name="buildVersion">版本号</param>
        /// <param name="buildConfig"></param>
        /// <returns></returns>
        public Dictionary<string, ABBuildInfo> ConfigSelfContainBuildTarget(string _platformStr, string buildVersion, ABBuildConfig buildConfig)
        {
            if (string.IsNullOrEmpty(Application.productName))
                Debug.LogError("Product Name is not set.  Do this in Edit->ProjectSettings->Player");

            // Build a configuration dictionary with all the settings stored PER-PLATFORM, for easy extraction.
            Dictionary<string, ABBuildInfo> buildTargets = new Dictionary<string, ABBuildInfo>();
            foreach (PlatformOptions po in platforms)
            {
                if (_platformStr == "win64" && po.Platform == BuildTarget.StandaloneWindows64 ||
                   _platformStr == "android" && po.Platform == BuildTarget.Android ||
                   _platformStr == "ios" && po.Platform == BuildTarget.iOS)
                {
                    // If this platform is specified twice in an ENABLED state, this is an error.
                    string platformString = ABEditorUtilities.GetPlatform(po.Platform);

                    if (buildTargets.ContainsKey(platformString))
                        Debug.LogError("Same platform is enabled twice: " + platformString);

                    // Helpful paths to control where bundles and players go.
                    string playerBuildFolder = ABEditorUtilities.GetPlayerBuildFolder(buildVersion, platformString);

                    // dest folder for all configuration files, which tend to NOT go to the CDN, but instead to a web site
                    string configBuildFolder = ABEditorUtilities.GetConfigBuildFolder(buildVersion, platformString);
                    string bundleBuildFolder = ABEditorUtilities.GetBundleBuildFolder(buildVersion, platformString);  // dest asset bundles folder  
                    string absSrcBundlesFolder = Application.dataPath + buildConfig.sourceBundleFolder;

                    // If this is a selfContainedBuild, this is where we will put the config.json, manifest, and bundles just before building the Player.
                    string embedBundlesFolder = ABEditorUtilities.GetEmbedBundlesFolder();

                    // this cleans up your project name so it can be a valid filename
                    string exeName = ABEditorUtilities.CreateExecutableName(po.Platform);

                    // Here, we deal with random hackery to conform with Unity's build option restrictions.
                    if (po.Platform != BuildTarget.Android && po.Platform != BuildTarget.iOS)
                        po.ExportProject = false;
                    if (po.Platform != BuildTarget.iOS)
                        po.SymlinkLibraries = false;
                    if (po.Platform == BuildTarget.WebGL)
                        po.DisableWriteTypeTree = false;
                    if (po.Development == false)
                    {
                        po.AllowDebugging = false;
                        po.ConnectToEditor = false;
                        po.ConnectWithProfiler = false;
                        po.EnableDeepProfiling = false;
                        po.WaitForDebuggerConnection = false;
                    }
                    if (po.WaitForDebuggerConnection)
                    {
                        po.ConnectToEditor = false;  // seems like this interferes with the wait-for-debugger flag
                    }

                    // Generate the .json file contents from the basic config.
                    // If this is an embedded build, rewrite the path to the manifest.json so it works out properly.
                    // had to make a copy here or we would end up changing an asset on disk by accident.
                    ABBootstrap tmpBootstrap = new ABBootstrap(bootstrap);
                    tmpBootstrap.cdnBundleUrl = embedBundlesFolder;
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    // Generated straight from checkboxes in the ScriptableObject.
                    BuildOptions playerOptions = po.GeneratePlayerOptionsFromSettings();
                    BuildAssetBundleOptions bundleOptions = po.GenerateBundleOptionsFromSettings();
                    buildTargets.Add(platformString, new ABBuildInfo(po.Platform, platformString, playerOptions, bundleOptions,
                        playerBuildFolder, exeName, absSrcBundlesFolder, bundleBuildFolder, configBuildFolder,
                        configJson, true, po.WaitForDebuggerConnection, po.ServerBuild, embedBundlesFolder,
                        buildVersion, buildConfig.doLogging, buildConfig.ignoreEndsWith, buildConfig.ignoreContains, buildConfig.ignoreExact));
                }
            }

            return buildTargets;
        }

        /// <summary>
        /// 指定一个单独的平台打包，不管配置表中激活与否
        /// </summary>
        /// <param name="_platformStr">传win64,android,ios其中一个</param>
        /// <param name="buildVersion">版本号</param>
        /// <param name="buildConfig"></param>
        /// <param name="isBos"></param>
        /// <returns></returns>
        public Dictionary<string, ABBuildInfo> ConfigBuildTarget(string _platformStr,string buildVersion, ABBuildConfig buildConfig, bool isBos = false)
        {
            if (string.IsNullOrEmpty(Application.productName))
                Debug.LogError("Product Name is not set.  Do this in Edit->ProjectSettings->Player");

            // Build a configuration dictionary with all the settings stored PER-PLATFORM, for easy extraction.
            Dictionary<string, ABBuildInfo> buildTargets = new Dictionary<string, ABBuildInfo>();
            foreach (PlatformOptions po in platforms)
            {
                if (_platformStr == "win64"&&po.Platform== BuildTarget.StandaloneWindows64||
                   _platformStr == "android" && po.Platform == BuildTarget.Android ||
                   _platformStr == "ios" && po.Platform == BuildTarget.iOS)
                {
                    // If this platform is specified twice in an ENABLED state, this is an error.
                    string platformString = ABEditorUtilities.GetPlatform(po.Platform);

                    if (buildTargets.ContainsKey(platformString))
                        Debug.LogError("Same platform is enabled twice: " + platformString);

                    // Helpful paths to control where bundles and players go.
                    string playerBuildFolder = ABEditorUtilities.GetPlayerBuildFolder(buildVersion, platformString);

                    // dest folder for all configuration files, which tend to NOT go to the CDN, but instead to a web site
                    string configBuildFolder = ABEditorUtilities.GetConfigBuildFolder(buildVersion, platformString);
                    string bundleBuildFolder = ABEditorUtilities.GetBundleBuildFolder(buildVersion, platformString);  // dest asset bundles folder  
                    string absSrcBundlesFolder = Application.dataPath + buildConfig.sourceBundleFolder;

                    // If this is a selfContainedBuild, this is where we will put the config.json, manifest, and bundles just before building the Player.
                    string embedBundlesFolder = ABEditorUtilities.GetEmbedBundlesFolder();

                    // this cleans up your project name so it can be a valid filename
                    string exeName = ABEditorUtilities.CreateExecutableName(po.Platform);

                    // Here, we deal with random hackery to conform with Unity's build option restrictions.
                    if (po.Platform != BuildTarget.Android && po.Platform != BuildTarget.iOS)
                        po.ExportProject = false;
                    if (po.Platform != BuildTarget.iOS)
                        po.SymlinkLibraries = false;
                    if (po.Platform == BuildTarget.WebGL)
                        po.DisableWriteTypeTree = false;
                    if (po.Development == false)
                    {
                        po.AllowDebugging = false;
                        po.ConnectToEditor = false;
                        po.ConnectWithProfiler = false;
                        po.EnableDeepProfiling = false;
                        po.WaitForDebuggerConnection = false;
                    }
                    if (po.WaitForDebuggerConnection)
                    {
                        po.ConnectToEditor = false;  // seems like this interferes with the wait-for-debugger flag
                    }

                    // Generate the .json file contents from the basic config.
                    // If this is an embedded build, rewrite the path to the manifest.json so it works out properly.
                    // had to make a copy here or we would end up changing an asset on disk by accident.
                    ABBootstrap tmpBootstrap = new ABBootstrap(bootstrap);
                    if (po.selfContainedBuild)
                    {
                        tmpBootstrap.cdnBundleUrl = embedBundlesFolder;
                    }
                    else
                    {
                        tmpBootstrap.cdnBundleUrl = ABUtilities.GetResUrl(platformString, isBos);

                        //这里修改了configJson的值，添加了子文件夹和版本号文件夹
                        // ABBuildConfig.Bootstrap.cdnBundleUrl只需要填写服务器根地址即可
                        if (!tmpBootstrap.cdnBundleUrl.EndsWith("/"))
                            tmpBootstrap.cdnBundleUrl = tmpBootstrap.cdnBundleUrl + "/";

                        string rootFolder = ABUtilities.GetRuntimeSubFolder();
                        string versionFolder = buildVersion;
                        string platFormfolder = "bundles_{PLATFORM}";

                        //https://zion-sdk-download.baidu-int.com/download/yuanbang/
                        //https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/
                        //+BaseBundlesBuild/0.1.9/win64/bundles_{PLATFORM}
                        tmpBootstrap.cdnBundleUrl = tmpBootstrap.cdnBundleUrl + rootFolder + "/" + versionFolder + "/" + platformString + "/" + platFormfolder + "/";
                    }
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    // Generated straight from checkboxes in the ScriptableObject.
                    BuildOptions playerOptions = po.GeneratePlayerOptionsFromSettings();
                    BuildAssetBundleOptions bundleOptions = po.GenerateBundleOptionsFromSettings();
                    buildTargets.Add(platformString, new ABBuildInfo(po.Platform, platformString, playerOptions, bundleOptions,
                        playerBuildFolder, exeName, absSrcBundlesFolder, bundleBuildFolder, configBuildFolder,
                        configJson, po.selfContainedBuild, po.WaitForDebuggerConnection, po.ServerBuild, embedBundlesFolder,
                        buildVersion, buildConfig.doLogging, buildConfig.ignoreEndsWith, buildConfig.ignoreContains, buildConfig.ignoreExact));
                }
            }

            return buildTargets;
        }

        //-------------------
        /// <summary>
        /// 从配置文件中读取打包配置信息
        /// </summary>
        /// <param name="buildVersion"></param>
        /// <param name="buildConfig"></param>
        /// <returns></returns>
        public Dictionary<string, ABBuildInfo> ConfigBuildTargets(string buildVersion, ABBuildConfig buildConfig,bool isBos= false)
        {
            if (string.IsNullOrEmpty(Application.productName))
                Debug.LogError("Product Name is not set.  Do this in Edit->ProjectSettings->Player");

            // Build a configuration dictionary with all the settings stored PER-PLATFORM, for easy extraction.
            Dictionary<string, ABBuildInfo> buildTargets = new Dictionary<string, ABBuildInfo>();
            foreach (PlatformOptions po in platforms)
            {
                if (po.enabled)
                {
                    // If this platform is specified twice in an ENABLED state, this is an error.
                    string platformString = ABEditorUtilities.GetPlatform(po.Platform);
                    if (buildTargets.ContainsKey(platformString))
                        Debug.LogError("Same platform is enabled twice: " + platformString);

                    // Helpful paths to control where bundles and players go.
                    string playerBuildFolder = ABEditorUtilities.GetPlayerBuildFolder(buildVersion, platformString);

                    // dest folder for all configuration files, which tend to NOT go to the CDN, but instead to a web site
                    string configBuildFolder = ABEditorUtilities.GetConfigBuildFolder(buildVersion,platformString);
                    string bundleBuildFolder = ABEditorUtilities.GetBundleBuildFolder(buildVersion, platformString);  // dest asset bundles folder  
                    string absSrcBundlesFolder = Application.dataPath + buildConfig.sourceBundleFolder;

                    // If this is a selfContainedBuild, this is where we will put the config.json, manifest, and bundles just before building the Player.
                    string embedBundlesFolder = ABEditorUtilities.GetEmbedBundlesFolder();

                    // this cleans up your project name so it can be a valid filename
                    string exeName = ABEditorUtilities.CreateExecutableName(po.Platform);

                    // Here, we deal with random hackery to conform with Unity's build option restrictions.
                    if (po.Platform != BuildTarget.Android && po.Platform != BuildTarget.iOS)
                        po.ExportProject = false;
                    if (po.Platform != BuildTarget.iOS)
                        po.SymlinkLibraries = false;
                    if (po.Platform == BuildTarget.WebGL)
                        po.DisableWriteTypeTree = false;
                    if (po.Development == false)
                    {
                        po.AllowDebugging = false;
                        po.ConnectToEditor = false;
                        po.ConnectWithProfiler = false;
                        po.EnableDeepProfiling = false;
                        po.WaitForDebuggerConnection = false;
                    }
                    if (po.WaitForDebuggerConnection)
                    {
                        po.ConnectToEditor = false;  // seems like this interferes with the wait-for-debugger flag
                    }

                    // Generate the .json file contents from the basic config.
                    // If this is an embedded build, rewrite the path to the manifest.json so it works out properly.
                    // had to make a copy here or we would end up changing an asset on disk by accident.
                    ABBootstrap tmpBootstrap = new ABBootstrap(bootstrap);
                    if (po.selfContainedBuild)
                    {
                        tmpBootstrap.cdnBundleUrl = embedBundlesFolder;
                    }
                    else
                    {
                        tmpBootstrap.cdnBundleUrl = ABUtilities.GetResUrl(platformString, isBos);

                        //这里修改了configJson的值，添加了子文件夹和版本号文件夹
                        // ABBuildConfig.Bootstrap.cdnBundleUrl只需要填写服务器根地址即可
                        if (!tmpBootstrap.cdnBundleUrl.EndsWith("/"))
                            tmpBootstrap.cdnBundleUrl = tmpBootstrap.cdnBundleUrl + "/";

                        string rootFolder = ABUtilities.GetRuntimeSubFolder();
                        string versionFolder = buildVersion;
                        string platFormfolder = "bundles_{PLATFORM}";

                        //https://zion-sdk-download.baidu-int.com/download/yuanbang/
                        //https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/
                        //+BaseBundlesBuild/0.1.9/win64/bundles_{PLATFORM}
                        tmpBootstrap.cdnBundleUrl = tmpBootstrap.cdnBundleUrl + rootFolder + "/" + versionFolder+"/" +platformString+ "/" + platFormfolder + "/";
                    }
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    // Generated straight from checkboxes in the ScriptableObject.
                    BuildOptions playerOptions = po.GeneratePlayerOptionsFromSettings();
                    BuildAssetBundleOptions bundleOptions = po.GenerateBundleOptionsFromSettings();
                    buildTargets.Add(platformString, new ABBuildInfo(po.Platform, platformString, playerOptions, bundleOptions,
                        playerBuildFolder, exeName, absSrcBundlesFolder, bundleBuildFolder, configBuildFolder,
                        configJson, po.selfContainedBuild, po.WaitForDebuggerConnection, po.ServerBuild, embedBundlesFolder,
                        buildVersion, buildConfig.doLogging, buildConfig.ignoreEndsWith, buildConfig.ignoreContains, buildConfig.ignoreExact));
                }
            }

            return buildTargets;
        }

        //-------------------
        // This is where you can inject specific scenes to include per-platform, if needed.
        static public string[] GetSceneList(BuildTarget target)
        {
            List<string> sceneList = new List<string>();

            // Here, we pull the list of scenes from the BuildSettings window.
            // if you want to completely hard-code your scene list, comment this next line out
            ABEditorUtilities.GetBuildSettingsSceneList(sceneList);
            if (sceneList.Count == 0)
                Debug.LogError("<color=#ff8080>No scenes in the build settings list.  Aborting.</color>");

            switch (target)
            {
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.WSAPlayer:  // windows store
                case BuildTarget.Android:
                case BuildTarget.iOS:
                case BuildTarget.tvOS:  // appleTV
                case BuildTarget.WebGL:
                case BuildTarget.Switch:
                case BuildTarget.PS4:
                case BuildTarget.Lumin:  // MagicLeap
                case BuildTarget.Stadia:
                case BuildTarget.CloudRendering:
                case BuildTarget.PS5:
                case BuildTarget.GameCoreXboxOne:
                case BuildTarget.GameCoreXboxSeries:
                    break;
                case BuildTarget.XboxOne:
                    // add custom scenes per platform here, if you need to... eg.
                    //sceneList.Add("Assets/Scenes/XBoxSplashScreen");
                    break;
            }
            return sceneList.ToArray();  // default is just the scene list
        }

        //-------------------
        /// <summary>
        /// 获取Resources文件夹下的ABBuildConfig
        /// </summary>
        /// <returns></returns>
        static public ABBuildConfig LoadFromResource()
        {
            //加载Resources文件夹下的ABBuildConfig配置文件，包含build version, final manifest URL, etc.
            ABBuildConfig config = Resources.Load<ABBuildConfig>(ABBuildConfig.kBuildConfigPath);
            if (config == null) Debug.LogError("<color=#ff8080>No ABBuildConfig object found.</color>");
            return config;
        }
    }
}
