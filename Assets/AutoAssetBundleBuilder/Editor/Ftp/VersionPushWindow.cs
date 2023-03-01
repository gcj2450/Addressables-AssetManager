//using UnityEngine;
//using System.Collections;
//using UnityEditor;
//using System.Collections.Generic;
//using System;
//using System.IO;
//using Newtonsoft.Json.Linq;
//using BaiduBce.Services.Bos;
//using BaiduBce.Services.Bos.Model;
//using System.Threading;
//using BaiduBce.Auth;
//using BaiduBce;
//using System.Net;
//using System.Text;
//using BaiduBce.Services.Sts.Model;
//using System.Runtime.InteropServices.ComTypes;
//using UnityEngine.Experimental.Rendering;

//public enum PushPlatform
//{
//    win64,
//    android,
//    ios
//}
//public class VersionPushWindow : EditorWindow
//{
//    [MenuItem("Window/VersionPushWindow")]
//    public static void ShowWindow()
//    {
//        VersionPushWindow window=EditorWindow.GetWindow(typeof(VersionPushWindow))as VersionPushWindow;
//        window.Show();
//        //Initialize("Default");
//    }

//    string dllVersionStr = "1.0.0";
//    PushPlatform dllPlatformStr = PushPlatform.win64;
//    string dllAppVerStr = "1.0.0";
//    string dllDownloadUrlStr = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/android/1666000153/BC.dll.byte";

//    string resVersionStr = "1.0.0";
//    PushPlatform resPlatformStr = PushPlatform.win64;
//    string resAppVerStr = "1.0.0";
//    string resDownloadUrlStr = "https://zion-sdk-download.baidu-int.com/download/yuanbang/";

//    string sceneVersionStr = "1.0.0";
//    string sceneNameStr = "1.0.0";
//    PushPlatform scenePlatformStr = PushPlatform.win64;
//    string sceneAppVerStr = "1.0.0";
//    string sceneDownloadUrlStr = "https://zion-sdk-download.baidu-int.com/download/yuanbang/";

//    void OnGUI()
//    {
//        dllAppVerStr = Application.version;
//        resAppVerStr = Application.version;
//        sceneAppVerStr = Application.version;

//        VersionPushHelper versionPushHelper = new VersionPushHelper();
//        GUILayout.Space(10);
//        GUILayout.Label("推送类型: bc-dll");
//        dllAppVerStr = EditorGUILayout.TextField("App Version: ", dllAppVerStr);
//        dllPlatformStr = (PushPlatform)EditorGUILayout.EnumPopup("Dll Platform: ", dllPlatformStr);
//        dllVersionStr = EditorGUILayout.TextField("Dll Version: ", dllVersionStr);
//        dllDownloadUrlStr = EditorGUILayout.TextField("DownloadUrl: ", dllDownloadUrlStr);
//        GUILayout.Space(10);
//        //推送到测试环境
//        if (GUILayout.Button("PushBC-Dll Version To Ftp Env"))
//        {
//            //string url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/"+ platformStr + "/" + dllVersionStr + "/BC.dll.byte";
//            versionPushHelper.PushLatestBCDllVersion(dllAppVerStr, dllPlatformStr.ToString(), dllVersionStr, dllDownloadUrlStr, false);
//        }

//        if (GUILayout.Button("PushBC-Dll Version To Online Env"))
//        {
//            //string url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/" + platformStr + "/" + dllVersionStr + "/BC.dll.byte";
//            versionPushHelper.PushLatestBCDllVersion(dllAppVerStr, dllPlatformStr.ToString(), dllVersionStr, dllDownloadUrlStr, true);
//        }

//        GUILayout.Space(20);
//        GUILayout.Label("推送类型: base-res");
//        resAppVerStr = EditorGUILayout.TextField("App Version: ", resAppVerStr);
//        resPlatformStr = (PushPlatform)EditorGUILayout.EnumPopup("BaseRes Platform : ", resPlatformStr);
//        resVersionStr = EditorGUILayout.TextField("BaseRes Version: ", resVersionStr);
//        resDownloadUrlStr = EditorGUILayout.TextField("DownloadUrl: ", resDownloadUrlStr);
//        GUILayout.Space(10);
//        //推送到测试环境
//        if (GUILayout.Button("PushBase-Res Version To Ftp Env"))
//        {
//            //string url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/"+ platformStr + "/" + dllVersionStr + "/BC.dll.byte";
//            versionPushHelper.PushLatestBaseResVersion(resAppVerStr, resPlatformStr.ToString(), resVersionStr, resDownloadUrlStr, false);
//        }

//        if (GUILayout.Button("PushBase-Res Version To Online Env"))
//        {
//            //string url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/" + platformStr + "/" + dllVersionStr + "/BC.dll.byte";
//            versionPushHelper.PushLatestBaseResVersion(resAppVerStr, resPlatformStr.ToString(), resVersionStr, resDownloadUrlStr, true);
//        }

//        GUILayout.Space(20);
//        GUILayout.Label("推送类型: Scene-AB");
//        sceneAppVerStr = EditorGUILayout.TextField("App Version: ", sceneAppVerStr);
//        sceneNameStr = EditorGUILayout.TextField("Scene Name: ", sceneNameStr);
//        scenePlatformStr = (PushPlatform)EditorGUILayout.EnumPopup("Scene Platform : ", scenePlatformStr);
//        sceneVersionStr = EditorGUILayout.TextField("Scene Version: ", sceneVersionStr);
//        sceneDownloadUrlStr = EditorGUILayout.TextField("DownloadUrl: ", sceneDownloadUrlStr);
//        GUILayout.Space(10);
//        //推送到测试环境
//        if (GUILayout.Button("PushScene-AB Version To Ftp Env"))
//        {
//            //string url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/"+ platformStr + "/" + dllVersionStr + "/BC.dll.byte";
//            versionPushHelper.PushLatestSceneABVersion(sceneNameStr, sceneAppVerStr, resPlatformStr.ToString(), sceneVersionStr, sceneDownloadUrlStr, false);
//        }

//        if (GUILayout.Button("PushScene-AB Version To Online Env"))
//        {
//            //string url = "https://zion-sdk-download.baidu-int.com/download/yuanbang/dll/bc/" + platformStr + "/" + dllVersionStr + "/BC.dll.byte";
//            versionPushHelper.PushLatestSceneABVersion(sceneNameStr, sceneAppVerStr, resPlatformStr.ToString(), sceneVersionStr, sceneDownloadUrlStr, true);
//        }

//    }


//}
