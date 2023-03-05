using System;

namespace ZionGame.Editor
{
    [Serializable]
    public class ReplaceBundleName
    {
        public string key;
        public string value;
        public bool enabled = true;
    }
}