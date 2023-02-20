using System;
using System.Collections.Generic;
using System.IO;
using Asgard.Resource;
using UnityEngine;
using System.Net;

namespace Asgard
{
    public class ABResChecker
    {
        private static ABResChecker instance;
        public static ABResChecker Instance 
        {
            get 
            {
                if (instance == null) 
                {
                    instance = new ABResChecker();
                }

                return instance; 
            }
        }

        public static Dictionary<string, int> buildResDic = new Dictionary<string, int>()
        {
            {"Assets/BuildRes/Audios",ABResMapScriptObj.ABLoadTypeAudio},
            {"Assets/BuildRes/Prefabs", ABResMapScriptObj.ABLoadTypeTankPerfab},
            {"Assets/BuildRes/ConfigDatas", ABResMapScriptObj.ABLoadTypeConfigDatas},
            {"Assets/BuildRes/Sprites", ABResMapScriptObj.ABLoadTypeGuiSprites},
            {"Assets/BuildRes/Atlas", ABResMapScriptObj.ABLoadTypeGuiAtlas},
            {"Assets/BuildRes/UILoadingPrefabs", ABResMapScriptObj.ABLoadTypeUILoadingPrefab},
            {"Assets/BuildRes/UIPanelPrefabs", ABResMapScriptObj.ABLoadTypeUIPanelPrefab},
            {"Assets/BuildRes/Scenes", ABResMapScriptObj.ABLoadTypeScene},
            {"Assets/BuildRes/ParticlesPrefabs", ABResMapScriptObj.ABLoadTypeEffect},
        };

        private enum CurResourceStoge
        {
            Extend,
            Internal,
            CDN,
        }

        private CurResourceStoge curResStoge;
        public string cdnVersion;
        private static ABResMapScriptObj resMapExtend;
        private static ABResMapScriptObj resMapInternal;
        private static ABResMapScriptObj resMapCdn;

        private ABResExplorer abResExplorer = null;
        private ABResLoaderManager abResLoader = null;

        private Action OnCreataAllResAction = null;
        public void GetLatestVersion(Action OnCreataAllResAction)
        {
            //DispatcherProinfo(ProgressInfo.ProgressType.CHACK_RES);

            this.OnCreataAllResAction = OnCreataAllResAction;
#if !USEAB
            if (this.OnCreataAllResAction != null)
            {
                this.OnCreataAllResAction();
            }
            return;
#else

#if !UNITY_EDITOR || USECDN
            string verPath = ABResPathConfig.UrlPrefixExtend + "version.txt";
            bool ifSuccess = StartHttpDownLoad(ABResPathConfig.UrlVersion, verPath);
            if (ifSuccess)
            {
                StreamReader reader = new System.IO.StreamReader(verPath);
                cdnVersion = reader.ReadLine();
                reader.Close();
                reader.Dispose();
                //if (File.Exists(verPath)) File.Delete(verPath);
            }
            else
            {
                cdnVersion = "-1";
            }
            GetExtendMap();
#else
            //在 Unity里面 并且不用CDN
            ABResMapItemScriptObj abResMapItem = new ABResMapItemScriptObj();
            abResMapItem.AssetBundleName = ABResPathConfig.ABVersionFileAssetBundleName;
            TextResource res = new TextResource(abResMapItem, BaseResource.ResourceState.NeedUpdateFromCDN, BaseResource.Storage.None);
            abResLoader.LoadResource(res, OnGetLastestVersion);
#endif
#endif
        }

        private void OnGetLastestVersion(BaseResource baseResources)
        {
            if (baseResources.resourceState == BaseResource.ResourceState.Error)
            {
                cdnVersion = "-1";
            }
            else
            {
                string text = (baseResources as TextResource).Text;
                cdnVersion = text;
                baseResources.UnLoad(true);
            }
            GetExtendMap();
        }

        private void GetExtendMap()
        {
            string extendResMapFile = ABResPathConfig.UrlPrefixExtend + ABResPathConfig.ABResMapAssetBundleName;
            if (File.Exists(extendResMapFile))
            {
                ABResMapItemScriptObj abResMapItem = new ABResMapItemScriptObj();
                abResMapItem.AssetBundleName = ABResPathConfig.ABResMapAssetBundleName;
                ScriptObjResource res = new ScriptObjResource(abResMapItem, BaseResource.ResourceState.Create, BaseResource.Storage.Extend);
                abResLoader.LoadResource(res, OnGetExtendMap);
            }
            else
            {
                resMapExtend = null;
                GetInternalMap();
            }
        }

