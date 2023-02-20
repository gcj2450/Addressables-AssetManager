using UnityEngine;
using UnityEditor;
using System.IO;

public class WriteAtlasScript
{
    [MenuItem("Assets/重新生成图集脚本")]
    public static void CreateAtlasScript()
    {
        EditorUtility.DisplayProgressBar("生成图集脚本", "正在重新生成图集脚本...", 0.7f);
        string result = TitleStr + "\n";
        string guiResourceDic = "Assets/GUiResource/Src/";
        DirectoryInfo dic = new DirectoryInfo(guiResourceDic);
        if (!dic.Exists) { Debug.LogError("不存在这个文件夹 " + dic.FullName); return; }
        DirectoryInfo[] dics = dic.GetDirectories();
        if (dics == null || dics.Length <= 0) return;
        for (int i = 0; i < dics.Length; i++)
        {
            result += dics[i].Name + ",\n";
        }
        result += "None,\n";
        result += EndStr;
        // Debug.LogError(result);

        string filePath = "Assets/Asgard/ABResource/AtlasResourceEnum.cs";

        FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(result);
        writer.Close();
        writer.Dispose();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("生成图集脚本", "成功生成图集脚本！", "关闭");
    }

    private const string TitleStr =
    @"using UnityEngine;
      using System.Collections;
      using Asgard.Resource;

      public class AtlasResourceEnum
      {
          public enum AtlasType
          {";
    private const string EndStr =
    @"    }
      }";



}
