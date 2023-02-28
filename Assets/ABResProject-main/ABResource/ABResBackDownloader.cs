using System.Collections.Generic;
using Asgard.Resource;
using UnityEngine;

namespace Asgard
{
    public class DownLoadResShareableData
    {
        public DownLoadResShareableData(float p)
        {
            this.progress = p;
        }
        public float progress = 1.0f;
        public int curCount = 0;
        public int allCount = 1;
    }

    public class ABResBackDownloader
    {
        public static ABResBackDownloader instance = null;

        public static ABResBackDownloader Instance
        {
            get
            {
                if (instance == null) instance = new ABResBackDownloader();
                return instance;
            }
        }

        public bool ifDownLoadInBG = true;
        public Queue<BaseResource> needDownloadResInBg = new Queue<BaseResource>();

        public void AddQueueByType(BaseResource baseResource)
        {
            needDownloadResInBg.Enqueue(baseResource);
        }

        bool ifInit = false;

        private int AllNeedDownLoadResCount = 0;
        //这里应该是根据资源名获取资源
        public void StartDownLoadResByAbNameBg(BaseResource res)
        {
            //InitDownloadNames();
            if (needDownloadResInBg.Count <= 0)
            {

                for (int j = 0; j < res.dependResourceList.Count; j++)
                {
                    if (res.dependResourceList[j].resourceState == BaseResource.ResourceState.NeedUpdateFromCDN)
                    {
                        if (!needDownloadResInBg.Contains(res.dependResourceList[j]))
                            needDownloadResInBg.Enqueue(res.dependResourceList[j]);
                    }
                }

                if (res.resourceState == BaseResource.ResourceState.NeedUpdateFromCDN)
                {
                    if (!needDownloadResInBg.Contains(res))
                        needDownloadResInBg.Enqueue(res);

                }

                AddQueueByType(res);
            }

            if (needDownloadResInBg.Count > 0)
            {
                AllNeedDownLoadResCount = needDownloadResInBg.Count;
                ABResLoaderManager.Instance.LoadResource(needDownloadResInBg.Dequeue(), OoAllFinishAction, null, true);
                Debug.Log("开始后台下载..");
            }
            else
            {
                AllNeedDownLoadResCount = 0;
                Debug.Log("已经没有需要后台下载的资源了");
                //AsgardGame.DataDispatcher.BroadcastData(GameNotifyMessage.NOTIFY_ACTIVIYT_HAS_DOWNLOADRES, 3, new DownLoadResShareableData(1.0f));
            }

        }

        private void OoAllFinishAction(BaseResource res)
        {
            //Debug.Log("后台下载资源完毕 " + res.abResMapItem.AssetBundleName);
            int count = AllNeedDownLoadResCount - needDownloadResInBg.Count;
            float progress = (float)count / (float)AllNeedDownLoadResCount;
            DownLoadResShareableData d = new DownLoadResShareableData(progress);
            d.curCount = count;
            d.allCount = AllNeedDownLoadResCount;
            //AsgardGame.DataDispatcher.BroadcastData(GameNotifyMessage.NOTIFY_ACTIVIYT_HAS_DOWNLOADRES, 1, d);
            if (needDownloadResInBg.Count > 0 && ifDownLoadInBG)
            {
                ABResLoaderManager.Instance.LoadResource(needDownloadResInBg.Dequeue(), OoAllFinishAction, null, true);
            }
            else
            {
                if (needDownloadResInBg.Count <= 0)
                {
                    Debug.Log("已经没有需要后台下载的资源了");
                    //AsgardGame.DataDispatcher.BroadcastData(GameNotifyMessage.NOTIFY_ACTIVIYT_HAS_DOWNLOADRES, 3, new DownLoadResShareableData(1.0f));
                }
                else
                {
                    Debug.Log("ifDownLoadInBG == " + ifDownLoadInBG);
                }

            }

        }

        public void InitData()
        {

        }

        public void InitSys()
        {

        }

        public void DisposeData()
        {

        }

        public void DoFrameUpdate(int time, int delta)
        {

        }

        public void DoFixedUpdate()
        {
        }

    }
}