        private void OnGetExtendMap(BaseResource baseResource)
        {
            resMapExtend = (baseResource as ScriptObjResource).Asset;
            baseResource.UnLoad(true);
            GetInternalMap();
        }
        private void GetInternalMap()
        {
            string internalResMapFile = ABResPathConfig.UrlPrefixInternal + ABResPathConfig.ABResMapAssetBundleName;
            if (File.Exists(internalResMapFile))
            {
                ABResMapItemScriptObj abResMapItem = new ABResMapItemScriptObj();
                abResMapItem.AssetBundleName = ABResPathConfig.ABResMapAssetBundleName;
                ScriptObjResource res = new ScriptObjResource(abResMapItem, BaseResource.ResourceState.Create, BaseResource.Storage.Internal);
                abResLoader.LoadResource(res, OnGetInternalMap);
            }
            else
            {
                resMapInternal = null;
                CompareLoaclResource();
            }
        }

        private void OnGetInternalMap(BaseResource baseResource)
        {
            resMapInternal = (baseResource as ScriptObjResource).Asset;
            baseResource.UnLoad(true);
            CompareLoaclResource();
        }

        private void CompareLoaclResource()
        {
            float extendVersion = resMapExtend == null ? -1 : resMapExtend.Version;
            float internalVersion = resMapInternal == null ? -1 : resMapInternal.Version;
            float maxLocalVersion = Math.Max(extendVersion, internalVersion);
            if (float.Parse(cdnVersion) <= maxLocalVersion)
            {
                if (extendVersion > internalVersion)
                {
                    //用extend
                    curResStoge = CurResourceStoge.Extend;
                }
                else
                {
                    //用internal
                    curResStoge = CurResourceStoge.Internal;
                }
                SetCurResMap();
            }
            else if (float.Parse(cdnVersion) > maxLocalVersion)
            {
                GetCdnMap();
            }
        }

        private void GetCdnMap()
        {
            ABResMapItemScriptObj abResMapItem = new ABResMapItemScriptObj();
            abResMapItem.AssetBundleName = ABResPathConfig.ABResMapAssetBundleName;
            ScriptObjResource res = new ScriptObjResource(abResMapItem, BaseResource.ResourceState.NeedUpdateFromCDN, BaseResource.Storage.None);
            abResLoader.LoadResource(res, OnGetCdnMap);
        }

        private void OnGetCdnMap(BaseResource baseResource)
        {
            ScriptObjResource res = baseResource as ScriptObjResource;
            resMapCdn = res.Asset;
            curResStoge = CurResourceStoge.CDN;

            if (resMapExtend == null)
            {
                if (Directory.Exists(ABResPathConfig.UrlPrefixExtend))
                {
                    ABResPathConfig.DelectDir(ABResPathConfig.UrlPrefixExtend);
                }
            }
            else
            {
                for (int i = 0; i < resMapExtend.Resources.Count; i++)
                {
                    ABResMapItemScriptObj extendItem = resMapExtend.Resources[i];
                    ABResMapItemScriptObj findItem = null;
                    for (int j = 0; j < resMapCdn.Resources.Count; j++)
                    {
                        if (resMapCdn.Resources[j].AssetBundleName.Equals(extendItem.AssetBundleName))
                        {
                            findItem = resMapCdn.Resources[j]; break;
                        }
                    }
                    if (findItem == null || (!findItem.Md5.Equals(extendItem.Md5)))
                    {
                        //删除文件
                        string filePath = ABResPathConfig.UrlPrefixExtend + extendItem.AssetBundleName;
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            //Debug.LogError("删除本地md5不同的资源文件" + filePath);
                        }
                    }
                }
            }

            string path = ABResPathConfig.UrlPrefixExtend + ABResPathConfig.ABResMapAssetBundleName;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            string dic = path.Substring(0, path.LastIndexOf("/"));
            if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
            File.WriteAllBytes(path, res.bytes);
            SetCurResMap();
        }

        ABResMapScriptObj curResMap;
        string curPathPrefix = "";
        BaseResource[] allResources;
        private bool ifCheckFilesOver = false;
        private bool ifItemFinshed = false;

        private void SetCurResMap()
        {
            if (curResStoge == CurResourceStoge.Extend)
            {
                curResMap = resMapExtend;
                curPathPrefix = ABResPathConfig.UrlPrefixExtend;
            }
            else if (curResStoge == CurResourceStoge.Internal)
            {
                curResMap = resMapInternal;
                curPathPrefix = ABResPathConfig.UrlPrefixInternal;
            }
            else
            {
                curResMap = resMapCdn;
                curPathPrefix = ABResPathConfig.UrlPrefixExtend;
            }

            new System.Threading.Thread(CheckFiles).Start();
        }

