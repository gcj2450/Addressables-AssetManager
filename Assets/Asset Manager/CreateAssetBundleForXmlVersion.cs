//using UnityEditor;
//using UnityEngine;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;

///// <summary>
///// 4 将变更列表文件也打包成assetbundle
///// 也就是将VersionNum.xml打包后供下载 
///// </summary>
//public class CreateAssetBundleForXmlVersion
//{
//    public static void Execute(UnityEditor.BuildTarget target)
//    {
//        string SavePath = AssetBundleController.GetPlatformPath(target);
//        Object obj = AssetDatabase.LoadAssetAtPath(SavePath + "VersionNum/VersionNum.xml", typeof(Object));
//        BuildPipeline.BuildAssetBundle(obj, null, SavePath + "VersionNum/VersionNum.assetbundle", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, target);

//        AssetDatabase.Refresh();
//    }

//    static string ConvertToAssetBundleName(string ResName)
//    {
//        return ResName.Replace('/', '.');
//    }
//}