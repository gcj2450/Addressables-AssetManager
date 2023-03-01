//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Net;
//using System.Security.Cryptography;
//using System.Text;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.Experimental.GlobalIllumination;
//using UnityEngine.UIElements;
///// <summary>
///// 版本推送接口
///// </summary>
///// 

//public class VersionPushHelper
//{
//    /// <summary>
//    /// 推送地址测试环境地址
//    /// </summary>
//    string pushTestHost = "http://10.154.78.12:8081";
//    /// <summary>
//    /// 推送开发环境地址
//    /// </summary>
//    string pushDevHost = "http://10.154.78.12:8082";
//    /// <summary>
//    /// 推送线上环境地址
//    /// </summary>
//    string pushOnlineHost = "https://tcm-bmeta.baidu.com";

//    /// <summary>
//    /// 用于拼接url的后缀
//    /// "/version/entry/v1"
//    /// </summary>
//    string uri = "/version/entry/v1";

//    string tenantID = "yuanbang";

//    /// <summary>
//    /// 测试环境资源下载地址
//    /// 王辉ftp上的资源下载地址
//    /// </summary>
//    //string bundleResUrl = "https://zion-sdk-download.baidu-int.com/download/yuanbang/";

//    /// <summary>
//    /// 推送BaseRes Bundle版本
//    /// </summary>
//    /// <param name="_appVersion">app版本号，Application.version</param>
//    /// <param name="_platform">平台:android,ios,win64 </param>
//    /// <param name="_resVersion">资源版本号</param>
//    public void PushLatestBaseResVersion(
//        string _appVersion, string _platform, string _resVersion, 
//        string _downloadUrl = "", bool _isOnline = false,bool _isDev=false)
//    {
//        VersionPushData versionPushData = new VersionPushData();
//        versionPushData.app_id = tenantID;
//        //应用版本号
//        versionPushData.app_version = _appVersion;
//        //资源平台android,ios,win64
//        versionPushData.platform = _platform;
//        //资源版本号
//        versionPushData.ver = _resVersion;
//        //资源类型
//        versionPushData.type = "base-res";
//        versionPushData.name = "base-res";
//        //资源下载地址
//        versionPushData.url = _downloadUrl;

//        PushVersionData(versionPushData, _isOnline, _isDev);
//    }

//    /// <summary>
//    /// 推送最新的场景AB包版本
//    /// </summary>
//    /// <param name="_sceneName"></param>
//    /// <param name="_appVersion"></param>
//    /// <param name="_platform"></param>
//    /// <param name="_sceneABVersion"></param>
//    public void PushLatestSceneABVersion(
//        string _sceneName, string _appVersion, string _platform, 
//        string _sceneABVersion, string _downloadUrl = "", bool _isOnline = false, bool _isDev = false)
//    {
//        VersionPushData versionPushData = new VersionPushData();
//        versionPushData.app_id = tenantID;
//        versionPushData.scene_id = _sceneName;
//        versionPushData.app_version = _appVersion;
//        versionPushData.platform = _platform;
//        versionPushData.ver = _sceneABVersion;
//        versionPushData.type = "scene-ab";
//        versionPushData.name = "scene-ab";
//        //下载地址
//        versionPushData.url = _downloadUrl;
//        PushVersionData(versionPushData, _isOnline, _isDev);
//    }

//    /// <summary>
//    /// 推送最新的bc-dll资源版本
//    /// </summary>
//    /// <param name="_sceneName"></param>
//    /// <param name="_appVersion"></param>
//    /// <param name="_platform"></param>
//    /// <param name="_bcDllVersion">bc-dll版本号</param>
//    public void PushLatestBCDllVersion(
//        string _appVersion, string _platform, string _bcDllVersion, 
//        string _downloadUrl = "", bool _isOnline = false, bool _isDev = false)
//    {
//        VersionPushData versionPushData = new VersionPushData();
//        versionPushData.app_id = tenantID;
//        //versionPushData.scene_id = _sceneName;
//        versionPushData.app_version = _appVersion;
//        versionPushData.platform = _platform;
//        versionPushData.ver = _bcDllVersion;
//        versionPushData.type = "bc-dll";
//        versionPushData.name = "bc-dll";
//        //下载地址
//        versionPushData.url = _downloadUrl;
//        PushVersionData(versionPushData, _isOnline, _isDev);
//    }

