using UnityEngine;

namespace ZionGame.Example
{
    public class SceneLoader : MonoBehaviour
    {
        public ExampleScene scene;
        public float delay;

        public bool loadOnAwake = true;
        public bool showProgress;

        private ZionGameScene loading;

        private void Start()
        {
            if (loadOnAwake)
            {
                LoadScene();
            }
        }

        public void LoadScene()
        {
            if (delay > 0)
            {
                Invoke("Loading", 3);
                return;
            }

            Loading();
        }

        private void Loading()
        {
            if (loading != null)
            {
                return;
            }

            loading = ZionGameScene.LoadAsync(scene.ToString());
            if (showProgress)
            {
                PreloadManager.Instance.ShowProgress(loading);
            }
        }
    }
}