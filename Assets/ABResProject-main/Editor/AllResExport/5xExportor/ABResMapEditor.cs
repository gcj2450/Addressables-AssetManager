using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Asgard;

public class ABResMapEditor
{
    private static string filePath = ABResPathConfig.ABResourceMapFileAssetPath + "/" + ABResPathConfig.ABResourceMapFileFullName;
    private static string filePathVersionTxt = ABResPathConfig.ABResourceMapFileAssetPath + "/" + ABResPathConfig.ABVersionFileName + ".txt";
    private static ABResMapScriptObj abResMapObj = null;

    /// <summary>
    /// 打开文件到对象
    /// </summary>
    /// <returns></returns>
    private static void OpenAbResourceMap()
    {
        if (!File.Exists(filePath))
        {
            abResMapObj = ABResMapScriptObj.CreateInstance<ABResMapScriptObj>();
        }
        else
        {
            abResMapObj = AssetDatabase.LoadAssetAtPath<ABResMapScriptObj>(filePath);
        }

    }

    /// <summary>
    /// 存储对象到文件 (每次对Res对象更改之后 需要存储到文件的时候 必须从外面手动调用这个保存一下)
    /// </summary>
    public static void SaveAbResourceMap()
    {
        if (!File.Exists(filePath))
        {
            if (!Directory.Exists(ABResPathConfig.ABResourceMapFileAssetPath))
                Directory.CreateDirectory(ABResPathConfig.ABResourceMapFileAssetPath);
            AssetDatabase.CreateAsset(abResMapObj, filePath);
        }
        else
        {
            EditorUtility.SetDirty(abResMapObj);
            AssetDatabase.SaveAssets();
        }

        abResMapObj = null;
    }

    /// <summary>
    /// 增或改某一条
    /// </summary>
    /// <param name="addItem"></param>
    public static void AddResMapItem(ABResMapItemScriptObj addItem, bool ifMainAsset)
    {
        if (abResMapObj == null) OpenAbResourceMap();
        ABResMapItemScriptObj findItem = abResMapObj.Resources.Find(item => { return item.AssetBundleName.Equals(addItem.AssetBundleName); });
        if (findItem == null)
        {
            abResMapObj.Resources.Add(addItem);
        }
        else
        {
            findItem.AssetBundleName = addItem.AssetBundleName;
            findItem.Md5 = addItem.Md5;
            findItem.Size = addItem.Size;
            findItem.ResourceType = addItem.ResourceType;
            findItem.ABResType |= addItem.ABResType;
            if (ifMainAsset)
            {
                findItem.DependAssetBundleName = addItem.DependAssetBundleName;
                findItem.ABResLoadType = addItem.ABResLoadType;
            }
        }
    }

    /// <summary>
    /// 清空Res对象
    /// </summary>
    public static void ClearResMap()
    {
        if (abResMapObj == null) { OpenAbResourceMap(); }
        abResMapObj.Resources.RemoveAll(item =>
        {
            bool condition = item.AssetBundleName.Equals(ABResPathConfig.ABResMapAssetBundleName) || item.AssetBundleName.Equals(ABResPathConfig.ABVersionFileAssetBundleName);
            return !condition;
        });

    }

    /// <summary>
    /// 设置version
    /// </summary>
    /// <param name="version"></param>
    public static void SetVersion(float version)
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "USEAB");
        //设置版本号
        if (abResMapObj == null) OpenAbResourceMap();
        abResMapObj.Version = version;
        SaveAbResourceMap();

        //创建version文件 并 保存
        if (File.Exists(filePathVersionTxt))
        {
            File.Delete(filePathVersionTxt);
        }
        if (!Directory.Exists(ABResPathConfig.ABResourceMapFileAssetPath))
            Directory.CreateDirectory(ABResPathConfig.ABResourceMapFileAssetPath);
        using (FileStream fs = new FileStream(filePathVersionTxt, FileMode.Create))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(version.ToString());
                sw.Flush();
            }
        }
        //FileStream fs = new FileStream(filePathVersionTxt, FileMode.Create);
        //StreamWriter sw = new StreamWriter(fs);

        AssetDatabase.Refresh();
        TextAsset tx = AssetDatabase.LoadAssetAtPath<TextAsset>(filePathVersionTxt);
        AssetImporter importer = AssetImporter.GetAtPath(filePathVersionTxt);
        importer.SaveAndReimport();

        //打包两个文件
        List<FileInfo> fileInfos = new List<FileInfo> { new FileInfo(filePath), new FileInfo(filePathVersionTxt) };
        fileInfos.ForEach(item => { ABResBuilder.BuildAssetBundleByFile(item); });

    }

    public static float Version
    {
        get
        {
            if (abResMapObj == null) OpenAbResourceMap();
            return abResMapObj.Version;
        }
    }

    /// <summary>
    /// 设置platform
    /// </summary>
    /// <param name="platform"></param>
    public static void SetPlatform(string platform)
    {
        if (abResMapObj == null) OpenAbResourceMap();
        abResMapObj.Platform = platform;
    }

    public static ABResMapItemScriptObj GetAbResMapItem(string assetBundleName)
    {
        if (!assetBundleName.EndsWith(".unity3d")) { assetBundleName = assetBundleName + ".unity3d"; }
        if (abResMapObj == null) OpenAbResourceMap();
        ABResMapItemScriptObj result = abResMapObj.Resources.Find(item => { return item.AssetBundleName.Equals(assetBundleName.ToLower()); });
        abResMapObj = null;
        return result;
    }

    public static List<ABResMapItemScriptObj> GetAllAbResMapItem()
    {
        if (abResMapObj == null) OpenAbResourceMap();
        return abResMapObj.Resources;
    }
}
