using System.IO;
using UnityEngine;

namespace ZionGame.Example
{
    public class LoadRawAsset : MonoBehaviour
    {
        public void Load()
        {
            var asset = RawAsset.Load("versions");
            asset.Release();
            var text = File.ReadAllText(asset.savePath);
            Debug.Log(text);
        }
    }
}