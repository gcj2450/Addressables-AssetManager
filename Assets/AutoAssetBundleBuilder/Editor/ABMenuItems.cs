using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BaiduFtpUploadFiles;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEditor.PlayerSettings;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Net;
using System.Text;

namespace ZionGame
{
    //打包的Bundle文件夹需要和项目的Assets文件夹同级
    //打包的时候修改ABBuildVersion内的版本号，
    //加载的时候修改ABLoader里的version字段即可

    /// <summary>
    /// 菜单项
    /// </summary>
    public class ABMenuItems
    {
        const string kBuildAllEnabledPlatformsToFtp = "Window/ABuilder/Build [All Enabled Platforms] to FTP";
        const string kBuildAllEnabledPlatformsToBos = "Window/ABuilder/Build [All Enabled Platforms] to Bos";
        const string kBuildCurrentPlatformForEditor = "Window/ABuilder/Build [Current] for Editor";
        const string kBuildCurrentPlatformUpdateForEditor = "Window/ABuilder/Build [Current] Update and Play #%u";
        const string kOpenPublishBundleFolder = "Window/ABuilder/Open Folder: Bundles to Publish";
        const string kOpenEditorBundleFolder = "Window/ABuilder/Open Folder: Bundles for Editor";
        const string kClearEditorBundleCache = "Window/ABuilder/Clear Cached Bundles for Editor";
        const string kRevealBuildConfigs = "Window/ABuilder/Reveal Build Configs";

        const string buildTestWin64 = "ET/资源发布/基础数资/测试环境/Build Win64 To Test And Push";
        const string buildTestAndroid = "ET/资源发布/基础数资/测试环境/Build Android To Test And Push";
        const string buildTestIOS = "ET/资源发布/基础数资/测试环境/Build IOS To Test And Push";

        const string buildDevWin64 = "ET/资源发布/基础数资/开发环境/Build Win64 To Dev And Push";
        const string buildDevAndroid = "ET/资源发布/基础数资/开发环境/Build Android To Dev And Push";
        const string buildDevIOS = "ET/资源发布/基础数资/开发环境/Build IOS To Dev Push";

        const string buildOnlineWin64 = "ET/资源发布/基础数资/线上环境/Build Win64 To Bos And Push";
        const string buildOnlineAndroid = "ET/资源发布/基础数资/线上环境/Build Android To Bos And Push";
        const string buildOnlineIOS = "ET/资源发布/基础数资/线上环境/Build IOS To Bos And Push";
        //-------------------
        // Build all the platforms you have enabled, drop all the bundles in a single folder hierarchy, and report the results.
        //打包并上传到Ftp
        //[MenuItem(kBuildAllEnabledPlatformsToFtp, false, 201)]
        static void DoBuildAllEnabledPlatformsToFtp()
        {
            BuildAndUpload(false);
            //if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            //{
            //    //从Resources文件夹内的ABBuildConfig读取配置
            //    ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
            //    //从Resources文件夹内的ABBuildVersion读取版本号
            //    string buildVersion = ABBuildVersion.GetUpdatedVersion();
            //    Debug.Log(buildVersion);
            //    // get the list of build platforms
            //    Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTargets(buildVersion, buildConfig);
            //    int failedCount = ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets);

            //    //如果都打包成功了，上传到ftp服务器
            //    if (failedCount == 0)
            //    {
            //        FtpUploaderHelper ftpUpHelper = new FtpUploaderHelper("10.27.209.219", "8021", "/", "tmbh", "anquan@123", false);

            //        //本地打包出的文件夹
            //        string localFolder = ABEditorUtilities.GetBuildRootFolder(buildVersion);
            //        Debug.Log("localFolder: " + localFolder);
            //        //项目文件夹
            //        string projectfolder = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/'));
            //        Debug.Log("projectfolder: " + projectfolder);
            //        //本地文件夹去掉项目文件夹前缀：BaseBundlesBuild/0.1.19/
            //        string remoteFolder = localFolder.Replace(projectfolder, "");
            //        remoteFolder = "/yuanbang" + remoteFolder;
            //        Debug.Log("remoteFolder: " + remoteFolder);
            //        //上传到服务器
            //        ftpUpHelper.UploadFolderAsync(localFolder, remoteFolder);
            //    }
            //}
            //else
            //{
            //    UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            //}
        }

