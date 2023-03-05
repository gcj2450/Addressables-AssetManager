using System;

namespace ZionGame
{
    [Serializable]
    public class BuildVersion
    {
        public string name;
        public string file;
        public long size;
        public string hash;
    }
}