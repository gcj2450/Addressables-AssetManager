using System;
using System.Collections.Generic;
using UnityEngine;
using Asgard.Resource;
namespace Asgard
{
    public class ABResExplorer
    {
        public BaseResource[] allResourceList = null;
        public Dictionary<string, BaseResource> mResourcesMap = null;
        private Dictionary<AtlasResourceEnum.AtlasType, AtlasResource> mAtlasResMap = null;
        private ABResLoaderManager abResLoader = null;

        private static ABResExplorer instance = null;

        public static ABResExplorer Instance 
        {
            get 
            {
                if (instance == null) 
                {
                    instance = new ABResExplorer();
                }

                return instance;

            }
        }
        public void InitSys()
        {
            mAtlasResMap = new Dictionary<AtlasResourceEnum.AtlasType, AtlasResource>();
            mResourcesMap = new Dictionary<string, BaseResource>();
            abResLoader = ABResLoaderManager.Instance;
        }

        public void InitData()
        {
            //            mAtlasResMap = new Dictionary<AtlasResourceEnum.AtlasType, AtlasResource>();
            //            mResourcesMap = new Dictionary<string, BaseResource>();
            //            abResLoader = AsgardGame.AbResLoader;
        }

        public void DisposeData()
        {
            mAtlasResMap.Clear();
            mAtlasResMap = null;

            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                pair.Value.Dispose();
            }

            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                pair.Value.ClearDependList();
            }
            mResourcesMap.Clear();
            mResourcesMap = null;

            if (this.allResourceList != null)
            {
                for (int i = 0; i < this.allResourceList.Length; i++)
                {
                    this.allResourceList[i] = null;
                }
                this.allResourceList = null;
            }

            Debug.Log("所有对象置空完毕");