        /// <summary>
        /// 打包并上传到百度Bos平台
        /// </summary>
        //[MenuItem(kBuildAllEnabledPlatformsToBos, false, 202)]
        static void DoBuildAllEnabledPlatformsToBos()
        {
            BuildAndUpload(true);
        }
        //============================测试环境
        /// <summary>
        /// 打包Win64到ftp
        /// </summary>
        [MenuItem(buildTestWin64, false, 1)]
        static void DoBuildTestWin64()
        {
            BuildAndUpload("win64",false,false);
        }

        /// <summary>
        /// 打包Android到ftp
        /// </summary>
        [MenuItem(buildTestAndroid, false, 2)]
        static void DoBuildTestAndroid()
        {
            BuildAndUpload("android", false,false);
        }

        /// <summary>
        /// 打包IOS到ftp
        /// </summary>
        [MenuItem(buildTestIOS, false,3)]
        static void DoBuildTestIOS()
        {
            BuildAndUpload("ios", false,false);
        }
        //============================测试环境

        //============================开发环境
        /// <summary>
        /// 打包Win64到开发环境
        /// </summary>
        [MenuItem(buildDevWin64, false, 1)]
        static void DoBuildDevWin64()
        {
            BuildAndUpload("win64", false,true);
        }

        /// <summary>
        /// 打包Android到开发环境
        /// </summary>
        [MenuItem(buildDevAndroid, false, 2)]
        static void DoBuildDevAndroid()
        {
            BuildAndUpload("android", false,true);
        }

        /// <summary>
        /// 打包IOS到开发环境
        /// </summary>
        [MenuItem(buildDevIOS, false, 3)]
        static void DoBuildDevIOS()
        {
            BuildAndUpload("ios", false,true);
        }
        //============================开发环境

        //============================线上Bos
        /// <summary>
        /// 打包Win64到Bos
        /// </summary>
        [MenuItem(buildOnlineWin64, false, 1)]
        static void DoBuildOnlineWin64()
        {
            BuildAndUpload("win64", true,false);
        }

        /// <summary>
        /// 打包Android到Bos
        /// </summary>
        [MenuItem(buildOnlineAndroid, false,2)]
        static void DoBuildOnlineAndroid()
        {
            BuildAndUpload("android", true,false);
        }

        /// <summary>
        /// 打包IOS到Bos
        /// </summary>
        [MenuItem(buildOnlineIOS, false, 3)]
        static void DoBuildOnlineIOS()
        {
            BuildAndUpload("ios", true,false);
        }
        //============================线上Bos

        /// <summary>
        /// 打包上传是否传到Bos平台，否则传到ftp
        /// </summary>
        /// <param name="isBos">是否上传Bos平台</param>
        static void BuildAndUpload(bool isBos)
        {
           
            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                //从Resources文件夹内的ABBuildConfig读取配置
                ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                //从Resources文件夹内的ABBuildVersion读取版本号
                string buildVersion = ABBuildVersion.GetUpdatedVersion();
                Debug.Log(buildVersion);
                // get the list of build platforms

                //删除已经存在的打包文件
                string buildRoot = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                if (Directory.Exists(buildRoot))
                {
                    Debug.Log("BuildAndUpload dir exist will delete: " + buildRoot);
                    Directory.Delete(buildRoot, true);
                }

                string currentPlatform = "";
#if UNITY_EDITOR
#if UNITY_ANDROID
                currentPlatform="android";
#elif UNITY_IOS
                currentPlatform="ios";
#elif UNITY_STANDALONE_WIN
                currentPlatform = "win64";
#endif
#endif
                Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTargets(buildVersion, buildConfig, isBos);

                //BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
                //if (activeTarget != BuildTarget.StandaloneWindows64)
                //{
                //    Debug.LogError("请先切换到win64平台");
                //    return;
                //}

                int failedCount = ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets);