        private void CheckFiles()
        {
            allCount = curResMap.Resources.Count;
            curCount = 0;

            ifCheckFilesOver = false;
            for (int i = 0; i < curResMap.Resources.Count; i++)
            {
                ABResMapItemScriptObj resMapItem = curResMap.Resources[i];
                string abFilePath = curPathPrefix + resMapItem.AssetBundleName;

                if (File.Exists(abFilePath))
                {
                    string curMd5 = GetMd5Hash(abFilePath).ToLower();
                    if ((!curMd5.Equals(resMapItem.Md5)) && (!resMapItem.AssetBundleName.Equals(ABResPathConfig.ABResMapAssetBundleName)))
                    {
                        if (File.Exists(abFilePath)) File.Delete(abFilePath);
                    }
                }
                curCount = i + 1;
                ifItemFinshed = true;
            }
            ifCheckFilesOver = true;
        }

        private void CreateAllResource()
        {
            allResources = new BaseResource[curResMap.Resources.Count];
            for (int i = 0; i < curResMap.Resources.Count; i++)
            {
                ABResMapItemScriptObj resMapItem = curResMap.Resources[i];
                string abFilePath = curPathPrefix + resMapItem.AssetBundleName;
                bool ifNeedUpdateFromCdn = !File.Exists(abFilePath);

                BaseResource.ResourceState resourceState = ifNeedUpdateFromCdn ? BaseResource.ResourceState.NeedUpdateFromCDN : BaseResource.ResourceState.Create;
                BaseResource.Storage storage = curResStoge == CurResourceStoge.Internal ? BaseResource.Storage.Internal : BaseResource.Storage.Extend;
                BaseResource resource = CreateResource(resMapItem, resourceState, storage);
                allResources[i] = resource;
            }


            abResExplorer.CreateALlResources(allResources);
            allResources = null;
            Debug.Log("开始加载配置表资源");

            allCount = ABResExplorer.Instance.DownLoadResourceByType(new int[]{
                            ABResMapScriptObj.ABLoadTypeConfigDatas,
            },
                            OnAllDownLoadFinishAction, OnItemDownLoadFinishAction);

            System.GC.Collect();
            curCount = 0;
            //DispatcherProinfo(ProgressInfo.ProgressType.UPDATA);
        }

        public int allCount;
        public int curCount = 0;
        //public void DispatcherProinfo(ProgressInfo.ProgressType type)
        //{
        //    ProgressInfo info = new ProgressInfo();
        //    info.type = type;
        //    switch (info.type)
        //    {
        //        case ProgressInfo.ProgressType.CHACK_RES:
        //        case ProgressInfo.ProgressType.FINISH:
        //            break;
        //        case ProgressInfo.ProgressType.UPDATA:
        //        case ProgressInfo.ProgressType.CHECK_RES_LOCAL:
        //            info.loadTotalCount = allCount;
        //            info.loadIndex = curCount;
        //            break;
        //    }
         
        //    AsgardGame.DataDispatcher.BroadcastData(GameLogicConst.DATA_RESOURCE_UPDATING_PROGRESS, 0, info);
        //}

        private void OnAllDownLoadFinishAction(List<BaseResource> list)
        {
            Debug.Log("加载配置表资源完毕");
            if (this.OnCreataAllResAction != null) this.OnCreataAllResAction();
        }

        private void OnItemDownLoadFinishAction(BaseResource resource)
        {
            //DispatcherProinfo(++curCount == allCount ? ProgressInfo.ProgressType.FINISH : ProgressInfo.ProgressType.UPDATA);
        }

