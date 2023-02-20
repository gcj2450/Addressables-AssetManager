using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace Asgard
{
    public class ABResMapScriptObj : ScriptableObject
    {
        public float Version = -1;
        public string Platform = "";

        public static int ABLoadTypeNone = 0;
        public static int ABLoadTypeAudio = 1 << 0;
        public static int ABLoadTypeTankPerfab = 1 << 1;
        public static int ABLoadTypeConfigDatas = 1 << 2;
        public static int ABLoadTypeGuiSprites = 1 << 3;
        public static int ABLoadTypeGuiAtlas = 1 << 4;
        public static int ABLoadTypeUIPanelPrefab = 1 << 5;
        public static int ABLoadTypeUILoadingPrefab = 1 << 6;
        public static int ABLoadTypeScene = 1 << 7;
        public static int ABLoadTypeEffect = 1 << 8;

        public static int ABResTypeMain = 1 << 0;
        public static int ABResTypeSub = 1 << 1;

        public List<ABResMapItemScriptObj> Resources = new List<ABResMapItemScriptObj>();
    }
}