                //如果都打包成功了，上传到服务器
                if (failedCount == 0)
                {
                    //本地打包出的文件夹，例如：D:/GitHub/Addressables-AssetManager/BaseBundlesBuild/0.1.19/
                    string localFolder = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                    Debug.Log("localFolder: " + localFolder);

                    //项目文件夹: D:/GitHub/Addressables-AssetManager
                    string projectfolder = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/'));
                    Debug.Log("projectfolder: " + projectfolder);

                    //本地文件夹去掉项目文件夹的值：BaseBundlesBuild/0.1.19/
                    string remoteFolder = localFolder.Replace(projectfolder, "");
                    Debug.Log("remoteFolder: " + remoteFolder);

                    //if (isBos)
                    //{
                    //    //最终服务器地址,最后是android还是ios和下面传的结果有关系：
                    //    //https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/
                    //    //最终文件地址：https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/BaseBundlesBuild/0.1.19/bundles_win64/manifest-0.1.19.json
                    //    BosUploaderHelper bosUploaderHelper = new BosUploaderHelper();

                    //    string[] dirs = Directory.GetDirectories(localFolder);
                    //    for (int i = 0,cnt=dirs.Length; i <cnt; i++)
                    //    {
                    //        DirectoryInfo directoryInfo = new DirectoryInfo(dirs[i]);
                    //        if (directoryInfo.Name=="win64")
                    //        {
                    //            Debug.Log("Upload to windows");
                    //            bosUploaderHelper.UploadToBos(localFolder, remoteFolder, "windows");
                    //        }
                    //        else if(directoryInfo.Name == "android")
                    //        {
                    //            Debug.Log("Upload to android");
                    //            bosUploaderHelper.UploadToBos(localFolder, remoteFolder, "android");
                    //        }
                    //        else if (directoryInfo.Name=="ios")
                    //        {
                    //            Debug.Log("Upload to ios");
                    //            bosUploaderHelper.UploadToBos(localFolder, remoteFolder, "ios");
                    //        }
                    //    }
                        
                    //}
                    //else
                    //{
                    //    FtpUploaderHelper ftpUpHelper = new FtpUploaderHelper("10.27.209.219", "8021", "/", "tmbh", "anquan@123", false);
                    //    remoteFolder = "/yuanbang" + remoteFolder;
                    //    Debug.Log("remoteFolder: " + remoteFolder);
                    //    //上传到服务器
                    //    ftpUpHelper.UploadFolderAsync(localFolder, remoteFolder);
                    //}
                }
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

        /// <summary>
        /// 打包指定的平台到线上或线下
        /// </summary>
        /// <param name="_platform">指定的平台：win64,ios,android</param>
        /// <param name="isBos">是否为线上</param>
        static void BuildAndUpload(string _platform,bool isBos,bool _isDev=false)
        {

            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                //从Resources文件夹内的ABBuildConfig读取配置
                ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                //从Resources文件夹内的ABBuildVersion读取版本号
                string buildVersion = ABBuildVersion.GetUpdatedVersion();
                Debug.Log(buildVersion);
                // get the list of build platforms

                //删除已经存在的打包文件
                string buildRoot = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                if (Directory.Exists(buildRoot))
                {
                    Debug.Log("BuildAndUpload dir exist will delete: " + buildRoot);
                    Directory.Delete(buildRoot, true);
                }

                BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;

                if (_platform=="win64"&& activeTarget != BuildTarget.StandaloneWindows64)
                {
                    Debug.LogError("请先切换到win64平台");
                    return;
                }
                if (_platform == "android" && activeTarget != BuildTarget.Android)
                {
                    Debug.LogError("请先切换到android平台");
                    return;
                }
                if (_platform == "ios" && activeTarget != BuildTarget.iOS)
                {
                    Debug.LogError("请先切换到ios平台");
                    return;
                }

                Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigBuildTarget(_platform, buildVersion, buildConfig, isBos);
                Debug.Log("AAAAAAAAAAA: " + allTargets.Count);
                //return;

                int failedCount = ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets);

