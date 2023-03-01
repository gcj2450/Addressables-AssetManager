using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Collections;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

namespace ZionGame
{
    public class ABLoader : MonoBehaviour
    {
        private List<ABILoadingState> _states = new List<ABILoadingState>();
        private Dictionary<string, ABIData> _configData = new Dictionary<string, ABIData>();
        private int _currentState = 0;

        /// <summary>
        /// 服务器地址，该地址会在运行时被用于拼接出最终地址
        /// 这个值和ABBuildConfig里保持一致
        /// </summary>
        public string BootstrapURL = "";

        public string NextSceneToLoad = "ABNextScene";

        public Image LoadingBar = null;
        public Text LoadingText = null;
        public Transform midTrans;
        /// <summary>
        /// 设置要加载的资源版本号
        /// </summary>
        public string version = "0.1.13";

        /// <summary>
        /// 设置是否为内嵌资源包
        /// </summary>
        public bool selfContained = false;
        /// <summary>
        /// 基础资源更新完成事件
        /// </summary>
        public Action OnBaseBundleUpdateCompleted;
        public Action OnBaseBundleUpdateFailed;
        void Start()
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
            bBuild.platform = ABUtilities.GetRuntimePlatform();
            bBuild.buildVersion = version;
            bBuild.selfContained = selfContained;

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
            BootstrapURL = BootstrapURL + ABUtilities.GetRuntimeSubFolder() + "/" + version + "/" + ABUtilities.GetRuntimePlatform() + "/" + platformFile;
            string configURL = BootstrapURL;
            Debug.Log("configURL: " + configURL);
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
            _states.Add(new ABLoadingStateBootstrap(configURL,StateOnError));

            string overrideManifestPath = "";
            //#if UNITY_EDITOR
            //                // If we want to run with override bundles, go ahead and load it last.  If it's missing, ignore it.
            //                overrideManifestPath = ABUtilities.GetRuntimeCacheFolder() + "/override-" + build.buildVersion + ".json";
            //#endif
            _states.Add(new ABLoadingStateManifest(overrideManifestPath, bundleFolderPrefix, StateOnError));

            //这里修改要不要打嵌入包
            bool UseStreamingAssets = false;

            if (UseStreamingAssets)
            {
                _states.Add(new ABLoadingStateAssetBundles(version, bundleFolderPrefix, BundleStateError));
            }
            else
            {
                _states.Add(new ABLoadingStateAssetBundles_Network(version, bundleFolderPrefix, Bundle_NetworkError));
            }
            //注掉暂时不加载场景
            //_states.Add(new ABLoadingStateNextScene(NextSceneToLoad));

            // Start the first state here
            _currentState = 0;
            _states[_currentState].Begin(_configData);
        }

        GameObject loadingErrorTip;
        GameObject sureGo;
        private void Bundle_NetworkError(object sender, EventArgs e)
        {
            ErrorArgs ErrorArgs = (ErrorArgs) e;
            Debug.Log($"Bundle download error: {ErrorArgs.Reason}, current state: {_currentState}");
            //网络错误回调，弹出提示框
            //if (loadingErrorTip == null)
            //    loadingErrorTip = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("UI/UILoadingTip"), midTrans);
            //sureGo = loadingErrorTip.GetComponent<ReferenceCollector>().Get<GameObject>("Sure");
            //if (sureGo != null)
            //{
            //    sureGo.GetComponent<Button>().onClick.AddListener(RetryClick);
            //}
        }

        /// <summary>
        /// 基础数资更新失败重试事件
        /// </summary>
        private void RetryClick()
        {
            GameObject.Destroy(loadingErrorTip);
            Debug.Log($"ABloader RetryClick... Retry load current state: {_currentState}");
            //单纯重新加载当前进度的Bundle，不用再重新走一遍流程。
            _states[_currentState].Retry();
        }

