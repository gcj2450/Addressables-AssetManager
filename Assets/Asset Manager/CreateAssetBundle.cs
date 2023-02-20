//using UnityEditor;
//using UnityEngine;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;

///// <summary>
///// 1 将资源打包成assetbundle，并放到自定目录下。
///// </summary>
//public class CreateAssetBundle
//{
//    public static void Execute(UnityEditor.BuildTarget target)
//    {
//        string SavePath = AssetBundleController.GetPlatformPath(target);

//        // 当前选中的资源列表
//        foreach (Object o in Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets))
//        {
//            string path = AssetDatabase.GetAssetPath(o);

//            // 过滤掉meta文件和文件夹
//            if (path.Contains(".meta") || path.Contains(".") == false)
//                continue;

//            // 过滤掉UIAtlas目录下的贴图和材质(UI/Common目录下的所有资源都是UIAtlas)
//            if (path.Contains("UI/Common"))
//            {
//                if ((o is Texture) || (o is Material))
//                    continue;
//            }

//            path = SavePath + ConvertToAssetBundleName(path);
//            path = path.Substring(0, path.LastIndexOf('.'));
//            path += ".assetbundle";

//            BuildPipeline.BuildAssetBundle(o, null, path, BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle, target);
//        }

//        // scene目录下的资源


//        AssetDatabase.Refresh();
//    }

//    static string ConvertToAssetBundleName(string ResName)
//    {
//        return ResName.Replace('/', '.');
//    }
//}