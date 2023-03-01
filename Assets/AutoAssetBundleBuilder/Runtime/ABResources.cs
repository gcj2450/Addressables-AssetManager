using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using UnityEngine;

/// <summary>
/// AssetBundle加载，用于代替Resources类
/// </summary>
public class ABResources
{
    /// <summary>
    /// 加载Bundle内的文件,类似Resources.Load，注意需要带后缀名
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Object Load(string path)
    {
        //if (path.Contains(".prefab"))
        //    path = path.Replace(".prefab", "");
        //if (path.StartsWith("/"))
        //{
        //    path = path.Substring(1);
        //}
        //Debug.Log("AAAAAAAApath：" + path);
        //return Resources.Load(path);
        //if (path.Contains("/UI"))
        //    path = path.Replace("/UI", "");
       
        return ZionGame.ABAssetLoader.Load(path);
    }

    /// <summary>
    /// 返回指定类型的Asset
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public static T Load<T>(string path) where T:Object
    {
        //if (path.Contains(".prefab"))
        //    path = path.Replace(".prefab", "");
        //if (path.StartsWith("/"))
        //{
        //    path = path.Substring(1);
        //}
        //Debug.Log("AAAAAAAApath + path: " + path);
        //return Resources.Load<T>(path);
        //if (path.Contains("/UI"))
        //    path=path.Replace("/UI", "");

        return ZionGame.ABAssetLoader.GetAsset<T>(path);
    }

}