                //如果都打包成功了，上传到服务器
                if (failedCount == 0)
                {
                    //本地打包出的文件夹，例如：D:/GitHub/Addressables-AssetManager/BaseBundlesBuild/0.1.19/
                    string localFolder = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                    Debug.Log("localFolder: " + localFolder);

                    //项目文件夹: D:/GitHub/Addressables-AssetManager
                    string projectfolder = Application.dataPath.Remove(Application.dataPath.LastIndexOf('/'));
                    Debug.Log("projectfolder: " + projectfolder);

                    //本地文件夹去掉项目文件夹的值：BaseBundlesBuild/0.1.19/
                    string remoteFolder = localFolder.Replace(projectfolder, "");
                    Debug.Log("remoteFolder: " + remoteFolder);

                    //VersionPushHelper versionPushHelper = new VersionPushHelper();
                    //if (isBos)
                    //{
                    //    //最终服务器地址,最后是android还是ios和下面传的结果有关系：
                    //    //https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/
                    //    //最终文件地址：https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/BaseBundlesBuild/0.1.19/bundles_win64/manifest-0.1.19.json
                    //    BosUploaderHelper bosUploaderHelper = new BosUploaderHelper();

                    //    string[] dirs = Directory.GetDirectories(localFolder);
                    //    for (int i = 0, cnt = dirs.Length; i < cnt; i++)
                    //    {
                    //        DirectoryInfo directoryInfo = new DirectoryInfo(dirs[i]);
                    //        if (directoryInfo.Name == "win64")
                    //        {
                    //            Debug.Log("Upload to windows");

                    //            versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, directoryInfo.Name, buildVersion, 
                    //                "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/windows/", isBos,false);

                    //            bosUploaderHelper.UploadToBos(localFolder, remoteFolder, "windows");
                    //        }
                    //        else if (directoryInfo.Name == "android")
                    //        {
                    //            Debug.Log("Upload to android");

                    //            versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, directoryInfo.Name, buildVersion, 
                    //                "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/", isBos,false);

                    //            bosUploaderHelper.UploadToBos(localFolder, remoteFolder, "android");
                    //        }
                    //        else if (directoryInfo.Name == "ios")
                    //        {
                    //            Debug.Log("Upload to ios");

                    //            versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, directoryInfo.Name, buildVersion, 
                    //                "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/", isBos,false);

                    //            bosUploaderHelper.UploadToBos(localFolder, remoteFolder, "ios");
                    //        }
                    //    }

                    //}
                    //else
                    //{
                    //    if (_isDev)
                    //    {
                    //        versionPushHelper.PushLatestBaseResVersion(
                    //            Application.version, _platform, buildVersion, "https://zion-sdk-download.baidu-int.com/download/yuanbang/", isBos,true);
                    //    }
                    //    else
                    //    {
                    //        versionPushHelper.PushLatestBaseResVersion(
                    //            Application.version, _platform, buildVersion, "https://zion-sdk-download.baidu-int.com/download/yuanbang/", isBos,false);
                    //    }
                    //    FtpUploaderHelper ftpUpHelper = new FtpUploaderHelper("10.27.209.219", "8021", "/", "tmbh", "anquan@123", false);
                    //    remoteFolder = "/yuanbang" + remoteFolder;
                    //    Debug.Log("remoteFolder: " + remoteFolder);
                    //    //上传到服务器
                    //    ftpUpHelper.UploadFolderAsync(localFolder, remoteFolder);
                    //}
                }
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

