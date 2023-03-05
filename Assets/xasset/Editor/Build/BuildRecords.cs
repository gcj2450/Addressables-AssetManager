using System.Collections.Generic;
using UnityEngine;

namespace ZionGame.Editor
{
    public class BuildRecords : ScriptableObject
    {
        public List<BuildRecord> data = new List<BuildRecord>();

        public static BuildRecords GetRecords()
        {
            return EditorUtility.FindOrCreateAsset<BuildRecords>("Assets/xasset/Records.asset");
        }

        public void Save()
        {
            EditorUtility.SaveAsset(this);
        }

        public void Clear()
        {
            data.Clear();
        }
    }
}