using System;

namespace ZionGame
{
    [Serializable]
    public class ManifestAsset
    {
        public int id;
        public int dir;
        public string name;
        public int[] deps;
        public int bundle;
        public string path { get; set; }
    }
}