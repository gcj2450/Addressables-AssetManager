using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RuntimSwitchUrpSettings : MonoBehaviour
{
    public UniversalRenderPipelineAsset highQualityUniversalRenderPipelineAsset;
    public UniversalRenderPipelineAsset lowQualityUniversalRenderPipelineAsset;

    ScriptableRendererData[] rendererDataList;

    public ScriptableRendererData newRendererData;

    private RenderObjects rendererFeatrue;
    private UniversalRendererData URPData;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(System.DateTime.Now.Minute);

        // 通过GraphicsSettings获取当前的配置
        UniversalRenderPipelineAsset _pipelineAssetCurrent = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        // 通过QualitySettings获取当前的配置
        _pipelineAssetCurrent = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        // 通过QualitySettings获取不同等级的配置
        _pipelineAssetCurrent = QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel()) as UniversalRenderPipelineAsset;  

        //GraphicsSettings.renderPipelineAsset = highQualityUniversalRenderPipelineAsset;

        //UniversalRenderPipelineAsset URPAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        //FieldInfo propertyInfo = URPAsset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
        //UniversalRendererData URPData = (UniversalRendererData)(((ScriptableRendererData[])propertyInfo?.GetValue(URPAsset))?[0]);

        //获取当前渲染管线
        RenderPipelineAsset renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
        Debug.Log(renderPipelineAsset.name);

        var proInfo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
                    BindingFlags.NonPublic | BindingFlags.Instance);
        Debug.Log("proInfo==null: " + (proInfo ==null).ToString());

        if (proInfo != null)
        {
            rendererDataList = (ScriptableRendererData[])proInfo.GetValue(UniversalRenderPipeline.asset);
            Debug.Log("rendererDataList.Length: "+rendererDataList.Length);
            //var newList = new ScriptableRendererData[rendererDataList.Length + 1];
            //for (int i = 0; i < rendererDataList.Length; i++)
            //{

            //    newList[i] = rendererDataList[i];
            //    newList[rendererDataList.Length] = newRendererData;
            //}
            //proInfo.SetValue(GraphicsSettings.currentRenderPipeline, newList);
        }

        //GameObject[] rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
        //Debug.Log("rootObjs: " + rootObjs.Length);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void SetQualitySettings(QualityLevel qualityLevel)
    {
        switch (qualityLevel)
        {
            case QualityLevel.Fastest:
                //forward rendering像素灯数量，建议最少为1
                QualitySettings.pixelLightCount = 2;
                // 0完整分辨率，1/2分辨率，1/4分辨率，1/8分辨率
                QualitySettings.masterTextureLimit = 1;
                //抗锯齿级别：0不开启，2倍，4倍和8倍采样
                QualitySettings.antiAliasing = 0;
                //是否使用粒子融合
                QualitySettings.softParticles = false;
                //启用实时反射探针，此设置需要用的时候再开
                QualitySettings.realtimeReflectionProbes = false;
                //公告牌是否面向相机位置而不是相机方向
                QualitySettings.billboardsFaceCameraPosition = false;
                //软硬阴影是否打开
                QualitySettings.shadows =UnityEngine.ShadowQuality.Disable;
                QualitySettings.vSyncCount = 0;

                break;
            
            case QualityLevel.Good:
                //forward rendering像素灯数量，建议最少为1
                QualitySettings.pixelLightCount = 4;
                // 0完整分辨率，1/2分辨率，1/4分辨率，1/8分辨率
                QualitySettings.masterTextureLimit = 1;
                //抗锯齿级别：0不开启，2倍，4倍和8倍采样
                QualitySettings.antiAliasing =2;
                //是否使用粒子融合
                QualitySettings.softParticles = false;
                //启用实时反射探针，此设置需要用的时候再开
                QualitySettings.realtimeReflectionProbes = true;
                //公告牌是否面向相机位置而不是相机方向
                QualitySettings.billboardsFaceCameraPosition = true;
                //软硬阴影是否打开
                QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
                QualitySettings.vSyncCount = 2;

                break;
           
            case QualityLevel.Fantastic:
                //forward rendering像素灯数量，建议最少为1
                QualitySettings.pixelLightCount = 4;
                // 0完整分辨率，1/2分辨率，1/4分辨率，1/8分辨率
                QualitySettings.masterTextureLimit = 1;
                //抗锯齿级别：0不开启，2倍，4倍和8倍采样
                QualitySettings.antiAliasing = 8;
                //是否使用粒子融合
                QualitySettings.softParticles = true;
                //启用实时反射探针，此设置需要用的时候再开
                QualitySettings.realtimeReflectionProbes = true;
                //公告牌是否面向相机位置而不是相机方向
                QualitySettings.billboardsFaceCameraPosition = true;
                //软硬阴影是否打开
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                QualitySettings.vSyncCount = 2;
                break;
        }
    }

    //参考https://forum.unity.com/threads/how-to-access-scriptablerendererdata-from-script.1012336/
    private void AddRenderFeatrue()
    {
        rendererFeatrue = ScriptableObject.CreateInstance<RenderObjects>();
        rendererFeatrue.name = "Test";
        rendererFeatrue.settings.filterSettings.LayerMask = (1 << LayerMask.NameToLayer("yourlayer0") | 1 << LayerMask.NameToLayer("yourlayer1"));
        rendererFeatrue.settings.filterSettings.RenderQueueType = RenderQueueType.Transparent;
        rendererFeatrue.settings.overrideDepthState = true;
        rendererFeatrue.settings.depthCompareFunction = CompareFunction.Disabled;


        UniversalRenderPipelineAsset URPAsset = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
        FieldInfo propertyInfo = URPAsset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
        URPData = (UniversalRendererData)(((ScriptableRendererData[])propertyInfo?.GetValue(URPAsset))?[0]);
        URPData.rendererFeatures.Add(rendererFeatrue);

#if UNITY_EDITOR
        //在编辑器模式下，需将RenderObjects的本地文件id添加到m_RendererFeatureMap中        
        FieldInfo propertyInfo2 = URPAsset.GetType().GetField("m_RendererFeatureMap", BindingFlags.Instance | BindingFlags.NonPublic);
        List<long> renderFeatureMapList = (List<long>)propertyInfo2?.GetValue(URPAsset);
        UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rendererFeatrue, out var guid, out long localId);
        renderFeatureMapList.Add(localId);

        UnityEditor.AssetDatabase.AddObjectToAsset(rendererFeatrue, URPAsset);
        URPData.SetDirty();
