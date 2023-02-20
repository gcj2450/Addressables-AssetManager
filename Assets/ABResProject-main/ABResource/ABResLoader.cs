using UnityEngine;
using Asgard.Resource;
using System.IO;

namespace Asgard
{
    /// <summary>
    /// 下载完毕之后还需要存储文件
    /// </summary>
    public class ABResLoader
    {
        public enum LoaderState
        {
            Create,
            Downloading,
            Over,
        }

        public LoaderState loaderState = LoaderState.Create;
        public BaseResource resource;
        private ABResWWW www = null;

        public ABResLoader()
        {

        }

        public void Init(BaseResource resource)
        {
#if UNITY_EDITOR && !USEAB
            this.resource = resource;
            this.loaderState = LoaderState.Downloading;
#else
            this.www = null;
            this.resource = resource;
            this.loaderState = LoaderState.Over;
            string url = "";
            if (resource.resourceState == BaseResource.ResourceState.NeedUpdateFromCDN)
            {
                url = ABResPathConfig.UrlPrefixCdn + resource.abResMapItem.AssetBundleName;
                string savePath = "";
                //将AssetBundle写入文件
                if (resource.storage == BaseResource.Storage.Extend)
                {
                    savePath = ABResPathConfig.UrlPrefixExtend + resource.abResMapItem.AssetBundleName;
                }
                else if (resource.storage == BaseResource.Storage.Internal)
                {
                    savePath = ABResPathConfig.UrlPrefixInternal + resource.abResMapItem.AssetBundleName;
                }

                bool ifNeedWriteFile = resource.storage != BaseResource.Storage.None;//CDNMAP
                www = new ABResWWW(url, savePath, ifNeedWriteFile);
                this.loaderState = LoaderState.Downloading;
            }
            else
            {
                Debug.LogError("资源状态为 NeedUpdateFromCDN!!不需要下载!!");
            }
#endif
        }

        public void Dispose()
        {
            if (www != null)
            {
                this.www.Dispose();
            }
            this.resource = null;
            this.www = null;
        }
        public void DoFrameUpdate(int time, int delta)
        {
            if (this.loaderState == LoaderState.Over) return;

#if UNITY_EDITOR && !USEAB
            string assetPath = this.resource.abResMapItem.AssetBundleName;
            if (!File.Exists(assetPath))
            {
                this.resource.resourceState = BaseResource.ResourceState.Error;
                Debug.Log("不存在这个资源文件" + this.resource.abResMapItem.AssetBundleName + " Error状态");
                this.loaderState = LoaderState.Over; 
            }
            else
            {
                this.resource.resourceState = BaseResource.ResourceState.Create;
                this.loaderState = LoaderState.Over; 
            }
#else

            if (www.isDone)
            {
                if (www.error != null)
                {
                    Debug.LogError(www.error);
                    //this.resource.resourceState = BaseResource.ResourceState.Error;
                    //this.loaderState = LoaderState.Over;
                    this.www.Dispose();
                    this.Init(this.resource);
                }
                else
                {
                    this.resource.resourceState = BaseResource.ResourceState.Create;

                    if (resource.storage == BaseResource.Storage.None)
                    {
                        this.resource.bytes = www.bytes;
                    }

                    this.loaderState = LoaderState.Over;

                    this.www.Dispose();
                }
            }
#endif
        }
    }
}