//    public void PushLatestSceneDllVersion(
//        string _appVersion, string _platform, string _sceneName,string _sceneDllVersion,
//        string _downloadUrl = "", bool _isOnline = false, bool _isDev = false)
//    {
//        VersionPushData versionPushData = new VersionPushData();
//        versionPushData.app_id = tenantID;
//        versionPushData.scene_id = _sceneName;
//        versionPushData.app_version = _appVersion;
//        versionPushData.platform = _platform;
//        versionPushData.ver = _sceneDllVersion;
//        versionPushData.type = "scene-dll";
//        versionPushData.name = "scene-dll";
//        //下载地址
//        versionPushData.url = _downloadUrl;
//        PushVersionData(versionPushData, _isOnline, _isDev);
//    }

//    /// <summary>
//    /// 推送版本信息接口
//    /// </summary>
//    /// <param name="_versionPushData">推送的环境</param>
//    /// <param name="isOnline">是否为线上生产环境</param>
//    /// <param name="isDev">是否为开发环境，否则就是测试环境</param>
//    void PushVersionData(VersionPushData _versionPushData, bool isOnline = false,bool isDev=false)
//    {
//        string url = "";
//        if (isOnline)
//        {
//            url = pushOnlineHost;
//        }
//        else
//        {
//            if (isDev)
//            {
//                url = pushDevHost;
//            }
//            else
//            {
//                url = pushTestHost;
//            }
//        }

//        string json = JsonUtility.ToJson(_versionPushData);
//        string result = HttpPost(url, uri, json);
//        Debug.Log("PushVersionData: " + result);
//    }

//    /// <summary>
//    /// 发送一个http请求
//    /// </summary>
//    /// <param name="url"></param>
//    /// <param name="postDataStr"></param>
//    /// <returns></returns>
//    string HttpPost(string host, string uri, string postDataStr)
//    {
//        WebRequest request = WebRequest.Create(host + uri);
//        request.Headers = new WebHeaderCollection();

//        Debug.Log("AAAGetVersionToken: " + GetVersionToken(GlobalConfig.Instance.VersionAppKey, uri));
//        Debug.Log("App:  " + GlobalConfig.Instance.VersionAppId);
//        //return "";
//        request.Headers.Add("Token", GetVersionToken(GlobalConfig.Instance.VersionAppKey, uri));
//        request.Headers.Add("App", GlobalConfig.Instance.VersionAppId);
//        request.Method = "POST";
//        request.ContentType = "application/json";
//        byte[] buf = Encoding.UTF8.GetBytes(postDataStr);
//        byte[] byteArray = System.Text.Encoding.Default.GetBytes(postDataStr);
//        request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
//        request.GetRequestStream().Write(buf, 0, buf.Length);
//        WebResponse response = request.GetResponse();
//        Stream myResponseStream = response.GetResponseStream();
//        StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
//        string retString = myStreamReader.ReadToEnd();
//        myStreamReader.Close();
//        myResponseStream.Close();
//        return retString;
//    }

