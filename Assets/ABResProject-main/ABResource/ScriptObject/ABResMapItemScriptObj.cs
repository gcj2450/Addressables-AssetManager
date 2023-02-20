using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Asgard
{
    [System.Serializable]
    public class ABResMapItemScriptObj
    {
        public int ResourceType = -1;
        public int ABResType = 0;
        public string AssetBundleName = "";
        public long Size = -1;
        public string Md5 = "";
        public List<string> DependAssetBundleName = new List<string>();
        public int ABResLoadType = ABResMapScriptObj.ABLoadTypeNone;
    }
}