        public BaseResource CreateResource(ABResMapItemScriptObj abResMapItem, BaseResource.ResourceState resourceState, BaseResource.Storage storage)
        {
            bool main = (abResMapItem.ABResType & ABResMapScriptObj.ABResTypeMain) == ABResMapScriptObj.ABResTypeMain;
            if (main)
            {
                int type = GetABResLoadType(abResMapItem.AssetBundleName);
                if ((type & ABResMapScriptObj.ABLoadTypeAudio) == ABResMapScriptObj.ABLoadTypeAudio)
                {
                    return new AudioResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeConfigDatas) == ABResMapScriptObj.ABLoadTypeConfigDatas)
                {
                    return new BinaryResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeGuiAtlas) == ABResMapScriptObj.ABLoadTypeGuiAtlas)
                {
                    return new AtlasResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeGuiSprites) == ABResMapScriptObj.ABLoadTypeGuiSprites)
                {
                    return new TextureResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeScene) == ABResMapScriptObj.ABLoadTypeScene)
                {
                    return new SceneResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeTankPerfab) == ABResMapScriptObj.ABLoadTypeTankPerfab)
                {
                    return new ModelResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeUILoadingPrefab) == ABResMapScriptObj.ABLoadTypeUILoadingPrefab)
                {
                    return new UIResource(abResMapItem, resourceState, storage);
                }
                else if ((type & ABResMapScriptObj.ABLoadTypeUIPanelPrefab) == ABResMapScriptObj.ABLoadTypeUIPanelPrefab)
                {
                    return new UIResource(abResMapItem, resourceState, storage);
                }
            }
            return new BaseResource(abResMapItem, resourceState, storage);
        }

        private static int GetABResLoadType(string assetBundleName)
        {
            string name = "";
#if UNITY_EDITOR && !USEAB
            if (!assetBundleName.StartsWith("Assets/"))
            {
                assetBundleName = "Assets/" + assetBundleName;
            }
            name = assetBundleName.ToLower();
#else
            name = ("Assets/" + assetBundleName).ToLower();
#endif

            foreach (KeyValuePair<string, int> pair in Asgard.ABResChecker.buildResDic)
            {
                if (name.StartsWith(pair.Key.ToLower()))
                {
                    return pair.Value;
                }
            }
            return ABResMapScriptObj.ABLoadTypeNone;
        }

        public void InitData()
        {
            //            abResExplorer = AsgardGame.AbResExplorer;
            //            abResLoader = AsgardGame.AbResLoader;
        }

        public void InitSys()
        {
            abResExplorer = ABResExplorer.Instance;
            abResLoader = ABResLoaderManager.Instance;
        }

        public void DisposeData()
        {
            abResExplorer = null;
            abResLoader = null;
        }

        public void DoFrameUpdate(int time, int delta)
        {
            if (ifCheckFilesOver)
            {
                ifCheckFilesOver = false;
                ifItemFinshed = false;
                CreateAllResource();
            }

            if(ifItemFinshed)
            {
                //DispatcherProinfo(ProgressInfo.ProgressType.CHECK_RES_LOCAL);
            }
        }

        public void DoFixedUpdate()
        {
        }

        private const int BUFF_SIZE = 0xffff;
        private bool StartHttpDownLoad(string url, string destPath)
        {
            try
            {
                url += string.Format("?t={0}", System.DateTime.Now.Ticks);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                //request.ReadWriteTimeout = 10000;
                //request.Timeout = 6000;
                WebResponse response = request.GetResponse();
                Stream inStream = response.GetResponseStream();
                if (File.Exists(destPath)) { File.Delete(destPath); }
                FileStream outStream = new FileStream(destPath, FileMode.Create);

                byte[] buff = new byte[BUFF_SIZE];
                int len = 0;
                long totals = Math.Max(1, response.ContentLength);
                long pos = 0;
                while ((len = inStream.Read(buff, 0, BUFF_SIZE)) != 0)
                {
                    outStream.Write(buff, 0, len);
                    pos += len;
                }
                inStream.Close();
                outStream.Close();
                response.Close();
                if (pos != totals) { Debug.LogError("下载文件失败"); return false; }
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }

            Debug.Log("下载tank2.unity3d文件成功");
            return true;
        }

        public static string GetMd5Hash(string pathName)
        {
            string StrResult = string.Empty;
            string StrHashData = string.Empty;
            byte[] ArrbytHashValue = null;

            System.IO.FileStream OFileStream = null;
            System.Security.Cryptography.MD5CryptoServiceProvider MD5Hasher = new System.Security.Cryptography.MD5CryptoServiceProvider();
            try
            {
                OFileStream = new System.IO.FileStream(pathName.Replace("\"", ""), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                ArrbytHashValue = MD5Hasher.ComputeHash(OFileStream);
                OFileStream.Close();

                //由以连字符分隔的十六进制对构成的String，其中每一对表示value 中对应的元素；例如“F-2C-4A”
                StrHashData = System.BitConverter.ToString(ArrbytHashValue);
                StrHashData = StrHashData.Replace("-", "");
                StrResult = StrHashData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }
            return StrResult;
        }
    }
}