#endif

    }


    //private void OnDestroy()
    //{
    //    URPData.rendererFeatures.Remove(rendererFeatrue);
    //}

    //public static void FragmentBuild()
    //{
    //    if (!Directory.Exists(Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString()))
    //    {
    //        Directory.CreateDirectory(Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString());
    //    }

    //    BuildPipeline.BuildAssetBundles(Application.dataPath + "/../AssetBundle/" + EditorUserBuildSettings.activeBuildTarget.ToString(),
    //        GetAssetBundleBuild(Selection.gameObjects), BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    //    //DeleteMainfest();
    //}

    //public static AssetBundleBuild[] GetAssetBundleBuild(GameObject[] objs)
    //{
    //    List<string> all = GetAllBuildGamobj(objs);

    //    AssetBundleBuild[] builds = new AssetBundleBuild[all.Count];
    //    for (int i = 0; i < all.Count; i++)
    //    {
    //        if (all[i].EndsWith(".mat")) continue;
    //        AssetBundleBuild build = new AssetBundleBuild();
    //        string[] str = all[i].Split('/');
    //        string name = str[str.Length - 1];
    //        string[] str1 = name.Split('.');
    //        string result = str1[0];
    //        build.assetBundleName = result;
    //        build.assetNames = new string[] { all[i] };
    //        build.assetBundleVariant = "ab";
    //        builds[i] = build;
    //    }
    //    return builds;
    //}

    //static List<string> GetAllBuildGamobj(GameObject[] objs)
    //{
    //    List<string> all = new List<string>();
    //    for (int i = 0; i < objs.Length; i++)
    //    {
    //        if (!Check(all, AssetDatabase.GetAssetPath(objs[i])))
    //            all.Add(AssetDatabase.GetAssetPath(objs[i]));
    //        string[] depend = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(objs[i]));
    //        for (int j = 0; j < depend.Length; j++)
    //        {
    //            if (!Check(all, depend[j]))
    //                all.Add(depend[j]);
    //        }
    //    }
    //    return all;
    //}
    //private static bool Check(List<string> list, string target)
    //{
    //    for (int i = 0; i < list.Count; i++)
    //    {
    //        if (list[i] == target)
    //        {
    //            return true;
    //        }
    //    }
    //    return false;
    //}
}