        ABBootstrap _bootstrap;
		/// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StateOnError(object sender, EventArgs e)
        {
            Debug.Log("ABLoader StateOnError");
            if (OnBaseBundleUpdateFailed!=null)
            {
                OnBaseBundleUpdateFailed();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BundleStateError(object sender, EventArgs e)
        {
            Debug.Log("ABLoader StateOnError BundleStateError Unload UnloadAllAssetBundles ");
            //Bundle 包加载错了，全部卸载掉，重新执行
            AssetBundle.UnloadAllAssetBundles(true);
            if (OnBaseBundleUpdateFailed != null)
            {
                OnBaseBundleUpdateFailed();
            }
        }

        string totalSize = "";
        bool broadCastEnd = false;
        int clickCnt = 0;
        void Update()
        {
            //if (Input.GetMouseButtonUp(0)&& GlobalConfig.Instance.ServerCluster== ServerCluster.DEVELOP_ENVIROMENT)
            //{
            //    //屏幕左半边点击计数
            //    Rect bound = new Rect(0, 0, Screen.width * 0.5f, Screen.height);
            //    if (bound.Contains(Input.mousePosition))
            //    {
            //        clickCnt++;
            //        if(clickCnt>3)
            //            LoadingText.text = "点击屏幕，clickCnt: " + clickCnt;
            //    }
            //    //else
            //    //{
            //    //    EnterApp();
            //    //}
            //}
            try
            {
                //if there's more to do here, keep doing it.  The final state loads the next scene and kills this one.
                if (_currentState < _states.Count)
                {
                    ABILoadingState state = _states[_currentState];

                    // Update the loading bar (note, this is scaling a black bar from 1.0 down to 0.0,
                    // aligned to the right side, so it looks like a fancy bar is growing.
                    //float progressMinForState = _currentState / (float)_states.Count;
                    //float progressMaxForState = (_currentState + 1) / (float)_states.Count;
                    float currentProgress = state.GetProgress();// Mathf.Lerp(progressMinForState, progressMaxForState, state.GetProgress());

                    if (_configData.ContainsKey("Bootstrap"))
                    {
                        if(_bootstrap==null)
                            _bootstrap = _configData["Bootstrap"] as ABBootstrap;
                        if(string.IsNullOrEmpty(totalSize))
                            totalSize=(_bootstrap.totalFileSize / 1024.0 / 1024.0).ToString("F1");
                        if (currentProgress == 0)
                            LoadingText.text = "正在检查基础资源更新, 请稍候...";
                        else
                            LoadingText.text = $"正在下载基础资源，总大小: {totalSize} MB，进度：{Mathf.RoundToInt(currentProgress * 100)}%";
                    }
                    else
                    {
                        LoadingText.text = "正在检查基础资源更新, 请稍候...";
                    }
                    //LoadingText.text = state.GetStateText()+"_"+ (currentProgress*100).ToString("F2")+"%";
                    if (LoadingBar != null)
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
                        else
                        {
                            if (!broadCastEnd)
                            {
                                broadCastEnd = true;
                                Debug.Log("All state complete");
                                ABAssetLoader.Initialize(_configData);
                               
                                //if (GlobalConfig.Instance.ServerCluster == ServerCluster.DEVELOP_ENVIROMENT ||
                                //    GlobalConfig.Instance.ServerCluster == ServerCluster.ONLINE_EXP_ENVIROMENT ||
                                //    GlobalConfig.Instance.ServerCluster == ServerCluster.ONLINE_TEST_TENANT_ENVIROMENT)
                                //{
                                //    //LoadingText.text = "点左边切场景，右边进游戏";
                                //    StartCoroutine(EnterApp());
                                //}
                                //else
                                {
                                    LoadingText.text = $"更新完毕！";
                                    if (LoadingBar != null)
                                        LoadingBar.fillAmount = 1;  
                                    if (OnBaseBundleUpdateCompleted != null)
                                        OnBaseBundleUpdateCompleted();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Load Base Bundle Exception caught: " + e.Message);
                LoadingText.text = "更新资源失败，请重试...";
                if (Directory.Exists(ABUtilities.GetRuntimeCacheFolder(version)))
                {
                    Directory.Delete(ABUtilities.GetRuntimeCacheFolder(version), true);
                }
                StateOnError(this, null);
                //StartCoroutine(QuitApp());
                //Application.Quit();
                //#if UNITY_EDITOR
                //                    EditorApplication.isPlaying = false;
                //#endif
            }
        }

        /// <summary>
        /// 3秒后退出APP
        /// </summary>
        /// <returns></returns>
        IEnumerator QuitApp()
        {
            yield return new WaitForSeconds(3);
            Application.Quit();
        }

        /// <summary>
        /// 3秒后进入游戏
        /// </summary>
        /// <returns></returns>
        IEnumerator EnterApp()
        {
            yield return new WaitForSeconds(2);
            //if (ET.Define.IsDebug)
            //    ET.Log.Debug("EEEnterApp: " + clickCnt);
            PlayerPrefs.SetInt("abclickcount", clickCnt);
            PlayerPrefs.Save();
            if (OnBaseBundleUpdateCompleted != null)
                OnBaseBundleUpdateCompleted();
        }
    }
}
