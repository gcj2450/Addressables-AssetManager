﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZionGame
{
    public class ZionGameScene : Loadable, IEnumerator
    {
        public static Action<ZionGameScene> onSceneUnloaded;
        public static Action<ZionGameScene> onSceneLoaded;

        private static readonly List<AsyncOperation> Progressing = new List<AsyncOperation>();

        private static readonly List<string> scenesInBuild = new List<string>();
        public readonly List<ZionGameScene> additives = new List<ZionGameScene>();
        public Action<ZionGameScene> completed;
        public Action<AsyncOperation> onload;
        protected string sceneName;
        public Action<ZionGameScene> updated;
        protected bool mustCompleteOnNextFrame { get; set; }
        public static Func<string, bool, ZionGameScene> Creator { get; set; } = Create;

        public AsyncOperation load { get; private set; }

        public static ZionGameScene main { get; set; }
        protected LoadSceneMode loadSceneMode { get; set; }

        public Task<ZionGameScene> Task
        {
            get
            {
                var tcs = new TaskCompletionSource<ZionGameScene>();
                completed += operation => { tcs.SetResult(this); };
                return tcs.Task;
            }
        }

        public bool MoveNext()
        {
            return !isDone;
        }

        public void Reset()
        {
        }

        public object Current => null;

        protected void LoadSceneAsync(AsyncOperation operation)
        {
            if (operation == null)
            {
                return;
            }
            load = operation;
            onload?.Invoke(load);
            onload = null;
            Progressing.Add(operation);
        }

        private static void UnloadSceneAsync(AsyncOperation operation)
        {
            if (operation == null)
            {
                return;
            }
            Progressing.Add(operation);
        }

        private static ZionGameScene CreateInstance(string path, bool additive)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path));
            }

            PathManager.GetActualPath(ref path);
            return Creator(path, additive);
        }

        public static ZionGameScene LoadAsync(string assetPath, Action<ZionGameScene> completed = null, bool additive = false)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentNullException(nameof(assetPath));
            }

            var scene = CreateInstance(assetPath, additive);
            scene.Load();
            if (completed != null)
            {
                scene.completed += completed;
            }

            return scene;
        }

        public static ZionGameScene Load(string assetPath, bool additive = false)
        {
            var scene = CreateInstance(assetPath, additive);
            scene.mustCompleteOnNextFrame = true;
            scene.Load();
            return scene;
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading)
            {
                return;
            }

            UpdateLoading();
            updated?.Invoke(this);
        }

        protected void UpdateLoading()
        {
            if (load == null)
            {
                Finish("operation == null");
                return;
            }

            progress = 0.5f + load.progress * 0.5f;

            if (load.allowSceneActivation)
            {
                if (!load.isDone)
                {
                    return;
                }
            }
            else
            {
                // https://docs.unity3d.com/ScriptReference/AsyncOperation-allowSceneActivation.html
                if (load.progress < 0.9f)
                {
                    return;
                }
            }

            Finish();
        }

        protected override void OnLoad()
        {
            PrepareToLoad();
            if (mustCompleteOnNextFrame)
            {
                SceneManager.LoadScene(sceneName, loadSceneMode);
                Finish();
            }
            else
            {
                LoadSceneAsync(SceneManager.LoadSceneAsync(sceneName, loadSceneMode));
            }
        }

        protected void PrepareToLoad()
        {
            sceneName = Path.GetFileNameWithoutExtension(pathOrURL);
            if (loadSceneMode == LoadSceneMode.Single)
            {
                if (main != null)
                {
                    main.Release();
                    main = null;
                }

                main = this;
            }
            else
            {
                if (main == null)
                {
                    return;
                }

                main.additives.Add(this);
            }
        }

        protected override void OnUnused()
        {
            completed = null;
            Unused.Add(this);
        }

        private static void UnloadSceneAsync(string scene)
        {
            var getScene = SceneManager.GetSceneByName(scene);
            if (! getScene.IsValid())
            {
                return;
            }
            var unloadSceneAsync = SceneManager.UnloadSceneAsync(scene);
            if (unloadSceneAsync == null)
            {
                return;
            }

            UnloadSceneAsync(unloadSceneAsync);
        }

        public static bool IsLoadingOrUnloading()
        {
            for (var i = 0; i < Progressing.Count; i++)
            {
                var item = Progressing[i];
                if (item != null && !item.isDone)
                {
                    return true;
                }
                Progressing.RemoveAt(i);
                i--;
            }

            return false;
        }
        
        protected override void OnUnload()
        {
            if (loadSceneMode == LoadSceneMode.Additive)
            {
                main?.additives.Remove(this);
                if (string.IsNullOrEmpty(error))
                {
                    UnloadSceneAsync(sceneName);
                }
            }
            else
            {
                foreach (var item in additives)
                {
                    item.Release();
                }
                additives.Clear();
            }

            onSceneUnloaded?.Invoke(this);
        }

        protected override void OnComplete()
        {
            onSceneLoaded?.Invoke(this);
            if (completed == null)
            {
                return;
            }

            var saved = completed;
            completed?.Invoke(this);
            completed -= saved;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scene = SceneManager.GetSceneByBuildIndex(i);
                scenesInBuild.Add(scene.path);
            } 
        } 

        private static ZionGameScene Create(string assetPath, bool additive = false)
        {
            if (!Versions.Contains(assetPath))
            {
                if (!scenesInBuild.Contains(assetPath))
                {
                    throw new FileNotFoundException(assetPath);
                }
                return new ZionGameScene
                {
                    pathOrURL = assetPath,
                    loadSceneMode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single
                };
            }

            return new BundledScene
            {
                pathOrURL = assetPath,
                loadSceneMode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single
            };
        }
    }
}