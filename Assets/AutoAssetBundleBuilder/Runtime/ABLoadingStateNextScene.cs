using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZionGame
{
    public class ABLoadingStateNextScene : ABILoadingState
    {
        private Dictionary<string, ABIData> _configData = null;
        private string _nextSceneName;
        private AsyncOperation _request = null;
        private float _lastProgress = 0.0f;

        public event EventHandler OnError;

        public ABLoadingStateNextScene(string nextSceneName,EventHandler eventHandler)
        {
            _nextSceneName = nextSceneName;
            OnError = eventHandler;
        }

        public void Begin(Dictionary<string, ABIData> configData)
        {
            _configData = configData;

            // load the next scene
            _request = SceneManager.LoadSceneAsync(_nextSceneName, LoadSceneMode.Additive);
            if (_request == null) Debug.LogError("<color=#ff8080>Scene load request is null.</color>");
            _request.completed += StartNextScene;
        }

        public void End()
        {
            _configData = null;
        }

        public string GetStateText()
        {
            if (_request != null && _request.isDone && _request.progress < 1.0f)
                return "Scene load failed " + _nextSceneName;
            return "Starting Up!";
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

        public void Retry()
        {
            Begin(_configData);
        }

        /// <summary>
        /// 加载下一个场景，卸载当前场景
        /// </summary>
        /// <param name="asyncOp"></param>
        private void StartNextScene(AsyncOperation asyncOp)
        {
            try
            {
                Scene nextScene = SceneManager.GetSceneByName(_nextSceneName);
                if (!nextScene.IsValid() || !nextScene.isLoaded) Debug.LogError("Next scene is not valid or could not be loaded.");

                // Start the next scene
                Scene loadingScene = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(nextScene);

                // Tell the asset loader about the data.
                ABAssetLoader.Initialize(_configData);

                // this will kill our GameObject, btw...
                SceneManager.UnloadSceneAsync(loadingScene);
            }
            catch (Exception loadingException)
            {
                Debug.LogError("Failed to start scene: " + loadingException);
                throw;
            }
        }


    }
}
