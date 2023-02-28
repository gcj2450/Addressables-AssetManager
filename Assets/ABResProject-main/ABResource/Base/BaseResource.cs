using System.Collections.Generic;
using UnityEngine;

namespace Asgard.Resource
{
    public class BaseResource
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public enum ResourceType
        {
            RESOURCE_NONE = -1,
            RESOURCE_TEXTURE,   //材质            0
            RESOURCE_MODEL,     //模型            1
            RESOURCE_PREFAB,    //预制物          2
            RESOURCE_SPRITE,    //精灵            3
            RESOURCE_EFFECT,    //粒子            4
            RESOURCE_SCENE,     //场景            5
            RESOURCE_BINARY,    //二进制          6
            RESOURCE_TEXT,      //文本文件，例如配置文件，一般json格式  7
            RESOURCE_AUDIO,     //音频            8
            RESOURCE_LIGHTMAP,  //光照贴图        9
            RESOURCE_SHADER,    //shader          10
            RESOURCE_FONT,      //字体            11
            RESOURCE_GUI_SKIN,  //GUISkin         12
            RESOURCE_TEXTASSET, //TextAsset       13
            RESOURCE_MATERIAL,  //材质            14
            RESOURCE_PHYSIC_MATERIAL,    //       15
            RESOURCE_ANIMATION_CLIP,     //       16
            RESOURCE_ANIMATOR_CONTROLLER,//       17
            RESOURCE_ASSET,              //       18
            RESOURCE_SCRIPT,             //       19
            RESOURCE_GAMEOBJ,            //       20
            RESOURCE_OTHER,              //       21
            RESOURCE_COUNT               //       22
        }

        /// <summary>
        /// 资源状态，未加载，加载或者需要更新等
        /// </summary>
        public enum ResourceState
        {
            /// <summary>
            /// 初始化状态 无AssetBundle
            /// </summary>
            Create,
            /// <summary>
            /// 已经赋值AssetBundele
            /// </summary>
            Loaded,
            /// <summary>
            /// Unload(false)状态
            /// </summary>
            Unloaded,
            /// <summary>
            /// 本地没有这个AB文件 或者 CDN上的文件比本地的新
            /// </summary>
            NeedUpdateFromCDN,
            /// <summary>
            /// 下载错误状态
            /// </summary>
            Error,
        }

        /// <summary>
        /// 存储位置
        /// </summary>
        public enum Storage
        {
            /// <summary>
            /// 存储在外部
            /// </summary>
            Extend,
            /// <summary>
            /// 存储在包内
            /// </summary>
            Internal,
            /// <summary>
            /// 不存储资源
            /// </summary>
            None,
        }

        //public ResourceType resourceType
        //{
        //    get
        //    {
        //        return (ResourceType)(this.abResMapItem.ResourceType);
        //    }
        //}


        //public ABResMapItemScriptObj abResMapItem = null;
        protected AssetBundle assetBundle;
        public byte[] bytes;
        private int referenceCount;
        protected string loadName = "";

        public ResourceState resourceState;
        public Storage storage;

        public string LoadName
        {
            get
            {
                return loadName;
            }
        }

        public List<BaseResource> dependResourceList = new List<BaseResource>();

        //public BaseResource(ABResMapItemScriptObj abResMapItem, ResourceState resourceState, Storage storage)
        //{
        //    this.abResMapItem = abResMapItem;
        //    this.resourceState = resourceState;
        //    this.storage = storage;
        //    this.referenceCount = 0;
        //    string loadName = this.abResMapItem.AssetBundleName.Substring(this.abResMapItem.AssetBundleName.LastIndexOf("/") + 1);
        //    if (loadName.Contains("."))
        //        loadName = loadName.Substring(0, loadName.LastIndexOf("."));
        //    this.loadName = loadName;
        //}

        public void SetAssetBundle(AssetBundle assetBundle)
        {
            this.assetBundle = assetBundle;
        }

        //public void LoadAssetBundle()
        //{
        //    for (int i = 0; i < this.dependResourceList.Count; i++)
        //    {
        //        this.dependResourceList[i].LoadAssetBundle();
        //    }
        //    if (this.resourceState == ResourceState.Loaded) return;
        //    if (this.resourceState == ResourceState.Create || this.resourceState == ResourceState.Unloaded)
        //    {
        //        string url = "";
        //        if (this.storage == BaseResource.Storage.Extend)
        //        {
        //            url = ABResPathConfig.UrlPrefixExtend + this.abResMapItem.AssetBundleName;
        //        }
        //        else if (this.storage == BaseResource.Storage.Internal)
        //        {
        //            url = ABResPathConfig.UrlPrefixInternal + this.abResMapItem.AssetBundleName;
        //        }
        //        this.assetBundle = AssetBundle.LoadFromFile(url);
        //        this.resourceState = ResourceState.Loaded;
        //        this.OnLoaded(null);
        //    }
        //    else
        //    {
        //        Debug.LogError("同步加载错误 资源状态为 " + Enum.GetName(typeof(BaseResource.ResourceState), this.resourceState));
        //    }

        //}

        public void AddReferenceCount()
        {
            for (int i = 0; i < this.dependResourceList.Count; i++)
            {
                this.dependResourceList[i].AddReferenceCount();
            }
            ++this.referenceCount;
        }

