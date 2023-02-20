using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using Asgard;
using Asgard.Resource;
using System;

public class ABResBuilder
{
    //[MenuItem("Assets/输出路径")]
    //public static void PrintPath()
    //{
    //    Debug.Log(Application.persistentDataPath);
    //}

    public static void ClearNoUseResource()
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "USEAB");
        List<ABResMapItemScriptObj> resources = ABResMapEditor.GetAllAbResMapItem();
        ABResMapEditor.SaveAbResourceMap();
        System.IO.DirectoryInfo dic = new DirectoryInfo(ABResPathConfig.UrlPrefixCdn);
        if (dic == null || !dic.Exists)
        {
            Debug.LogError("不存在这个目录 " + ABResPathConfig.UrlPrefixCdn);
            return;
        }
        FileInfo[] files = dic.GetFiles("*.*", SearchOption.AllDirectories);
        if (files != null && files.Length > 0)
        {
            for (int i = 0; i < files.Length; i++)
            {
                bool condition1 = files[i].Extension.Equals(".manifest");
                if (condition1)
                {
                    EditorUtility.DisplayProgressBar("清理资源", "正在清理第 " + i + " / " + files.Length + " 个资源", i / (float)files.Length);
                    files[i].Delete();
                    continue;
                }
                string fileFullName = files[i].FullName.Replace("\\", "/");
                string assetBundleName = fileFullName.Substring(ABResPathConfig.UrlPrefixCdn.Length);
                List<ABResMapItemScriptObj> result = resources.FindAll(item => { return item.AssetBundleName.Equals(assetBundleName); });
                if (result == null || result.Count == 0)
                {
                    EditorUtility.DisplayProgressBar("清理资源", "正在清理第 " + i + " / " + files.Length + " 个资源", i / (float)files.Length);
                    files[i].Delete();
                }
            }
        }

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("清理资源", "清理资源结束", "关闭");

    }


    [MenuItem("Assets/打包面板")]
    public static void setAbVersion()
    {
        EditorWindow.GetWindow<SetVersionWindow>().Show();
    }
    [MenuItem("Assets/显示资源信息")]
    public static void showResource()
    {
        List<UnityEngine.Object> selectedDics = new List<UnityEngine.Object>(Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets));
        selectedDics.RemoveAll(item =>
        {
            return Directory.Exists(AssetDatabase.GetAssetPath(item.GetInstanceID()));
        });
        if (selectedDics.Count <= 0) { EditorUtility.DisplayDialog("错误", "选择的文件数量为0", "确定"); return; }

        SetVersionWindow window = EditorWindow.GetWindow<SetVersionWindow>();
        window.obj = selectedDics[0];
        window.Show();
    }

    [MenuItem("Assets/打包选中资源")]
    public static void buildAssetBundleBySelect()
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "USEAB");
        EditorUtility.DisplayProgressBar("打包资源", "正在处理选中资源..", 0.1f);
        List<UnityEngine.Object> selectedDics = new List<UnityEngine.Object>(Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets));
        selectedDics.RemoveAll(item =>
        {
            return Directory.Exists(AssetDatabase.GetAssetPath(item.GetInstanceID()));
        });
        List<string> assetPaths = CheckSelectedFiles(selectedDics);
        if (assetPaths == null) return;
        for (int i = 0; i < assetPaths.Count; i++)
        {
            EditorUtility.DisplayProgressBar("打包资源", "正在打包资源 " + i + "/" + assetPaths.Count, i / (float)assetPaths.Count);
            Debug.Log(assetPaths[i]);
            BuildAssetBundleByFile(new FileInfo(assetPaths[i]));
        }

        EditorUtility.DisplayProgressBar("打包资源", "正在保存资源信息..", 0.1f);
        ABResMapEditor.SaveAbResourceMap();
        ABResMapEditor.SetVersion(ABResMapEditor.Version);
        ABResMapEditor.SaveAbResourceMap();
        EditorUtility.DisplayDialog("打包资源", "打包结束！", "关闭");

    }

    /// <summary>
    /// 打包buildResPath下面所有文件
    /// </summary>
    public static void BuildAllAssetBundle()
    {
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "USEAB");
        EditorUtility.DisplayProgressBar("打包所有资源", "正在准备..", 0.1f);
        ABResMapEditor.ClearResMap();
        ABResMapEditor.SetPlatform(Enum.GetName(typeof(BuildTarget), BuildTarget.StandaloneWindows));
        int mainCount = 0;
        foreach (KeyValuePair<string, int> pair in Asgard.ABResChecker.buildResDic)
        {
            DirectoryInfo dicInfo = new DirectoryInfo(pair.Key);
            BuildAssetBundleByDic(dicInfo, ++mainCount, Asgard.ABResChecker.buildResDic.Count);
        }
        EditorUtility.DisplayProgressBar("打包资源", "正在保存资源信息..", 0.1f);
        ABResMapEditor.SaveAbResourceMap();
        ABResMapEditor.SetVersion(ABResMapEditor.Version);
        ABResMapEditor.SaveAbResourceMap();
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("打包资源", "打包结束！请手动设置资源版本号", "关闭");
    }

    private static BuildTarget buildTarget = BuildTarget.StandaloneWindows;

    /// <summary>
    /// 根据文件夹打包AB
    /// </summary>
    /// <param name="dicInfo"></param>
    public static void BuildAssetBundleByDic(DirectoryInfo dicInfo, int mainCount, int MaxMainCount)
    {
        if (!dicInfo.Exists) { Debug.LogError("不存在这个文件夹 " + dicInfo.FullName); return; }
        FileInfo[] fileInfos = dicInfo.GetFiles("*.*", SearchOption.AllDirectories);
        if (fileInfos == null || fileInfos.Length == 0)
        {
            Debug.LogError("目录 " + dicInfo.FullName + " 下面没有文件!");
            EditorUtility.ClearProgressBar();
            return;
        }
        List<FileInfo> fileInfoList = new List<FileInfo>(fileInfos);
        fileInfoList.RemoveAll(item => { return item.Extension.Equals(".meta"); });
        if (fileInfoList.Count == null) { Debug.LogError("目录 " + dicInfo.FullName + " 下面没有可以打包的资源文件！"); EditorUtility.ClearProgressBar(); return; }
        int subCount = 0;
        fileInfoList.ForEach(item =>
        {
            ++subCount;
            EditorUtility.DisplayProgressBar("打包资源 " + mainCount + "/" + MaxMainCount, "正在打包资源 " + subCount + "/" + fileInfoList.Count, subCount / (float)fileInfoList.Count);
            BuildAssetBundleByFile(item);
        });
    }
    /// <summary>
    /// 根据文件打包AB
    /// </summary>
    /// <param name="resPath"></param>
    public static void BuildAssetBundleByFile(FileInfo fileInfo)
    {
        string assetName = fileInfo.FullName.Substring(fileInfo.FullName.IndexOf("Assets")).Replace("\\", "/");
        string assetBundleName = GetAssetBundleName(assetName);
        List<string> depenFiles = new List<string>(AssetDatabase.GetDependencies(assetName));
        List<string> depenAssetBundleNames = new List<string>();

        depenFiles = GetFliterFiles(assetName, depenFiles);
        for (int i = 0; i < depenFiles.Count; i++)
        {
            depenAssetBundleNames.Add(GetAssetBundleName(depenFiles[i]));
        }

        //开始打包
        List<AssetBundleBuild> assetBuildList = new List<AssetBundleBuild>();
        AssetBundleBuild mainAssetBuild = new AssetBundleBuild();
        mainAssetBuild.assetBundleName = assetBundleName;
        mainAssetBuild.assetNames = new string[] { assetName /*, dependAssetPath*/ };
        assetBuildList.Add(mainAssetBuild);

        for (int j = 0; j < depenFiles.Count; j++)
        {
            AssetBundleBuild subAssetBuild = new AssetBundleBuild();
            subAssetBuild.assetBundleName = depenAssetBundleNames[j];
            subAssetBuild.assetNames = new string[] { depenFiles[j] };
            assetBuildList.Add(subAssetBuild);
        }

        BuildPipeline.BuildAssetBundles(Asgard.ABResPathConfig.UrlPrefixCdn, assetBuildList.ToArray(), BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        int abResLoadType = GetABResLoadType(assetName);
        //写入ResMap文件
        for (int k = 0; k < assetBuildList.Count; k++)
        {
            string tempAssetName = assetBuildList[k].assetNames[0];
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(tempAssetName, typeof(UnityEngine.Object));
            string abFileFullPath = (Asgard.ABResPathConfig.UrlPrefixCdn + assetBuildList[k].assetBundleName).Replace("\\", "/");

            ABResMapItemScriptObj resMapItem = new ABResMapItemScriptObj();
            resMapItem.AssetBundleName = assetBuildList[k].assetBundleName;
            resMapItem.Md5 = ABResChecker.GetMd5Hash(abFileFullPath).ToLower();
            resMapItem.Size = new FileInfo(abFileFullPath).Length;
            resMapItem.ResourceType = (int)GetBundleType(asset, tempAssetName);
            if (tempAssetName.Equals(assetName))
            {
                resMapItem.ABResLoadType = abResLoadType;
                resMapItem.ABResType = ABResMapScriptObj.ABResTypeMain;
                resMapItem.DependAssetBundleName = depenAssetBundleNames;
                ABResMapEditor.AddResMapItem(resMapItem, true);
            }
            else
            {
                resMapItem.ABResType = ABResMapScriptObj.ABResTypeSub;
                ABResMapEditor.AddResMapItem(resMapItem, false);
            }

        }
    }

    private static string GetAssetBundleName(string assetName)
    {
        assetName = assetName.Replace(" ", "");
        assetName = assetName.Replace("#", "");
        string tempName = assetName.Substring("Assets/".Length);
        return (tempName.Substring(0, tempName.LastIndexOf(".")) + ".unity3d").ToLower();
    }

    private static int GetABResLoadType(string assetName)
    {
        foreach (KeyValuePair<string, int> pair in Asgard.ABResChecker.buildResDic)
        {
            if (assetName.StartsWith(pair.Key))
            {
                return pair.Value;
            }
        }
        return ABResMapScriptObj.ABLoadTypeNone;
    }

    private static List<string> CheckSelectedFiles(List<UnityEngine.Object> selectedDics)
    {
        List<string> fileNames = new List<string>();
        List<string> dic = new List<string>();
        for (int i = 0; i < selectedDics.Count; i++)
        {
            UnityEngine.Object item = selectedDics[i];
            string assetPath = AssetDatabase.GetAssetPath(item.GetInstanceID());

            string fileName = assetPath.Substring(0, assetPath.LastIndexOf("."));
            if (!fileNames.Contains(fileName))
            {
                fileNames.Add(fileName);
                dic.Add(assetPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "选择的文件夹 " + fileName.Substring(0, fileName.LastIndexOf("/")) + " 下面有 相同名称的文件 请重新选择", "确定");
                return null;
            }
        }
        if (dic.Count <= 0)
        {
            EditorUtility.DisplayDialog("错误", "选择的文件数量为零或者文件夹下面没有文件", "确定");
            return null;
        }
        return dic;
    }

    /// <summary>
    /// 模型处理过滤
    /// </summary>
    /// <param name="assetName"></param>
    /// <param name="depenFiles"></param>
    /// <returns></returns>
    public static List<string> GetFliterFiles(string assetName, List<string> depenFiles)
    {
        depenFiles.RemoveAll(item =>
        {
            bool ifSelf = item.Equals(assetName);
            return ifSelf || item.EndsWith(".cs") || item.EndsWith(".js") || item.EndsWith(".shader");
        });
        int abLoadType = GetABResLoadType(assetName);
        if (abLoadType == ABResMapScriptObj.ABLoadTypeNone)
        {

        }
        else if (abLoadType == ABResMapScriptObj.ABLoadTypeTankPerfab)
        {
            depenFiles.RemoveAll(item =>
            {
                return (!item.EndsWith(".tga") && !item.EndsWith(".TGA") && !item.EndsWith(".PNG") && !item.EndsWith(".png") && !item.EndsWith(".FBX") && !item.EndsWith(".fbx") && !item.EndsWith(".mat") && !item.EndsWith(".TTF") && !item.EndsWith(".fnt"));
            });
        }
        else if (abLoadType == ABResMapScriptObj.ABLoadTypeUIPanelPrefab)
        {
            depenFiles.RemoveAll(item =>
            {
                return (!item.EndsWith(".tga") && !item.EndsWith(".TGA") && !item.EndsWith(".PNG") && !item.EndsWith(".png") && !item.EndsWith(".mat") && !item.EndsWith(".TTF") && !item.EndsWith(".fnt"));
            });
        }
        else if (abLoadType == ABResMapScriptObj.ABLoadTypeEffect)
        {
            depenFiles.RemoveAll(item =>
            {
                return (!item.EndsWith(".tga") && !item.EndsWith(".TGA") && !item.EndsWith(".PNG") && !item.EndsWith(".png") && !item.EndsWith(".FBX") && !item.EndsWith(".fbx") && !item.EndsWith(".mat") && !item.EndsWith(".TTF") && !item.EndsWith(".fnt") && !item.EndsWith(".controller") && !item.EndsWith(".anim") && !item.EndsWith(".obj"));
            });
        }
        else if (abLoadType == ABResMapScriptObj.ABLoadTypeScene)
        {
            depenFiles.RemoveAll(item =>
            {
                bool condition1 = (!item.EndsWith(".tga") && !item.EndsWith(".TGA") && !item.EndsWith(".PNG") && !item.EndsWith(".png") && !item.EndsWith(".FBX") && !item.EndsWith(".fbx") && !item.EndsWith(".mat") && !item.EndsWith(".TTF") && !item.EndsWith(".fnt"));
                bool condition2 = item.EndsWith(".asset");
                return (condition1 || condition2);
            });
        }
        //后面还要增加其他类型的文件过滤代码
        return depenFiles;
    }

    public static BaseResource.ResourceType GetBundleType(UnityEngine.Object Obj, string ObjPath)
    {
        if (ObjPath.EndsWith(".unity")) return BaseResource.ResourceType.RESOURCE_SCENE;
        if (ObjPath.EndsWith(".asset")) return BaseResource.ResourceType.RESOURCE_ASSET;
        if (ObjPath.EndsWith(".assets")) return BaseResource.ResourceType.RESOURCE_ASSET;
        if (ObjPath.EndsWith(".bytes")) return BaseResource.ResourceType.RESOURCE_BINARY;
        if (ObjPath.EndsWith(".cs")) return BaseResource.ResourceType.RESOURCE_SCRIPT;
        if (ObjPath.EndsWith(".js")) return BaseResource.ResourceType.RESOURCE_SCRIPT;
        if (ObjPath.EndsWith(".shader")) return BaseResource.ResourceType.RESOURCE_SHADER;

        if (Obj is UnityEngine.Texture)
        {
            return BaseResource.ResourceType.RESOURCE_TEXTURE;
        }
        else if (Obj is UnityEngine.Shader)
        {
            return BaseResource.ResourceType.RESOURCE_SHADER;
        }
        else if (Obj is UnityEngine.PhysicMaterial)
        {
            return BaseResource.ResourceType.RESOURCE_PHYSIC_MATERIAL;
        }
        else if (Obj is UnityEngine.AudioClip)
        {
            return BaseResource.ResourceType.RESOURCE_AUDIO;
        }
        else if (Obj is UnityEngine.TextAsset)
        {
            return BaseResource.ResourceType.RESOURCE_TEXTASSET;
        }
        else if (Obj is UnityEngine.AnimationClip)
        {
            return BaseResource.ResourceType.RESOURCE_ANIMATION_CLIP;
        }
        else if (Obj is UnityEngine.Font)
        {
            return BaseResource.ResourceType.RESOURCE_FONT;
        }
        else if (Obj is UnityEditor.Animations.AnimatorController)
        {
            return BaseResource.ResourceType.RESOURCE_ANIMATOR_CONTROLLER;
        }
        else if (Obj is UnityEngine.Material)
        {
            return BaseResource.ResourceType.RESOURCE_MATERIAL;
        }

        else if (Obj is UnityEngine.GUISkin)
        {
            return BaseResource.ResourceType.RESOURCE_GUI_SKIN;
        }
        else if (Obj is UnityEngine.Mesh)
        {
            return BaseResource.ResourceType.RESOURCE_MODEL;
        }
        else if (Obj is UnityEngine.GameObject)
        {
            PrefabType tmpPrefabType = PrefabUtility.GetPrefabType(Obj);

            if (tmpPrefabType == PrefabType.Prefab)
            {
                return BaseResource.ResourceType.RESOURCE_PREFAB; ;
            }
            else if (tmpPrefabType == PrefabType.ModelPrefab)
            {
                return BaseResource.ResourceType.RESOURCE_MODEL;
            }
            else
            {
                return BaseResource.ResourceType.RESOURCE_GAMEOBJ;
            }
        }
        else
        {
            return BaseResource.ResourceType.RESOURCE_OTHER; ;
        }
    }
}
public class SetVersionWindow : EditorWindow
{
    float version = 1.0f;
    string stateStr = "请输入 version 版本号:";