//    /// <summary>
//    /// 对版本服务请求uri签名
//    /// Token值计算方法如下：
//    //（1）获得客户端的API_KEY（测试环境默认为：“xr-2022-test”）
//    //（2）将 API_KEY ＋URI 拼接成一个字符串（URI不含host和port）
//    //（3）计算上一步得到的字符串的md5，获得一个32字符长度的字符串，取其前8个字符作为Token
//    /// </summary>
//    /// <param name="appId"></param>
//    /// <param name="appKey"></param>
//    /// <param name="uri"></param>
//    /// <returns></returns>
//    /// <exception cref="ArgumentNullException"></exception>
//    /// <exception cref="Exception"></exception>
//    string GetVersionToken(string appKey, string uri)
//    {
//        if (string.IsNullOrEmpty(appKey))
//        {
//            throw new ArgumentNullException("[Version][Token] the argument: appKey is invalid.");
//        }
//        if (string.IsNullOrEmpty(uri))
//        {
//            throw new ArgumentNullException("[Version][Token] the argument: uri is invalid.");
//        }
//        string buf = null;
//        try
//        {

//            buf = $"{appKey}{uri}";
//            Debug.Log($"[Version][Token] md5 source:{buf}");
//            buf = EncryptString(buf);
//            Debug.Log($"[Version][Token] md5 hash:{buf}");
//            string token = buf.Substring(0, 8);
//            Debug.Log($"[Version][Token] ver token:{token}");
//            return token;
//        }
//        catch (Exception e)
//        {
//            throw new Exception($"[Version][Token] Failed to calculate the MD5-Hash, source: [{buf}], cause by: {e}");
//        }


//    }

//    string EncryptString(string str)
//    {
//        MD5 md5 = MD5.Create();
//        // 将字符串转换成字节数组
//        byte[] byteOld = Encoding.UTF8.GetBytes(str);
//        // 调用加密方法
//        byte[] byteNew = md5.ComputeHash(byteOld);
//        // 将加密结果转换为字符串
//        StringBuilder sb = new StringBuilder();
//        foreach (byte b in byteNew)
//        {
//            // 将字节转换成16进制表示的字符串，
//            sb.Append(b.ToString("x2"));
//        }
//        // 返回加密的字符串
//        return sb.ToString();
//    }
//}

///// <summary>
///// 版本推送接口数据
///// 这里的数据是可以自定义的，需要和获取时候保持一致
///// </summary>
//public class VersionPushData
//{
//    /// <summary>
//    /// app名称
//    /// </summary>
//    public string app_id = "yuanbang";
//    /// <summary>
//    /// 资源平台：win64,android,ios
//    /// </summary>
//    public string platform = "win64";
//    /// <summary>
//    /// app版本，Application.version
//    /// </summary>
//    public string app_version = "0.1.5";
//    /// <summary>
//    /// 场景ID
//    /// </summary>
//    public string scene_id = "0";
//    /// <summary>
//    /// 资源类型 VerResourceType：app,bc-dll等
//    /// resourceType.ToResourceSymbol()
//    /// </summary>
//    public string type = "scene-ab";
//    /// <summary>
//    /// 资源名称
//    /// </summary>
//    public string name = "scene-ab";
//    /// <summary>
//    /// 推送的版本号
//    /// </summary>
//    public string ver = "2022_09_30_12_13";
//    /// <summary>
//    /// 该资源的下载地址
//    /// </summary>
//    public string url = "https://a.b.com/bc-dll/20221001/";
//    /// <summary>
//    /// 备注
//    /// </summary>
//    public string note = "";
//    public int level = 10;
//    public VersionPushData() { }

//    public VersionPushData
//        (
//     string _app_id = "yuanbang",
//     string _platform = "win64",
//     string _app_version = "0.1.5",
//     string _scene_id = "0",
//     string _type = "scene-ab",
//     string _name = "scene-ab",
//     string _ver = "2022_09_30_12_13",
//     string _url = "https://a.b.com/bc-dll/20221001/",
//     string _note = "",
//     int _level = 10
//        )
//    {
//        app_id = _app_id;
//        platform = _platform;
//        app_version = _app_version;
//        scene_id = _scene_id;
//        type = _type;
//        name = _name;
//        ver = _ver;
//        url = _url;
//        note = _note;
//        level = _level;
//    }
//}
