using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace ZionGame
{
    /// <summary>
    /// 基础资源加载器
    /// </summary>
    public class BaseAssetsLoader
    {
        private List<ABILoadingState> _states = new List<ABILoadingState>();
        private Dictionary<string, ABIData> _configData = new Dictionary<string, ABIData>();
        private int _currentState = 0;

        /// <summary>
        /// 服务器地址，该地址会在运行时被用于拼接出最终地址
        /// 这个值和ABBuildConfig里保持一致
        /// </summary>
        public string BootstrapURL = "";

        public string NextSceneToLoad;

        public Image LoadingBar = null;
        public Text LoadingText = null;

        /// <summary>
        /// 设置要加载的资源版本号
        /// </summary>
        public string version = "0.1.13";

        public void Start()
        {
            //清理一下缓存，防止出错
            //if (Directory.Exists(ABUtilities.GetRuntimeCacheFolder(version)))
            //{
            //    Directory.Delete(ABUtilities.GetRuntimeCacheFolder(version), true);
            //}

            //Resources文件夹下的ABBuild文件在这里加载并修改它的内容，
            //ABBuild的设置无效，所以不用管里面的值是什么
            //ABBuild bBuild = Resources.Load<ABBuild>("ABBuild");
            ABBuild bBuild = ScriptableObject.CreateInstance<ABBuild>();
            //根据运行平台，自动配置资源平台
            string platform = "";
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    platform = "win64";
                    break;

                case RuntimePlatform.Android:
                    platform = "android";
                    break;

                case RuntimePlatform.IPhonePlayer:
                    platform = "ios";
                    break;
            }
            bBuild.platform = platform;
            bBuild.buildVersion = version;
            bBuild.selfContained = false;

            // go ahead and load the asset bundles off the web
            _configData.Add("BuildData", bBuild);

            // Configure the bundle cache
            ABUtilities.ConfigureCache(version);

            // 拼接出资源所在服务器地址
            // For self-contained, it depends on the platform.
            if (!BootstrapURL.EndsWith("/"))
            {
                BootstrapURL = BootstrapURL + "/";
            }

            string platformFile = "config_{PLATFORM}.json";
            BootstrapURL = BootstrapURL + ABUtilities.GetRuntimeSubFolder() + "/" + version + "/" + platformFile;
            string configURL = BootstrapURL;
            Debug.Log("configURL: "+configURL);
            string bundleFolderPrefix = string.Empty;

            // 内置包的加载方案
            if (bBuild.selfContained)
            {
                // Android already comes with "jar:file://", but everything else is just raw path.
                bundleFolderPrefix = Application.streamingAssetsPath;
#if !UNITY_ANDROID
                bundleFolderPrefix = "file://" + bundleFolderPrefix;
#endif
                bundleFolderPrefix.TrimEnd('/');  // guarantee this does not end in a slash.

                configURL = bundleFolderPrefix + "/config_" + bBuild.platform + ".json";
            }

            // 先加载 config_{PLATFORM}.json 文件.  内置包的话会在 /StreamingAssets/ 文件夹内.  
            // 里面记录了manifest文件和bundles 文件在服务器上的地址.
            _states.Add(new ABLoadingStateBootstrap(configURL,OnLoadError));

            string overrideManifestPath = "";
            //#if UNITY_EDITOR
            //                // If we want to run with override bundles, go ahead and load it last.  If it's missing, ignore it.
            //                overrideManifestPath = ABUtilities.GetRuntimeCacheFolder() + "/override-" + build.buildVersion + ".json";
            //#endif
            _states.Add(new ABLoadingStateManifest(overrideManifestPath, bundleFolderPrefix, OnLoadError));
            _states.Add(new ABLoadingStateAssetBundles(version, bundleFolderPrefix, OnLoadError));
            //注掉暂时不加载场景
            //_states.Add(new ABLoadingStateNextScene(NextSceneToLoad));

            // Start the first state here
            _currentState = 0;
            _states[_currentState].Begin(_configData);
        }

        private void OnLoadError(object sender, EventArgs e)
        {
        }

        bool broadCastEnd = false;
        public void Update()
        {
            try
            {
                //if there's more to do here, keep doing it.  The final state loads the next scene and kills this one.
                if (_currentState < _states.Count)
                {
                    ABILoadingState state = _states[_currentState];

                    // Update the loading bar (note, this is scaling a black bar from 1.0 down to 0.0,
                    // aligned to the right side, so it looks like a fancy bar is growing.
                    if(LoadingText!=null)
                        LoadingText.text = state.GetStateText();
                    float progressMinForState = _currentState / (float)_states.Count;
                    float progressMaxForState = (_currentState + 1) / (float)_states.Count;
                    float currentProgress = Mathf.Lerp(progressMinForState, progressMaxForState, state.GetProgress());
                    if(LoadingBar!=null)
                        LoadingBar.fillAmount = currentProgress;  // let the image fill handle the loading bar

                    // if we succeeded, end the current state and start the next one, or launch the next scene if we're done.
                    if (state.IsDone())
                    {
                        state.End();
                        _currentState++;
                        // when we are done starting states, this update will just spin forever.  That's ok, the last state kills this scene.
                        if (_currentState < _states.Count)
                        {
                            _states[_currentState].Begin(_configData);
                        }
                    }
                }
                else
                {
                    if(!broadCastEnd)
                    {

                    }
                    Debug.Log("All state complete");
                }
            }
            catch (Exception e)
            {
                Debug.Log("Exception caught: " + e.Message);
                //Application.Quit();
                //#if UNITY_EDITOR
                //                    EditorApplication.isPlaying = false;
                //#endif
            }
        }
    }
}
