using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZionGame
{
    /// <summary>
    /// 加载URP设置
    /// </summary>
    public class ABLoadingStateUrpSettings : ABILoadingState
    {
        private Dictionary<string, ABIData> _configData = null;
        private string urpSettingsName;
        private AsyncOperation _request = null;
        private float _lastProgress = 0.0f;

        public event EventHandler OnError;

        public ABLoadingStateUrpSettings(string _urpSettingsName)
        {
            urpSettingsName = _urpSettingsName;
        }

        public void Begin(Dictionary<string, ABIData> configData)
        {
            _configData = configData;
            // Tell the asset loader about the data.
            ABAssetLoader.Initialize(_configData);

            // load the next scene
            _request = ABAssetLoader.LoadAssetAsync(urpSettingsName, ConfigUrpSettings);
            if (_request == null) Debug.LogError("<color=#ff8080>Scene load request is null.</color>");
        }

        public void End()
        {
            _configData = null;
        }

        public string GetStateText()
        {
            if (_request != null && _request.isDone && _request.progress < 1.0f)
                return "UrpSettings load failed " + urpSettingsName;
            return "Loading urp settings";
        }

        public bool IsDone()
        {
            return _request != null && _request.isDone;
        }

        public float GetProgress()
        {
            if (_request != null)
            {
                _lastProgress = _request.progress;
            }
            return _lastProgress;
        }

        /// <summary>
        /// 加载URP设置，并进行质量设置
        /// </summary>
        /// <param name="asyncOp"></param>
        private void ConfigUrpSettings(AsyncOperation asyncOp)
        {
            AssetBundleRequest abr = asyncOp as AssetBundleRequest;
            if (asyncOp.progress == 1.0f && asyncOp.isDone)
            {
                if (abr.asset == null)
                {
                    Debug.Log("ConfigUrpSettings Asset loaded as null.");
                    return;
                }

                UniversalRenderPipelineAsset tempSettings = abr.asset as UniversalRenderPipelineAsset;
                if (tempSettings == null)
                {
                    Debug.Log("Asset is not a UniversalRenderPipelineAsset: " + abr.asset.name);
                }
                else
                {
                    //设置为动态加载的URP Settings
                    GraphicsSettings.renderPipelineAsset = tempSettings;

                    Debug.Log($"GetQualityLevel: {QualitySettings.GetQualityLevel()}");
                    //这个设置需要在QualitySettings中勾选足够数量的质量等级，如果只勾选0,1两级，设置为2无效
                    QualitySettings.SetQualityLevel(2, true);
                    Debug.Log($"GetQualityLevel: {QualitySettings.GetQualityLevel()}");
                }
            }
            else
            {
                Debug.Log("Completed called but progress is " + asyncOp.progress + " and done==" + asyncOp.isDone);
            }
        }

        public void Retry()
        {
            Begin(_configData);
        }
    }
}