            System.GC.Collect();
        }

        public void DoFrameUpdate(int time, int delta)
        {

        }

        public void DoFixedUpdate()
        {
        }

        /// <summary>
        /// 创建所有资源 游戏开始时调用
        /// </summary>
        internal void CreateALlResources(BaseResource[] allResourceList)
        {
            List<BaseResource> mainResourceList = new List<BaseResource>();
            for (int i = 0; i < allResourceList.Length; i++)
            {
                if ((allResourceList[i].abResMapItem.ABResType & ABResMapScriptObj.ABResTypeMain) == ABResMapScriptObj.ABResTypeMain)
                {
                    mainResourceList.Add(allResourceList[i]);
                }
            }

            for (int j = 0; j < mainResourceList.Count; j++)
            {
                BaseResource resource = mainResourceList[j];
                resource.dependResourceList = FindDependResource(resource, allResourceList);
                mResourcesMap.Add(resource.abResMapItem.AssetBundleName, resource);
                if (resource is AtlasResource)
                {
                    AtlasResource res = resource as AtlasResource;
                    if (mAtlasResMap.ContainsKey(res.atlasType)) { Debug.Log("已经包含这个key " + res.atlasType + " " + res.abResMapItem.AssetBundleName); continue; }
                    mAtlasResMap.Add(res.atlasType, res);
                }
            }

            this.allResourceList = allResourceList;
            mainResourceList.Clear();
            mainResourceList = null;
            System.GC.Collect();
        }

        private List<BaseResource> FindDependResource(BaseResource mainResource, BaseResource[] allResourceList)
        {
            List<BaseResource> result = new List<BaseResource>();
            for (int i = 0; i < allResourceList.Length; i++)
            {
                if (mainResource.abResMapItem.DependAssetBundleName.Contains(allResourceList[i].abResMapItem.AssetBundleName))
                {
                    result.Add(allResourceList[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// 异步 通过类型加载资源AB (比如只加载UI类型资源AB)
        /// </summary>
        /// <param name="AbLoadType"></param>

        public void GetResourcesByType(int[] loadTypes, Action<List<BaseResource>> allFinishAction, Action<BaseResource> itemFinishAction = null)
        {
            List<BaseResource> list = new List<BaseResource>();
            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                for (int i = 0; i < loadTypes.Length; i++)
                {
                    if ((pair.Value.abResMapItem.ABResLoadType & loadTypes[i]) == loadTypes[i])
                    {
                        list.Add(pair.Value);
                    }
                }

            }
            abResLoader.LoadResources(list, allFinishAction, itemFinishAction);
        }

        /// <summary>
        /// 异步 通过类型 下载资源 
        /// </summary>
        /// <param name="loadTypes"></param>
        /// <param name="allFinishAction"></param>
        /// <param name="itemFinishAction"></param>
        public int DownLoadResourceByType(int[] loadTypes, Action<List<BaseResource>> allFinishAction, Action<BaseResource> itemFinishAction = null, List<string> assetBundleNames = null)
        {
            List<BaseResource> list = new List<BaseResource>();
            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                for (int i = 0; i < loadTypes.Length; i++)
                {
                    if ((pair.Value.abResMapItem.ABResLoadType & loadTypes[i]) == loadTypes[i])
                    {
                        if (pair.Value.ifNeedUpdateFromCdn)
                            list.Add(pair.Value);
                    }
                }
            }

            if (assetBundleNames != null && assetBundleNames.Count > 0)
            {
                for (int i = 0; i < assetBundleNames.Count; i++)
                {
                    string abName = assetBundleNames[i];
                    if (mResourcesMap.ContainsKey(abName))
                    {
                        BaseResource res = mResourcesMap[abName];
                        if (res.ifNeedUpdateFromCdn && (!list.Contains(res)))
                            list.Add(res);
                    }
                }
            }


            abResLoader.LoadResources(list, allFinishAction, itemFinishAction, true);
            return list.Count;
        }

        /// <summary>
        /// 异步 通过名称 下载资源
        /// </summary>
        /// <param name="loadTypes"></param>
        /// <param name="allFinishAction"></param>
        /// <param name="itemFinishAction"></param>
        public int DownLoadResourceByName(List<string> assetBundleNames, Action<List<BaseResource>> allFinishAction, Action<BaseResource> itemFinishAction = null)
        {
            List<BaseResource> list = new List<BaseResource>();
            for (int i = 0; i < assetBundleNames.Count; i++)
            {
                string abName = assetBundleNames[i];
                if (mResourcesMap.ContainsKey(abName))
                {
                    BaseResource res = mResourcesMap[abName];
                    if (res.ifNeedUpdateFromCdn)
                        list.Add(res);
                }
            }
            abResLoader.LoadResources(list, allFinishAction, itemFinishAction, true);
            return list.Count;
        }

        public void UnloadResourceByType(int loadType)
        {
            Debug.Log("卸载资源 loadType == " + loadType);
            List<BaseResource> list = new List<BaseResource>();
            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                if ((pair.Value.abResMapItem.ABResLoadType & loadType) == loadType)
                {
                    pair.Value.UnLoad();
                }
            }
            Debug.Log("卸载资源完毕 loadType == " + loadType);
        }

        public void UnloadResourcesByType(int[] loadTypes)
        {
            Debug.LogError("卸载资源");
            List<BaseResource> list = new List<BaseResource>();
            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                for (int i = 0; i < loadTypes.Length; i++)
                {
                    if ((pair.Value.abResMapItem.ABResLoadType & loadTypes[i]) == loadTypes[i])
                    {
                        pair.Value.UnLoad();
                    }
                }
            }
            Debug.LogError("卸载资源完毕");
        }

        public void DisposeResourcesByType(int[] loadTypes)
        {
            List<BaseResource> list = new List<BaseResource>();
            foreach (KeyValuePair<string, BaseResource> pair in mResourcesMap)
            {
                for (int i = 0; i < loadTypes.Length; i++)
                {
                    if ((pair.Value.abResMapItem.ABResLoadType & loadTypes[i]) == loadTypes[i])
                    {
                        pair.Value.Dispose();
                    }
                }

            }

        }

        public void UnloadResourcesByName(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (mResourcesMap.ContainsKey(names[i]))
                {
                    mResourcesMap[names[i]].UnLoad();
                }
            }
        }

        public void DisposeResourcesByName(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (mResourcesMap.ContainsKey(names[i]))
                {
                    mResourcesMap[names[i]].Dispose();
                }
            }
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        public BaseResource GetResourceT(string assetBundleName)
        {
#if UNITY_EDITOR && !USEAB
            assetBundleName = TrimName(assetBundleName);
            if (!mResourcesMap.ContainsKey(assetBundleName))
            {
                ABResMapItemScriptObj obj = new ABResMapItemScriptObj();
                obj.AssetBundleName = assetBundleName;
                obj.ABResType = ABResMapScriptObj.ABResTypeMain;
                BaseResource resource = ABResChecker.Instance.CreateResource(obj, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                resource.Load();
                mResourcesMap.Add(assetBundleName, resource);
            }
            return mResourcesMap[assetBundleName];
#else
            assetBundleName = TrimName(assetBundleName);
            if (!mResourcesMap.ContainsKey(assetBundleName))
            {
                Debug.LogError("资源系统里面没有这个资源 " + assetBundleName); return null;
            }

            BaseResource resource = mResourcesMap[assetBundleName];
            resource.Load();
            return resource;
#endif

        }

        /// <summary>
        /// 同步加载多个资源
        /// </summary>
        /// <param name="assetBundleNames"></param>
        /// <param name="allFinishAction"></param>
        public List<BaseResource> GetResourcesT(List<string> assetBundleNames)
        {
#if UNITY_EDITOR && !USEAB
            for (int j = 0; j < assetBundleNames.Count; j++)
            {
                assetBundleNames[j] = TrimName(assetBundleNames[j]);
            }
            List<BaseResource> list = new List<BaseResource>();
            for (int i = 0; i < assetBundleNames.Count; i++)
            {
                if (!mResourcesMap.ContainsKey(assetBundleNames[i]))
                {
                    ABResMapItemScriptObj obj = new ABResMapItemScriptObj();
                    obj.AssetBundleName = assetBundleNames[i];
                    obj.ABResType = ABResMapScriptObj.ABResTypeMain;
                    BaseResource resource = ABResChecker.Instance.CreateResource(obj, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                    resource.Load();
                    mResourcesMap.Add(assetBundleNames[i], resource);
                }
                list.Add(mResourcesMap[assetBundleNames[i]]);
            }
            return list;
#else
            for (int j = 0; j < assetBundleNames.Count; j++)
            {
                assetBundleNames[j] = TrimName(assetBundleNames[j]);
            }
            List<BaseResource> list = new List<BaseResource>();
            for (int i = 0; i < assetBundleNames.Count; i++)
            {
                if (!mResourcesMap.ContainsKey(assetBundleNames[i]))
                {
                    Debug.LogError("没有这个资源" + assetBundleNames[i]); return null;
                }
                list.Add(mResourcesMap[assetBundleNames[i]]);
            }
            for (int k = 0; k < list.Count; k++)
            {
                list[k].Load();
            }
            return list;
#endif
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetBundleName"></param>
        /// <param name="allFinishAction"></param>
        public void GetResourceY(string assetBundleName, System.Action<BaseResource> allFinishAction, System.Action<BaseResource> itemFinishAction = null)
        {
#if UNITY_EDITOR && !USEAB
            assetBundleName = TrimName(assetBundleName);
            if (!mResourcesMap.ContainsKey(assetBundleName))
            {
                ABResMapItemScriptObj obj = new ABResMapItemScriptObj();
                obj.AssetBundleName = assetBundleName;
                obj.ABResType = ABResMapScriptObj.ABResTypeMain;
                BaseResource resource = ABResChecker.Instance.CreateResource(obj, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                mResourcesMap.Add(assetBundleName, resource);
            }
            abResLoader.LoadResource(mResourcesMap[assetBundleName], allFinishAction, itemFinishAction);
#else

            assetBundleName = TrimName(assetBundleName);
            if (!mResourcesMap.ContainsKey(assetBundleName))
            {
                //Debug.LogError("资源系统里面没有这个资源 " + assetBundleName + " 请检查 \n1 是否真的还需要这个资源 2 这个资源没有在 ABResChecker.buildResDic目录下 \n强制执行回调 \n解决办法: 可以手动打一下这个资源 但是一定要确保2");
                Debug.LogError("资源系统里面没有这个资源 " + assetBundleName);
                try
                {
                    if (itemFinishAction != null) itemFinishAction(null);
                    if (allFinishAction != null) allFinishAction(null);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }

                return;
            }

            BaseResource resource = mResourcesMap[assetBundleName];
            abResLoader.LoadResource(resource, allFinishAction, itemFinishAction);
#endif
        }

        /// <summary>
        /// 异步加载多个资源
        /// </summary>
        /// <param name="assetBundleNames"></param>
        /// <param name="allFinishAction"></param>
        public void GetResourcesY(List<string> assetBundleNames, Action<List<BaseResource>> allFinishAction, Action<BaseResource> itemFinishAction = null)
        {
#if UNITY_EDITOR && !USEAB
            for (int j = 0; j < assetBundleNames.Count; j++)
            {
                assetBundleNames[j] = TrimName(assetBundleNames[j]);
            }
            List<BaseResource> list = new List<BaseResource>();
            for (int i = 0; i < assetBundleNames.Count; i++)
            {
                if (!mResourcesMap.ContainsKey(assetBundleNames[i]))
                {
                    ABResMapItemScriptObj obj = new ABResMapItemScriptObj();
                    obj.AssetBundleName = assetBundleNames[i];
                    obj.ABResType = ABResMapScriptObj.ABResTypeMain;
                    BaseResource resource = ABResChecker.Instance.CreateResource(obj, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                    mResourcesMap.Add(assetBundleNames[i], resource);
                }
                list.Add(mResourcesMap[assetBundleNames[i]]);
            }
            abResLoader.LoadResources(list, allFinishAction, itemFinishAction);
#else
            for (int j = 0; j < assetBundleNames.Count; j++)
            {
                assetBundleNames[j] = TrimName(assetBundleNames[j]);
            }

            List<BaseResource> list = new List<BaseResource>();
            for (int i = 0; i < assetBundleNames.Count; i++)
            {
                if (!mResourcesMap.ContainsKey(assetBundleNames[i]))
                {
                    Debug.LogError("资源系统里面没有这个资源" + assetBundleNames[i]);
                    try
                    {
                        if (itemFinishAction != null) itemFinishAction(null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }

                }
                else
                {
                    list.Add(mResourcesMap[assetBundleNames[i]]);
                }
            }
            abResLoader.LoadResources(list, allFinishAction, itemFinishAction);
#endif

        }


        /// <summary>
        /// 同步加载Atlas资源
        /// </summary>
        /// <param name="atlasType"></param>
        /// <returns></returns>
        public BaseResource GetAtlasResource(AtlasResourceEnum.AtlasType atlasType)
        {
            if (!mAtlasResMap.ContainsKey(atlasType))
            {
#if UNITY_EDITOR && !USEAB
                ABResMapItemScriptObj obj = new ABResMapItemScriptObj();
                string assetBundleName = "UI/GUiResource/Atlas/" + Enum.GetName(typeof(AtlasResourceEnum.AtlasType), atlasType) + ".png";
                assetBundleName = TrimName(assetBundleName);
                obj.AssetBundleName = assetBundleName;
                AtlasResource resource = new AtlasResource(obj, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                resource.Load();
                mResourcesMap.Add(assetBundleName, resource);
                mAtlasResMap.Add(atlasType, resource);
#else

                Debug.LogError("不包含 " + Enum.GetName(typeof(AtlasResourceEnum.AtlasType), atlasType) + " 这个类型的Atlas资源");
                return null;
#endif
            }
            return GetResourceT(mAtlasResMap[atlasType].abResMapItem.AssetBundleName);
        }

        /// <summary>
        /// 异步加载Atlas资源
        /// </summary>
        /// <param name="atlasType"></param>
        /// <param name="spriteName"></param>
        /// <param name="allFinishAction"></param>
        public void GetAtlasResource(AtlasResourceEnum.AtlasType atlasType, Action<BaseResource> allFinishAction, Action<BaseResource> itemFinishAction = null)
        {
            if (!mAtlasResMap.ContainsKey(atlasType))
            {
#if UNITY_EDITOR && !USEAB
                ABResMapItemScriptObj obj = new ABResMapItemScriptObj();
                string assetBundleName = "UI/GUiResource/Atlas/" + Enum.GetName(typeof(AtlasResourceEnum.AtlasType), atlasType) + ".png";
                assetBundleName = TrimName(assetBundleName);
                obj.AssetBundleName = assetBundleName;
                AtlasResource resource = new AtlasResource(obj, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                mResourcesMap.Add(assetBundleName, resource);
                mAtlasResMap.Add(atlasType, resource);
#else
                Debug.LogError("不包含 " + Enum.GetName(typeof(AtlasResourceEnum.AtlasType), atlasType) + " 这个类型的Atlas资源");
                return;
#endif
            }
            GetResourceY(mAtlasResMap[atlasType].abResMapItem.AssetBundleName, allFinishAction, itemFinishAction);
        }

        private string TrimName(string name)
        {
#if UNITY_EDITOR && !USEAB
            if (!name.StartsWith("Assets/"))
                name = "Assets/" + name;
            string ext = System.IO.Path.GetExtension(name);
            if (string.IsNullOrEmpty(ext))
            {
                if (!name.Contains("/")) return name;
                string fileName = name.Substring(name.LastIndexOf("/") + 1);
                string fileDic = name.Substring(0, name.LastIndexOf("/"));
                System.IO.DirectoryInfo dicInfo = new System.IO.DirectoryInfo(fileDic);
                if (dicInfo == null) return name;
                System.IO.FileInfo[] fileInfos = dicInfo.GetFiles(fileName + ".*", System.IO.SearchOption.TopDirectoryOnly);
                if (fileInfos == null) return name;
                List<System.IO.FileInfo> fileList = new List<System.IO.FileInfo>(fileInfos);
                fileList.RemoveAll(item => { return item.Extension.Equals(".meta"); });
                if (fileList.Count == 0) return name;
                if (fileList.Count > 1)
                {
                    Debug.LogError("出现同名文件 " + name);
                    return name;
                }
                else
                {
                    if (fileList[0] == null) return name;
                    return fileDic + "/" + fileList[0].Name;
                }
            }
#else
            name = name.ToLower();
            int dotIndex = name.LastIndexOf(".");
            if (dotIndex == -1)
            {
                name = name + ".unity3d";
            }
            else
            {
                name = name.Substring(0, name.LastIndexOf(".")) + ".unity3d";
            }
            if (name.StartsWith("assets/"))
            {
                name = name.Substring("assets/".Length);
            }
#endif
            return name;
        }


    }
}