        //************************
        /// <summary>
        /// 打包Win64到嵌入包
        /// </summary>
        [MenuItem("ET/资源发布/基础数资/嵌入包/Win64")]
        static void DoBuildSelfWindows()
        {
            BuildSelContainedBundle("win64");
        }
        /// <summary>
        /// 打包Android到嵌入包
        /// </summary>
        [MenuItem("ET/资源发布/基础数资/嵌入包/Android")]
        static void DoBuildSelfAndroid()
        {
            BuildSelContainedBundle("android");
        }
        /// <summary>
        /// 打包IOS到嵌入包
        /// </summary>
        [MenuItem("ET/资源发布/基础数资/嵌入包/IOS")]
        static void DoBuildSelfIOS()
        {
            BuildSelContainedBundle("ios");
        }
        /// <summary>
        /// 打包到嵌入程序的Bundle
        /// </summary>
        /// <param name="_platform">指定的平台：win64,ios,android</param>
        static void BuildSelContainedBundle(string _platform)
        {
            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                //从Resources文件夹内的ABBuildConfig读取配置
                ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                //从Resources文件夹内的ABBuildVersion读取版本号
                //string buildVersion = ABBuildVersion.GetUpdatedVersion();
                //内置包版本号永远是0.0.0
                string buildVersion = "0.0.0";
                Debug.Log(buildVersion);
                // get the list of build platforms

                //删除已经存在的打包文件
                string buildRoot = ABEditorUtilities.GetBuildRootFolder(buildVersion);
                if (Directory.Exists(buildRoot))
                {
                    Debug.Log("BuildAndUpload dir exist will delete: " + buildRoot);
                    Directory.Delete(buildRoot, true);
                }

                BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;

                if (_platform == "win64" && activeTarget != BuildTarget.StandaloneWindows64)
                {
                    Debug.LogError("请先切换到win64平台");
                    return;
                }
                if (_platform == "android" && activeTarget != BuildTarget.Android)
                {
                    Debug.LogError("请先切换到android平台");
                    return;
                }
                if (_platform == "ios" && activeTarget != BuildTarget.iOS)
                {
                    Debug.LogError("请先切换到ios平台");
                    return;
                }

                Dictionary<string, ABBuildInfo> allTargets = buildConfig.ConfigSelfContainBuildTarget(_platform, buildVersion, buildConfig);
                Debug.Log("AAAAAAAAAAA: " + allTargets.Count);

                int failedCount = ABAutoBuilder.DoBuilds(new List<string>(allTargets.Keys), allTargets);
                Debug.Log("Build result: " + failedCount);
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

        //************************

        //-------------------
        // Build the CURRENT platform defined in ABEditorConfig, and drop the results in the runtime folder.
        //[MenuItem(kBuildCurrentPlatformForEditor, false, 301)]
        static void DoBuildCurrentPlatformForEditor()
        {
            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kBuildPath);
                if (fakeBuild == null)
                {
                    Debug.LogError("Please create an ABEditorConfig asset for setting platform and " +
                        "version on play (ReachableGames->AutoBuilder->Reveal Build Configs).");
                }

                // get the list of build platforms
                Dictionary<string, ABBuildInfo> allTargets =
                    buildConfig.ConfigBuildTargets(fakeBuild.buildVersion, buildConfig);

                if (allTargets.ContainsKey(fakeBuild.platform) == false)
                    Debug.LogError("Current platform set in ABEditorConfig is [" + fakeBuild.platform + "] but is not enabled in ABBuildConfig");

                // We want to build directly into the Cache folder, which ends up being Application.persistentDataPath+"/Bundles" typically
                ABUtilities.ConfigureCache(ABBuildVersion.GetUpdatedVersion());

                // Build all the bundles for this specific platform directly into a runnable place.
                string outputPath = ABUtilities.GetRuntimeCacheFolder(ABBuildVersion.GetUpdatedVersion()) + "/";
                ABBuildInfo realBuildInfo = allTargets[fakeBuild.platform];

                ABBundleBuilder.DoBuildBundles(realBuildInfo.SrcBundleFolder, fakeBuild.buildVersion,
                    outputPath, realBuildInfo.BundleOptions, realBuildInfo.Target, fakeBuild.platform, realBuildInfo.Logging,
                    realBuildInfo.IgnoreEndsWith, realBuildInfo.IgnoreContains, realBuildInfo.IgnoreExact);

                UnityEngine.Debug.Log("<color=#8080ff>Current Platform bundles built successfully for editor.</color>");
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

        //-------------------
        // Build the CURRENT platform defined in ABEditorConfig
        //[MenuItem(kBuildCurrentPlatformUpdateForEditor, false, 302)]
        static void DoBuildCurrentPlatformUpdateForEditor()
        {
            if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
            {
                // Force the project to save.  If I don't do this,
                // small changes do NOT get stored into the asset database,
                // and do not get built into the override bundle.  Annoying.
                AssetDatabase.SaveAssets();

                ABBuildConfig buildConfig = ABBuildConfig.LoadFromResource();
                ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kBuildPath);
                if (fakeBuild == null)
                {
                    Debug.LogError("Please create an ABEditorConfig asset for setting platform " +
                        "and version on play (ReachableGames->AutoBuilder->Reveal Build Configs).");
                }

                // get the list of build platforms
                Dictionary<string, ABBuildInfo> allTargets =
                    buildConfig.ConfigBuildTargets(fakeBuild.buildVersion, buildConfig);

                if (allTargets.ContainsKey(fakeBuild.platform) == false)
                    Debug.LogError("Current platform [" + fakeBuild.platform + "] not enabled");

                // Build the override bundle
                ABBundleBuilder.DoOverrideBundleBuild(allTargets[fakeBuild.platform], fakeBuild);
                UnityEngine.Debug.Log("<color=#8080ff>Override bundle built successfully, entering Play mode.</color>");

                EditorApplication.isPlaying = true;
            }
            else
            {
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
        }

        //-------------------
        // This tries to open the exact folder you are currently using in the editor, so it is easy to access the bundles for current builds.
        //[MenuItem(kOpenPublishBundleFolder, false, 401)]
        static private void DoOpenPublishBundleFolder()
        {
            string folder = ABEditorUtilities.GetBuildRootFolder(ABBuildVersion.GetUpdatedVersion());
            ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kBuildPath);
            if (fakeBuild != null)
                folder = ABEditorUtilities.GetBundleBuildFolder(ABBuildVersion.GetUpdatedVersion(), fakeBuild.platform);

            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(folder);
        }

