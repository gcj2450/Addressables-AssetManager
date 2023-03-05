using System.Collections.Generic;
using UnityEngine;

namespace ZionGame.Example
{
    public class LoadSceneAdditive : MonoBehaviour
    {
        public string sceneName;
        private readonly List<ZionGameScene> _scenes = new List<ZionGameScene>();

        public void Unload()
        {
            if (_scenes.Count > 0)
            {
                var index = _scenes.Count - 1;
                var scene = _scenes[index];
                scene.Release();
                _scenes.RemoveAt(index);
            }
        }

        public void Load()
        {
            _scenes.Add(ZionGameScene.LoadAsync(sceneName, null, true));
        }
    }
}