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
                if(instance == null) instance = new ABResBackDownloader();
                return instance; 
            }
        }

        public bool ifDownLoadInBG = true;
        public Queue<BaseResource> needDownloadResInBg = new Queue<BaseResource>();

        public void AddQueueByType(int[] loadTypes)
        {
            foreach (KeyValuePair<string, BaseResource> pair in ABResExplorer.Instance.mResourcesMap)
            {
                for (int i = 0; i < loadTypes.Length; i++)
                {
                    if ((pair.Value.abResMapItem.ABResLoadType & loadTypes[i]) == loadTypes[i])
                    {
                        if (pair.Value.ifNeedUpdateFromCdn)
                            needDownloadResInBg.Enqueue(pair.Value);
                    }
                }

            }
        }

        string[] names = new string[] 
        { 
            "medias/meishu/scene/baiseyandongmap.unity3d", 
            "medias/meishu/scene/fanxigumap.unity3d",
            "medias/meishu/scene/kaxinuoshangumap.unity3d",
            "medias/meishu/scene/lunanhuizhanmap.unity3d",
            "medias/meishu/scene/tieluzhengduozhan2.unity3d",
        };

        int[] loadTypes = new int[] { ABResMapScriptObj.ABLoadTypeTankPerfab };
        bool ifInit = false;
        public void InitDownloadNames()
        {
            //if (ifInit) return;
            //Dictionary<string, MetaDataBase> dic = AsgardGame.MetaData.GetMetaDataMap<MD_BgDown>();
            //if (dic != null && dic.Count > 0)
            //{
            //    int i = 0;
            //    names = new string[dic.Count];
            //    foreach (KeyValuePair<string, MetaDataBase> pair in dic)
            //    {
            //        names[i++] = "medias/meishu/scene/" + ((MD_BgDown)pair.Value).scenename.ToLower() + ".unity3d";
            //    }
            //}

            //ifInit = true;

        }
        private int AllNeedDownLoadResCount = 0;
        public void StartDownLoadResByAbNameBg()
        {
            //InitDownloadNames();
            if (needDownloadResInBg.Count <= 0)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (ABResExplorer.Instance.mResourcesMap.ContainsKey(names[i]))
                    {
                        BaseResource res = ABResExplorer.Instance.mResourcesMap[names[i]];

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

                    }
                }

                AddQueueByType(loadTypes);
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