        public void ReduceReferenceCount()
        {
            for (int i = 0; i < this.dependResourceList.Count; i++)
            {
                this.dependResourceList[i].ReduceReferenceCount();
            }
            --this.referenceCount;
        }

        public void Load()
        {
            for (int i = 0; i < this.dependResourceList.Count; i++)
            {
                this.dependResourceList[i].Load();
            }
#if UNITY_EDITOR && !USEAB
            this.resourceState = ResourceState.Loaded;
            OnLoaded(null);
#else
            if (this.resourceState == ResourceState.Create || this.resourceState == ResourceState.Unloaded)
            {
                string url = "";
                //从本地load
                if (this.storage == BaseResource.Storage.Extend)
                {
                    url = ABResPathConfig.UrlPrefixExtend + this.abResMapItem.AssetBundleName;
                }
                else if (this.storage == BaseResource.Storage.Internal)
                {
                    url = ABResPathConfig.UrlPrefixInternal + this.abResMapItem.AssetBundleName;
                }

                if (this.storage != Storage.None)
                {
                    this.assetBundle = AssetBundle.LoadFromFile(url);
                }
                else
                {
                    this.assetBundle = AssetBundle.LoadFromMemory(this.bytes);
                }
                this.resourceState = ResourceState.Loaded;

                OnLoaded(null);
            }

#endif
        }

        public void UnLoad(bool ifForce = false)
        {
            for (int i = 0; i < this.dependResourceList.Count; i++)
            {
                this.dependResourceList[i].UnLoad(ifForce);
            }
#if UNITY_EDITOR && !USEAB
            this.resourceState = ResourceState.Unloaded;
            OnUnLoaded ();
#else
            if ((this.assetBundle != null && this.resourceState == ResourceState.Loaded && this.referenceCount == 0) || ifForce)
            {
                this.assetBundle.Unload(false);
                this.resourceState = ResourceState.Unloaded;
                OnUnLoaded();
            }
            //else
            //{
            //    Debug.Log("资源UnLoad失败 " + this.loadName);
            //}
#endif

        }


        public void Dispose()
        {
            for (int i = 0; i < this.dependResourceList.Count; i++)
            {
                this.dependResourceList[i].Dispose();
            }
#if UNITY_EDITOR && !USEAB
            this.resourceState = ResourceState.Create;
            OnDispose();
#else
            if (this.assetBundle != null && this.referenceCount == 0)
            {
                this.assetBundle.Unload(true);
                this.resourceState = ResourceState.Create;
                OnDispose();
            }
            else
            {
                //Debug.Log("资源dispose失败 " + this.loadName);
            }
#endif

        }

        public void ClearDependList()
        {
            for (int i = 0; i < this.dependResourceList.Count; i++)
            {
                if (this.dependResourceList[i] != null)
                    this.dependResourceList[i].ClearDependList();
            }

            this.dependResourceList.Clear();
        }

        protected virtual void OnLoaded(System.Object obj)
        {

        }

        protected virtual void OnUnLoaded()
        {

        }

        protected virtual void OnDispose()
        {

        }

        public bool ifLoaded
        {
            get
            {
                if (this.resourceState != ResourceState.Loaded)
                {
                    return false;
                }

                for (int i = this.dependResourceList.Count - 1; i >= 0; i--)
                {
                    if (this.dependResourceList[i].resourceState != ResourceState.Loaded)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool ifCreate
        {
            get
            {
                if (this.resourceState == ResourceState.NeedUpdateFromCDN || this.resourceState == ResourceState.Error)
                {
                    return false;
                }

                for (int i = this.dependResourceList.Count - 1; i >= 0; i--)
                {
                    if (this.dependResourceList[i].resourceState == ResourceState.NeedUpdateFromCDN || this.dependResourceList[i].resourceState == ResourceState.Error)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool ifError
        {
            get
            {
                if (this.resourceState == ResourceState.Error)
                {
                    return true;
                }
                for (int i = this.dependResourceList.Count - 1; i >= 0; i--)
                {
                    if (this.dependResourceList[i].resourceState == ResourceState.Error)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool ifNeedUpdateFromCdn
        {
            get
            {
                if (this.resourceState == ResourceState.NeedUpdateFromCDN)
                {
                    return true;
                }
                for (int i = this.dependResourceList.Count - 1; i >= 0; i--)
                {
                    if (this.dependResourceList[i].resourceState == ResourceState.NeedUpdateFromCDN)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

//        public GameObject InstanceObj
//        {
//            get
//            {
//#if UNITY_EDITOR && !USEAB
//                GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath(this.abResMapItem.AssetBundleName, typeof(UnityEngine.Object)) as GameObject;
//#else
//                GameObject go = this.assetBundle.LoadAsset(loadName) as GameObject;
//#endif
//                return GameObject.Instantiate(go);
//            }

//        }

//        public UnityEngine.Object AssetObj
//        {
//            get
//            {
//                UnityEngine.Object go = null;
//#if UNITY_EDITOR && !USEAB
//                 go = UnityEditor.AssetDatabase.LoadAssetAtPath(this.abResMapItem.AssetBundleName, typeof(UnityEngine.Object));
//#else
//                if (this.assetBundle != null)
//                    go = this.assetBundle.LoadAsset(loadName);
//                else
//                    Debug.LogError("资源[" + loadName + "][" + GetType() + "]没有assetbundle对象");
//#endif
//                return go;
//            }

//        }

    }
}
