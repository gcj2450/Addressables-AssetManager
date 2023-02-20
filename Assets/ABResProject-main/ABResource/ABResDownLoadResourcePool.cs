
using System.Collections.Generic;
using Asgard.Resource;
using System.Net;

namespace Asgard
{
    public class ABResDownLoadResourcePool
    {
        public static int MaxDownloadedResouceCount = 30;
        private Queue<BaseResource> downLoadedQueue = null;
        private static ABResDownLoadResourcePool instance = null;
        public static ABResDownLoadResourcePool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ABResDownLoadResourcePool();
                }
                return instance;
            }
        }

        private List<ABResLoader> downLoadedList = null;
        private List<ABResLoader> freeLoaderList = null;

        public void DisposeData()
        {
            foreach (ABResLoader loader in downLoadedList)
            {
                loader.Dispose();
            }
            downLoadedList.Clear();
            downLoadedQueue.Clear();
            freeLoaderList.Clear();
        }

        public ABResDownLoadResourcePool()
        {
            ServicePointManager.DefaultConnectionLimit = 512;
            this.downLoadedList = new List<ABResLoader>();
            this.downLoadedQueue = new Queue<BaseResource>();
            this.freeLoaderList = new List<ABResLoader>();
        }
        public void AddDownLoadResource(List<BaseResource> addResource)
        {
            for (int i = 0; i < addResource.Count; i++)
            {
                AddDownLoadResource(addResource[i]);
            }
        }

        public void AddDownLoadResource(BaseResource addResource)
        {
            AddResource(addResource);
            for (int i = 0; i < addResource.dependResourceList.Count; i++)
            {
                AddResource(addResource.dependResourceList[i]);
            }
        }

        private void AddResource(BaseResource addResource)
        {
            if (addResource.resourceState != BaseResource.ResourceState.NeedUpdateFromCDN) return;
            for (int i = 0; i < downLoadedList.Count; i++)
            {
                if (downLoadedList[i].resource == addResource)
                {
                    return;
                }
            }

            if (downLoadedQueue.Contains(addResource)) return;
            downLoadedQueue.Enqueue(addResource);

        }

        public void DoFrameUpdate(int time, int delta)
        {
            if (downLoadedQueue.Count > 0)
            {
                if (downLoadedList.Count < MaxDownloadedResouceCount)
                {
                    int tempNum = MaxDownloadedResouceCount - downLoadedList.Count;
                    int count = System.Math.Min(tempNum, downLoadedQueue.Count);
                    for (int i = 0; i < count; i++)
                    {
                        ABResLoader addLoader = GetFreeLoader();
                        addLoader.Init(downLoadedQueue.Dequeue());
                        downLoadedList.Add(addLoader);
                    }
                }
            }

            for (int i = 0; i < downLoadedList.Count; i++)
            {
                ABResLoader loader = downLoadedList[i];
                if (loader.loaderState != ABResLoader.LoaderState.Over)
                {
                    loader.DoFrameUpdate(time, delta);
                }

                if (loader.loaderState == ABResLoader.LoaderState.Over)
                {
                    //UnityEngine.Debug.LogError("下载资源完毕" + loader.resource.abResMapItem.AssetBundleName);
                    loader.Dispose();
                    freeLoaderList.Add(loader);
                }
            }

            if (downLoadedList.Count > 0)
            {
                for (int j = 0; j < freeLoaderList.Count; j++)
                {
                    ABResLoader tmp = freeLoaderList[j];
                    if (downLoadedList.Contains(tmp))
                        downLoadedList.Remove(tmp);
                }
            }

            //UnityEngine.Debug.Log("downLoadedList.count == " + downLoadedList.Count);
        }

        public ABResLoader GetFreeLoader()
        {
            //UnityEngine.Debug.Log("freeLoaderList.Count == " + freeLoaderList.Count);
            if (freeLoaderList.Count <= 0) { return new ABResLoader(); }
            else
            {
                ABResLoader loader = freeLoaderList[0];
                freeLoaderList.Remove(loader);
                return loader;
            }
        }

    }
}
