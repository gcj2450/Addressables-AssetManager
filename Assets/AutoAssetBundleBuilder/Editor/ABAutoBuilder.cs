using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor.Build.Reporting;
using BaiduFtpUploadFiles;

namespace ZionGame
{
    public class ABAutoBuilder
    {
        // You can launch one or multiple builds from the command line like so:
        // d:\Unity\2018.4.1f1\Editor\unity.exe -quit -batchmode -projectPath d:\project\path\ -executeMethod ReachableGames.ABAutoBuilder.BuildFromCommandLine -target=win32 -target=osx -target=win64
        //
        // You can launch ALL enabled build platforms like so:
        // "C:\Program Files\Unity\Hub\Editor\2021.3.6f1c1\Editor\Unity.exe" -quit -batchmode -projectPath D:\baidu\ZionBase\zionsdk-unity-demo\ -executeMethod ZionGame.ABAutoBuilder.BuildIosFromCmd
        // "C:\Program Files\Unity\Hub\Editor\2021.3.6f1c1\Editor\Unity.exe" -quit -batchmode -projectPath D:\baidu\ZionBase\zionsdk-unity-demo\ -executeMethod ZionGame.ABAutoBuilder.BuildAndroidFromCmd
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
                    if (allTargets.ContainsKey(target) == false)
                        Debug.LogError("<color=#ff8080>Target [" + target + "] not recognized OR not enabled.</color>");
                    targetsToBuild.Add(target);
                }
            }

            // If none were specified, assume we mean ALL targets.
            if (targetsToBuild.Count == 0)
                targetsToBuild.AddRange(allTargets.Keys);

            int failed = DoBuilds(targetsToBuild, allTargets);
            // if any builds failed, this will be non-zero, which is an error in most operating systems
            EditorApplication.Exit(failed);
        }

        /// <summary>
        /// 命令行执行打包安卓线上资源
        /// </summary>
        static public void BuildAndroidOnlineCmd()
        {
            BuildAndUpload("android", true, false);
        }

        /// <summary>
        /// 命令行执行打包安卓测试资源
        /// </summary>
        static public void BuildAndroidTestCmd()
        {
            BuildAndUpload("android", false, false);
        }

        /// <summary>
        /// 命令行执行打包安卓开发资源
        /// </summary>
        static public void BuildAndroidDevCmd()
        {
            BuildAndUpload("android", false, true);
        }
          

        /// <summary>
        /// 命令行执行打包IOS线上资源
        /// </summary>
        static public void BuildIosOnlineCmd()
        {
            BuildAndUpload("ios", true, false);
        }

        /// <summary>
        /// 命令行执行打包IOS测试资源
        /// </summary>
        static public void BuildIosTestCmd()
        {
            BuildAndUpload("ios", false, false);
        }

        /// <summary>
        /// 命令行执行打包IOS开发资源
        /// </summary>
        static public void BuildIosDevCmd()
        {
            BuildAndUpload("ios", false, true);
        }

        /// <summary>
        /// 打包指定的平台到线上或线下
        /// </summary>
        /// <param name="_platform">指定的平台：win64,ios,android</param>
        /// <param name="isBos">是否为线上</param>
        static async void BuildAndUpload(string _platform, bool isBos, bool _isDev = false)
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
                    bool upresult = false;
                    //上传注释掉了
                    //VersionPushHelper versionPushHelper = new VersionPushHelper();
                    //if (isBos)
                    //{
                    //    //最终服务器地址,最后是android还是ios和下面传的结果有关系：
                    //    //https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/
                    //    //最终文件地址：https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/BaseBundlesBuild/0.1.19/bundles_win64/manifest-0.1.19.json
                    //    BosUploaderHelper bosUploaderHelper = new BosUploaderHelper();
                    //    bool resultStatus = false;
                    //    string[] dirs = Directory.GetDirectories(localFolder);
                    //    for (int i = 0, cnt = dirs.Length; i < cnt; i++)
                    //    {
                    //        DirectoryInfo directoryInfo = new DirectoryInfo(dirs[i]);
                    //        if (directoryInfo.Name == "win64")
                    //        {
                    //          resultStatus= await bosUploaderHelper.UploadFolderTask(localFolder, remoteFolder, "windows");
                    //            Debug.Log("Upload to windows, resultStatus: " + resultStatus);
                    //            if (resultStatus)
                    //            {
                    //                versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, directoryInfo.Name, buildVersion,
                    //                "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/windows/", isBos, false);
                    //            }
                    //        }
                    //        else if (directoryInfo.Name == "android")
                    //        {
                    //            resultStatus = await bosUploaderHelper.UploadFolderTask(localFolder, remoteFolder, "android");
                    //            Debug.Log("Upload to android, resultStatus: " + resultStatus);
                    //            if (resultStatus)
                    //            {
                    //                versionPushHelper.PushLatestBaseResVersion(
                    //               Application.version, directoryInfo.Name, buildVersion,
                    //               "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/android/", isBos, false);
                    //            }
                    //        }
                    //        else if (directoryInfo.Name == "ios")
                    //        {
                    //            resultStatus = await bosUploaderHelper.UploadFolderTask(localFolder, remoteFolder, "ios");
                    //            Debug.Log("Upload to ios, resultStatus: " + resultStatus);
                    //            if (resultStatus)
                    //            {
                    //                versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, directoryInfo.Name, buildVersion,
                    //                "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/", isBos, false);
                    //            }
                    //        }
                    //    }

                    //    EditorApplication.Exit(resultStatus ? 0 : -1);
                    //}
                    //else
                    //{
                    //    FtpUploaderHelper ftpUpHelper = new FtpUploaderHelper("10.27.209.219", "8021", "/", "tmbh", "anquan@123", false);
                    //    remoteFolder = "/yuanbang" + remoteFolder;
                    //    Debug.Log("remoteFolder: " + remoteFolder);
                    //    //上传到服务器
                    //    //ftpUpHelper.UploadFolderAsync(localFolder, remoteFolder);
                    //    upresult=await ftpUpHelper.UploadFolderTask(localFolder, remoteFolder);
                    //    if (upresult)
                    //    {
                    //        Debug.Log("upload success, push Base Res Version...");
                    //        if (_isDev)
                    //        {
                    //            versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, _platform, buildVersion, "https://zion-sdk-download.baidu-int.com/download/yuanbang/", isBos, true);
                    //        }
                    //        else
                    //        {
                    //            versionPushHelper.PushLatestBaseResVersion(
                    //                Application.version, _platform, buildVersion, "https://zion-sdk-download.baidu-int.com/download/yuanbang/", isBos, false);
                    //        }
                    //    }
                    //}

                    EditorApplication.Exit(upresult?0:-1);
                }
            }
            else
            {
                EditorApplication.Exit(-1);
                UnityEngine.Debug.Log("<color=#ff8080>AutoBuilder cannot build bundles while running or compiling.</color>");
            }
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

                if (buildResult != BuildResult.Succeeded)  // count failures
                    failed++;

                if (buildResult == BuildResult.Cancelled)  // quit all builds immediately if one was canceled
                    break;
            }
            Debug.Log(failed + " builds failed out of " + builds);
            return failed;
        }

        static private BuildResult DoBuild(ABBuildInfo buildInfo)
        {
            // nuke the dest folder--if you make multiple builds in a row and *remove* something from the build, it will still be there.
            if (Directory.Exists(buildInfo.DestFolder))
                Directory.Delete(buildInfo.DestFolder, true);
            Directory.CreateDirectory(buildInfo.DestFolder);

            Directory.CreateDirectory(buildInfo.DestBundleFolder);
            Directory.CreateDirectory(buildInfo.DestConfigFolder);
            Directory.CreateDirectory(Application.streamingAssetsPath);

            string absEmbedPath = Application.streamingAssetsPath + buildInfo.EmbedBundleFolder;
            if (Directory.Exists(absEmbedPath))
                Directory.Delete(absEmbedPath, true);  // nuke anything that might be still sitting there since last build

            // Write out the config file
            //string configJson = buildInfo.ConfigJson;  // convert structure over to json
            //string configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
            string configEmbedPath = Application.streamingAssetsPath + "/config_" + buildInfo.TargetString + ".json";

            //File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

            // Write out the version file (for easy scripting)
            string versionPath = buildInfo.DestConfigFolder + "/version.txt";
            File.WriteAllText(versionPath, buildInfo.BuildVersion);

            // Modify the player version.  I think this will cause PlayerSettings.asset to get changed, unfortunately.
            //PlayerSettings.bundleVersion = buildInfo.BuildVersion;

            // Construct the build player options from the BuildInfo that was previously configured.
            BuildPlayerOptions options = new BuildPlayerOptions();
            options.locationPathName = buildInfo.DestFolder + "/" + buildInfo.ExeFilename;
            options.scenes = ABBuildConfig.GetSceneList(buildInfo.Target);
            options.options = buildInfo.PlayerOptions;
            options.target = buildInfo.Target;
            options.targetGroup = BuildPipeline.GetBuildTargetGroup(buildInfo.Target);

            //-------------------
            //不修改這個ABBuild文件了，每次都要提交
            // Patch in the platform string into the ABVersion and save it so it is stored correctly in the built player
            //ABBuild existingBuild = Resources.Load<ABBuild>(ABBuild.kBuildPath);
            //if (existingBuild != null)
            //{
            //    string fullPath = AssetDatabase.GetAssetPath(existingBuild);
            //    AssetDatabase.DeleteAsset(fullPath);
            //}
            //不修改這個ABBuild文件了，每次都要提交

            // This drops the ABBuild object in /Assets/ReachableGames/AutoBuilder/Resources/ABBuild by default.
            // It only exists during the build 
            // of the player, and only exists so the player knows exactly which platform and build number it was given,
            // so it can find its asset bundles on launch.
            string kInstallFolder = ABEditorUtilities.GetInstallFolder() + "/Resources";
            string kAssetDBInstallFolder = "Assets" + kInstallFolder;

            if (Directory.Exists(Application.dataPath + kInstallFolder) == false)
            {
                Directory.CreateDirectory(Application.dataPath + kInstallFolder);
                AssetDatabase.Refresh();
            }

            //不修改這個ABBuild文件了，每次都要提交
            // generate the scriptable object that will hold our Build data
            //string assetPathForBuildAsset = kAssetDBInstallFolder + "/" + ABBuild.kBuildPath + ".asset";
            //ABBuild buildObj = ScriptableObject.CreateInstance<ABBuild>();
            //buildObj.platform = buildInfo.TargetString;
            //buildObj.buildVersion = buildInfo.BuildVersion;
            //buildObj.selfContained = buildInfo.SelfContainedBuild;
            //ABEditorUtilities.CreateScriptableObjectAsset(buildObj, assetPathForBuildAsset);
            //AssetDatabase.Refresh();
            //不修改這個ABBuild文件了，每次都要提交

            // This requires bundles to be build BEFORE the app, because the bundles need to be IN the app.
            BuildResult result = BuildResult.Failed;
            if (buildInfo.SelfContainedBuild)
            {
                //先删除之前的Build文件
                File.Delete(absEmbedPath.TrimEnd('/', '\\') + ".meta");  // clean up the meta files we produce while making a build
                //防止切平台之后，之前平台config文件未删除
                string configEmbedPath1 = Application.streamingAssetsPath + "/config_win64"  + ".json";
                string configEmbedPath2 = Application.streamingAssetsPath + "/config_android"  + ".json";
                string configEmbedPath3 = Application.streamingAssetsPath + "/config_ios"  + ".json";
                File.Delete(configEmbedPath1);
                File.Delete(configEmbedPath1 + ".meta");
                File.Delete(configEmbedPath2);
                File.Delete(configEmbedPath2 + ".meta");
                File.Delete(configEmbedPath3);
                File.Delete(configEmbedPath3 + ".meta");

                File.Delete(configEmbedPath);
                File.Delete(configEmbedPath + ".meta");

                // Build asset bundles AFTER we build the player successfully.
                if (ABBundleBuilder.DoBuildBundles(buildInfo.SrcBundleFolder, buildInfo.BuildVersion,
                    buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString,
                    buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact))
                {
                    // Copy in the bundles to the right place in /StreamingAssets/
                    Directory.CreateDirectory(absEmbedPath);

                    // Write out the config file
                    ABBootstrap tmpBootstrap = JsonUtility.FromJson<ABBootstrap>(buildInfo.ConfigJson);
                    long totalLength = GetDirectoryLength(ABEditorUtilities.GetBuildRootFolder(buildInfo.BuildVersion));
                    Debug.Log("Bundle Total Length: " + totalLength);
                    tmpBootstrap.totalFileSize = totalLength;
                    configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
                    //string configEmbedPath = Application.streamingAssetsPath + "/config_" + buildInfo.TargetString + ".json";
                    string configJson = JsonUtility.ToJson(tmpBootstrap);
                    File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

                    File.Copy(configPath, configEmbedPath, true);

                    // Copy all bundles.  This only works because there are no subdirectories.
                    foreach (var file in Directory.GetFiles(buildInfo.DestBundleFolder))
                        File.Copy(file, Path.Combine(absEmbedPath, Path.GetFileName(file)), true);

                    //嵌入包不打包程序，只打包AssetBundle
                    // do the player build first
                    //try
                    //{
                    //    result = DoBuildPlayer(options, buildInfo.TargetString, buildInfo.WaitForDebugger, buildInfo.ServerBuild);
                    //}
                    //finally
                    //{
                    //    // get rid of the build asset.  We never want that to exist on disk except during the Player build
                    //    //不修改這個ABBuild文件了，每次都要提交
                    //    //AssetDatabase.DeleteAsset(assetPathForBuildAsset);

                    //    // Nuke the folder, so we don't leave garbage behind for the next build.
                    //    Directory.Delete(absEmbedPath, true);  // nuke anything that might be still sitting there since last build
                    //    File.Delete(absEmbedPath.TrimEnd('/', '\\') + ".meta");  // clean up the meta files we produce while making a build
                    //    File.Delete(configEmbedPath);
                    //    File.Delete(configEmbedPath + ".meta");
                    //}
                    result = BuildResult.Succeeded;
                }
            }
            else
            {
                //这里注释掉了，不打包程序，只打包AssetBundle
                // do the player build first
                //result = DoBuildPlayer(options, buildInfo.TargetString, buildInfo.WaitForDebugger, buildInfo.ServerBuild);
                //AssetDatabase.DeleteAsset(assetPathForBuildAsset);  // get rid of the build asset.  We never want that to exist on disk except during the Player build

                //if (result==BuildResult.Succeeded)
                //{
                // Build asset bundles AFTER we build the player successfully.
                bool bundleStatus = ABBundleBuilder.DoBuildBundles(buildInfo.SrcBundleFolder, buildInfo.BuildVersion,
                    buildInfo.DestBundleFolder, buildInfo.BundleOptions, buildInfo.Target, buildInfo.TargetString,
                    buildInfo.Logging, buildInfo.IgnoreEndsWith, buildInfo.IgnoreContains, buildInfo.IgnoreExact);
                //}

                // Write out the config file
                ABBootstrap tmpBootstrap = JsonUtility.FromJson<ABBootstrap>(buildInfo.ConfigJson);
                long totalLength = GetDirectoryLength(ABEditorUtilities.GetBuildRootFolder(buildInfo.BuildVersion));
                Debug.Log("Bundle Total Length: " + totalLength);
                tmpBootstrap.totalFileSize = totalLength;
                configPath = buildInfo.DestConfigFolder + "/config_" + buildInfo.TargetString + ".json";
                string configJson = JsonUtility.ToJson(tmpBootstrap);
                File.WriteAllText(configPath, configJson, System.Text.Encoding.UTF8);

                result = bundleStatus ? BuildResult.Succeeded : BuildResult.Failed;
            }
            return result;
        }

        static string configPath = "";
        /// <summary>
        /// 获取文件夹大小
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        static long GetDirectoryLength(string dirPath)
        {
            //判断给定的路径是否存在,如果不存在则退出
            if (!Directory.Exists(dirPath))
                return 0;
            long len = 0;
            //定义一个DirectoryInfo对象
            DirectoryInfo di = new DirectoryInfo(dirPath);
            //通过GetFiles方法,获取di目录中的所有文件的大小
            foreach (FileInfo fi in di.GetFiles())
            {
                //排除Config文件的大小
                if (fi.FullName != configPath)
                    len += fi.Length;
            }
            //获取di中所有的文件夹,并存到一个新的对象数组中,以进行递归
            DirectoryInfo[] dis = di.GetDirectories();
            if (dis.Length > 0)
            {
                for (int i = 0; i < dis.Length; i++)
                {
                    len += GetDirectoryLength(dis[i].FullName);
                }
            }
            return len;
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
                Debug.Log("<color=#8080ff><b>BUILD PLAYER SUCCESS! (platform=" + platformString + ")</b></color>!");
            }
            else if (result == BuildResult.Failed)
            {
                Debug.Log("<color=#ff8080><b>BUILD PLAYER FAILED! (platform=" + platformString + ")</b></color>!");
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