        //-------------------
        // This opens the folder where the editor will look when running.
        // If the bundles are already present with the correct version, they will be used rather than downloaded.
        // This lets you test things locally before publishing if you like.
        //[MenuItem(kOpenEditorBundleFolder, false, 402)]
        static private void DoOpenEditorBundleFolder()
        {
            string folder = ABUtilities.GetRuntimeCacheFolder(ABBuildVersion.GetUpdatedVersion());
            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(folder);
        }

        //-------------------
        // Clear the current cached bundles.
        //[MenuItem(kClearEditorBundleCache, false, 403)]
        static public void DoClearCachedBundlesForEditor()
        {
            Directory.Delete(ABUtilities.GetRuntimeCacheFolder(ABBuildVersion.GetUpdatedVersion()), true);
        }

        //-------------------
        //[MenuItem(kRevealBuildConfigs, false, 404)]
        static public void RevealBuildConfigs()
        {
            // Change this if you prefer to move all 3rdParty software someplace other than the root of your project.  Most people do.
            string kInstallFolder = ABEditorUtilities.GetInstallFolder();
            string kAssetDBInstallFolder = "Assets" + kInstallFolder + "/Editor/Resources/";

            if (Directory.Exists(Application.dataPath + kInstallFolder + "/Editor/Resources") == false)
            {
                Directory.CreateDirectory(Application.dataPath + kInstallFolder + "/Editor/Resources");
                AssetDatabase.Refresh();
            }

            string fullBuildConfigPath = kAssetDBInstallFolder + "/" + ABBuildConfig.kBuildConfigPath + ".asset";
            string fullBuildVersionPath = kAssetDBInstallFolder + "/" + ABBuildVersion.kVersionPath + ".asset";
            string fullFakeBuildDataPath = kAssetDBInstallFolder + "/" + ABBuild.kBuildPath + ".asset";

            ABBuildConfig config = Resources.Load<ABBuildConfig>(ABBuildConfig.kBuildConfigPath);
            if (config == null)
            {
                config = new ABBuildConfig();
                ABEditorUtilities.CreateScriptableObjectAsset(config, fullBuildConfigPath);
            }

            ABBuildVersion version = Resources.Load<ABBuildVersion>(ABBuildVersion.kVersionPath);
            if (version == null)
            {
                version = new ABBuildVersion();
                ABEditorUtilities.CreateScriptableObjectAsset(version, fullBuildVersionPath);
            }

            // Make a fake build resouce so you can select the specific platform and version of bundles to run with when you hit play in Editor.
            ABBuild fakeBuild = Resources.Load<ABBuild>(ABBuild.kBuildPath);
            if (fakeBuild == null)
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
