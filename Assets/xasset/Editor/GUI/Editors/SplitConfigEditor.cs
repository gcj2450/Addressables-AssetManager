using System.Text;
using UnityEditor;
using UnityEngine;

namespace ZionGame.Editor
{
    [CustomEditor(typeof(SplitConfig))]
    public class SplitConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("PingObject"))
                {
                    EditorUtility.PingWithSelected(target);
                }

                if (GUILayout.Button("Copy"))
                {
                    var sb = new StringBuilder();
                    var group = target as SplitConfig;
                    if (group != null)
                    {
                        foreach (var asset in group.GetAssets())
                        {
                            sb.AppendLine($"\"{asset}\",");
                        }
                    }

                    EditorGUIUtility.systemCopyBuffer = sb.ToString();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}