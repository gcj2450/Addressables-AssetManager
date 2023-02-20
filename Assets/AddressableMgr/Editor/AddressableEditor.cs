using BaiduFtpUploadFiles;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEngine;
/// <summary>
/// Addressable打包窗口
/// </summary>
public class AddressableEditor : EditorWindow
{
    [MenuItem("Window/AddressableEditor")]
    static void Init()
    {
        // Show existing open window, or make new one.
        AddressableEditor window = EditorWindow.GetWindow(typeof(AddressableEditor)) as AddressableEditor;
        window.Show();
    }
    const string applicationName = "Addressfolder";
    GUIContent AddressableFolderLabel = new GUIContent("AddressableFolder");
    /// <summary>
    /// 需要打包的文件夹
    /// </summary>
    string addressFolder = "";

    void OnGUI()
    {
        //读取之前设置的文件夹
        addressFolder = EditorPrefs.GetString(applicationName + " addressFolder", "");
        //设置默认文件夹
        if (string.IsNullOrEmpty(addressFolder))
            addressFolder = "AutoBundles/";
        //读取输入的文件夹名
        addressFolder = EditorGUILayout.TextField(AddressableFolderLabel, addressFolder);
        EditorPrefs.SetString(applicationName + " addressFolder", addressFolder);

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        //收集资源，添加进Addressable Group中
        if (GUILayout.Button("CollectAssets"))
        {
            AutoGroup("Packed Assets", addressFolder, true);
        }

        EditorGUILayout.EndHorizontal();
    }

    //====================Editormethods=========================
    /// <summary>
    /// 将资源放进指定的group内
    /// </summary>
    /// <param name="groupName">指定的组名兼label名</param>
    /// <param name="assetPath">资源路径</param>
    /// <param name="simplifed">是否简化address，去后缀</param>
    public static void AutoGroup(string groupName, string rootFolder, bool simplifed = false)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (group == null)
        {
            Debug.Log("group not found : " + groupName);

            List<AddressableAssetGroupSchema> schemasToCopy =
                new List<AddressableAssetGroupSchema>
                {
                    settings.DefaultGroup.Schemas[0],settings.DefaultGroup.Schemas[1]
                };
            group = settings.CreateGroup(groupName, false, false, false, schemasToCopy, typeof(SchemaType));
        }

        List<string> files = new List<string>();
        RecursiveFile(Application.dataPath + "/" + rootFolder, ref files);
        for (int i = 0, cnt = files.Count; i < cnt; i++)
        {
            string assetPath = files[i];
            //去掉Assets/之前的路径字符串
            string fPath = GetRelativePath(assetPath, Application.dataPath.Replace("Assets", ""));
            //去掉后缀
            string address = fPath.Replace(new FileInfo(fPath).Extension, "");
            //"\"替换为"/"
            address = address.Replace("\\", "/");
            //去掉rootFolder之前的路径
            address = address.Replace("Assets/" + rootFolder, "");

            SetAaEntry(settings, group, fPath, true, address);
        }
    }

    static void SetAaEntry(AddressableAssetSettings aaSettings, AddressableAssetGroup assetGroup, string path, bool create, string address = "")
    {
        if (create && assetGroup.ReadOnly)
        {
            Debug.LogError("Current default group is ReadOnly.  Cannot add addressable assets to it");
            return;
        }

        Undo.RecordObject(aaSettings, "AddressableAssetSettings");
        var guid = string.Empty;
        //if (create || EditorUtility.DisplayDialog("Remove Addressable Asset Entries", "Do you want to remove Addressable Asset entries for " + targets.Length + " items?", "Yes", "Cancel"))
        {
            var entriesAdded = new List<AddressableAssetEntry>();
            var modifiedGroups = new HashSet<AddressableAssetGroup>();
            guid = AssetDatabase.AssetPathToGUID(path);
            if (create)
            {
                var e = aaSettings.CreateOrMoveEntry(guid, assetGroup, false, false);
                if (!string.IsNullOrEmpty(address)) e.address = address;
                entriesAdded.Add(e);
                modifiedGroups.Add(e.parentGroup);
            }
            else
            {
                aaSettings.RemoveAssetEntry(guid);
            }

            if (create)
            {
                foreach (var g in modifiedGroups)
                    g.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, false, true);
                aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true, false);
            }
        }
    }

    /// <summary>
    /// 获取对应路径的相对路径（如absolutePath=E:/AB/c.txt,relativeTo=E:/AB/，输出c.txt）
    /// </summary>
    /// <param name="absolutePath"></param>
    /// <param name="relativeTo"></param>
    /// <returns></returns>
    static string GetRelativePath(string absolutePath, string relativeTo)
    {
        var fileInfo = new FileInfo(relativeTo);
        var fullFileInfo = new FileInfo(absolutePath);
        string absoluteName = fullFileInfo.FullName;
        absoluteName = MakePathPerfect(absoluteName);
        string relative = fileInfo.FullName;
        relative = MakePathPerfect(relative);
        string result = absoluteName.Replace(relative, "");
        return result;
    }

    static string MakePathPerfect(string path)
    {
        return path.Replace("\\", "/");
    }

    /// <summary>
    /// 遍历指定文件夹中的文件包括子文件夹的文件
    /// </summary>
    /// <param name="result">遍历之后的结果</param>
    /// <returns></returns>
    static void RecursiveFile(string filePathByForeach, ref List<string> results)
    {
        DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
        DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
        FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件

        foreach (FileInfo fileItem in file) //遍历文件
        {
            //不包含meta文件
            if (fileItem.Extension != ".meta")
                results.Add(fileItem.FullName.Replace("\\", "/"));
        }
        //遍历文件夹
        foreach (DirectoryInfo NextFolder in dirInfo)
        {
            RecursiveFile(NextFolder.FullName, ref results);
        }
    }
}