    string assetBundleName = "";
    string stateStr2 = "请拖入资源：";

    int ResourceType = -1;
    int ABResType = -1;
    string AssetBundleName = "";
    long Size = -1;
    string Md5 = "";
    List<string> DependAssetBundleName = new List<string>();
    int ABResLoadType = -1;

    Vector2 position;
    public UnityEngine.Object obj;
    public void OnGUI()
    {
        EditorGUILayout.LabelField(stateStr);
        version = EditorGUILayout.FloatField("Version", version);

        if (GUILayout.Button("设置版本号(支持小数)"))
        {
            ABResMapEditor.SetVersion(version);
            ABResMapEditor.SaveAbResourceMap();
            stateStr = "设置完毕！版本号为 " + version;
        }
        //if (GUILayout.Button("打包所有资源 重新打包Atlas并且替换UI"))
        //{
        //    OutAtlas.ExportAtlas();
        //    AssetDatabase.Refresh();
        //    ReplacePrefabSrc.ReplaceAllRefabToAtlas();
        //    AssetDatabase.Refresh();
        //    ABResBuilder.BuildAllAssetBundle();
        //}
        if (GUILayout.Button("打包所有资源"))
        {
            ABResBuilder.BuildAllAssetBundle();
        }
        EditorGUILayout.Space();

        //if (GUILayout.Button("打包 -> 内网"))
        //{
        //    BuildPackage.BulidTarget("SkipGuidepGuide1;USEAB", "neiwang_");
        //}
        //if(GUILayout.Button("打包 -> 外网"))
        //{
        //    BuildPackage.BulidTarget("SkipGuidepGuide1;NET_OUTER;USEAB", "waiwang_");
        //}
        //if(GUILayout.Button("打包 -> 外网QQ"))
        //{
        //    BuildPackage.BulidTarget("SkipGuidepGuide1;NET_OUTER;QQLogin;USEAB;NO_FPS", "waiwang_qq_test_");
        //}
        //if (GUILayout.Button("打包 -> 外网QQ正式版"))
        //{
        //    BuildPackage.BulidTarget("SkipGuidepGuide1;NET_OUTER;QQLogin;USEAB;NO_FPS;ZHENG_SHI_NET", "waiwang_qq_official_");
        //}
        //if (GUILayout.Button("打包 -> 外网审计版"))
        //{
        //    BuildPackage.BulidTarget("SkipGuidepGuide1;NET_OUTER;SHEN_JI_NET;USEAB;NO_FPS", "waiwang_shenji_");
        //}

        EditorGUILayout.Space();

        if (GUILayout.Button("设置宏 -> CDN资源模式"))
        {
            //BuildPackage.SetScenesByType(1);
            BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "USEAB;USECDN");
        }
        if (GUILayout.Button("设置宏 -> 本地资源模式"))
        {
            //BuildPackage.SetScenesByType(1);
            BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "USEAB");
        }
        if (GUILayout.Button("设置宏 -> 普通模式"))
        {
            //BuildPackage.SetScenesByType(0);
            BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "asdaa");
        }
        EditorGUILayout.Space();
        if(GUILayout.Button("清理资源"))
        {
            ABResBuilder.ClearNoUseResource();
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(stateStr2);
        obj = EditorGUILayout.ObjectField(obj, typeof(UnityEngine.Object));

        if (obj == null) { stateStr2 = "请拖入资源文件！"; return; }
        string assetPath = AssetDatabase.GetAssetPath(obj.GetInstanceID());
        assetBundleName = assetPath.Substring("Assets/".Length);
        assetBundleName = assetBundleName.Substring(0, assetBundleName.LastIndexOf("."));

        ABResMapItemScriptObj result = ABResMapEditor.GetAbResMapItem(assetBundleName);
        if (result == null)
        {
            stateStr2 = "没有找到 " + assetBundleName + " 的资源";

            ABResLoadType = -1;
            ABResType = -1;
            AssetBundleName = "";
            Md5 = "";
            ResourceType = -1;
            Size = -1;
            DependAssetBundleName.Clear();

            return;
        }

        AssetBundleName = result.AssetBundleName;
        ABResLoadType = result.ABResLoadType;
        ABResType = result.ABResType;
        Md5 = result.Md5;
        ResourceType = result.ResourceType;
        Size = result.Size;
        DependAssetBundleName = result.DependAssetBundleName;
        result = null;
        DependAssetBundleName.Sort();

        EditorGUILayout.TextField("AssetBundleName == " + AssetBundleName);
        EditorGUILayout.LabelField("ABResLoadType == " + ABResLoadType);
        EditorGUILayout.LabelField("ABResType == " + ABResType);
        EditorGUILayout.LabelField("Md5 == " + Md5);
        EditorGUILayout.LabelField("ResourceType == " + ResourceType);
        EditorGUILayout.LabelField("Size == " + Size);
        EditorGUILayout.LabelField("DependAssetBundleName 数量为 " + DependAssetBundleName.Count);

        EditorGUILayout.BeginVertical();
        position = EditorGUILayout.BeginScrollView(position);
        for (int i = 0; i < DependAssetBundleName.Count; i++)
        {
            EditorGUILayout.LabelField(DependAssetBundleName[i]);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

    }
}
