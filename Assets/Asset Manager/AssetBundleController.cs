//using UnityEditor;
//using UnityEngine;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;

//public class AssetBundleController : EditorWindow
//{
//    public static AssetBundleController window;
//    public static UnityEditor.BuildTarget buildTarget = BuildTarget.StandaloneWindows;

//    [MenuItem("XiYouEditor/AssetBundle/AssetBundle For Windows32", false, 1)]
//    public static void ExecuteWindows32()
//    {
//        if (window == null)
//        {
//            window = (AssetBundleController)GetWindow(typeof(AssetBundleController));
//        }
//        buildTarget = UnityEditor.BuildTarget.StandaloneWindows;
//        window.Show();
//    }

//    [MenuItem("XiYouEditor/AssetBundle/AssetBundle For Windows64", false, 1)]
//    public static void ExecuteWindows64()
//    {
//        if (window == null)
//        {
//            window = (AssetBundleController)GetWindow(typeof(AssetBundleController));
//        }
//        buildTarget = UnityEditor.BuildTarget.StandaloneWindows64;
//        window.Show();
//    }

//    [MenuItem("XiYouEditor/AssetBundle/AssetBundle For IPhone", false, 2)]
//    public static void ExecuteIPhone()
//    {
//        if (window == null)
//        {
//            window = (AssetBundleController)GetWindow(typeof(AssetBundleController));
//        }
//        buildTarget = UnityEditor.BuildTarget.iOS;
//        window.Show();
//    }

//    [MenuItem("XiYouEditor/AssetBundle/AssetBundle For Mac", false, 3)]
//    public static void ExecuteMac()
//    {
//        if (window == null)
//        {
//            window = (AssetBundleController)GetWindow(typeof(AssetBundleController));
//        }
//        buildTarget = UnityEditor.BuildTarget.StandaloneOSX;
//        window.Show();
//    }

//    [MenuItem("XiYouEditor/AssetBundle/AssetBundle For Android", false, 4)]
//    public static void ExecuteAndroid()
//    {
//        if (window == null)
//        {
//            window = (AssetBundleController)GetWindow(typeof(AssetBundleController));
//        }
//        buildTarget = UnityEditor.BuildTarget.Android;
//        window.Show();
//    }

//    //[MenuItem("XiYouEditor/AssetBundle/AssetBundle For WebPlayer", false, 5)]
//    //public static void ExecuteWebPlayer()
//    //{
//    //    if (window == null)
//    //    {
//    //        window = (AssetBundleController)GetWindow(typeof(AssetBundleController));
//    //    }
//    //    buildTarget = UnityEditor.BuildTarget.WebPlayer;
//    //    window.Show();
//    //}

//    void OnGUI()
//    {
//        if (GUI.Button(new Rect(10f, 10f, 200f, 50f), "(1)CreateAssetBundle"))
//        {
//            CreateAssetBundle.Execute(buildTarget);
//            EditorUtility.DisplayDialog("", "Step (1) Completed", "OK");
//        }

//        if (GUI.Button(new Rect(10f, 80f, 200f, 50f), "(2)Generate MD5"))
//        {
//            CreateMD5List.Execute(buildTarget);
//            EditorUtility.DisplayDialog("", "Step (2) Completed", "OK");
//        }

//        if (GUI.Button(new Rect(10f, 150f, 200f, 50f), "(3)Compare MD5"))
//        {
//            CampareMD5ToGenerateVersionNum.Execute(buildTarget);
//            EditorUtility.DisplayDialog("", "Step (3) Completed", "OK");
//        }

//        if (GUI.Button(new Rect(10f, 220f, 200f, 50f), "(4)Build VersionNum.xml"))
//        {
//            CreateAssetBundleForXmlVersion.Execute(buildTarget);
//            EditorUtility.DisplayDialog("", "Step (4) Completed", "OK");
//        }
//    }

//    public static string GetPlatformPath(UnityEditor.BuildTarget target)
//    {
//        string SavePath = "";
//        switch (target)
//        {
//            case BuildTarget.StandaloneWindows:
//                SavePath = "Assets/AssetBundle/Windows32/";
//                break;
//            case BuildTarget.StandaloneWindows64:
//                SavePath = "Assets/AssetBundle/Windows64/";
//                break;
//            case BuildTarget.iOS:
//                SavePath = "Assets/AssetBundle/IOS/";
//                break;
//            case BuildTarget.StandaloneOSX:
//                SavePath = "Assets/AssetBundle/Mac/";
//                break;
//            case BuildTarget.Android:
//                SavePath = "Assets/AssetBundle/Android/";
//                break;

//            default:
//                SavePath = "Assets/AssetBundle/";
//                break;
//        }

//        if (Directory.Exists(SavePath) == false)
//            Directory.CreateDirectory(SavePath);

//        return SavePath;
//    }

//    public static string GetPlatformName(UnityEditor.BuildTarget target)
//    {
//        string platform = "Windows32";
//        switch (target)
//        {
//            case BuildTarget.StandaloneWindows:
//                platform = "Windows32";
//                break;
//            case BuildTarget.StandaloneWindows64:
//                platform = "Windows64";
//                break;
//            case BuildTarget.iOS:
//                platform = "IOS";
//                break;
//            case BuildTarget.StandaloneOSX:
//                platform = "Mac";
//                break;
//            case BuildTarget.Android:
//                platform = "Android";
//                break;
//            default:
//                break;
//        }
//        return platform;
//    }

//}