using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Asgard
{
    //AB用到的路径类
    public class ABResPathConfig
    {

        public static string UrlVersion = AsgardConst.UPDATE_RES_URL_ROOT + "version.txt";
        public static string ProjectFullPath = System.IO.Path.GetFullPath(Application.dataPath).Replace("\\", "/");
        /// <summary>
        /// AB文件的ResourceMap资源文件目录
        /// </summary>
        public static string ABResourceMapFileAssetPath
        {
            get
            {
                string path = Application.dataPath + "/ABResourceMapAsset";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path.Substring(path.LastIndexOf("Assets/"));
            }
        }

        public static string ABResourceMapFileName = "ABResourceMap";//资源名
        public static string ABResourceMapFileFullName = ABResourceMapFileName + ".asset";

        public static string ABResMapAssetBundleName = "abresourcemapasset/abresourcemap.unity3d";//文件名 assetBundleName

        public static string ABFilesDicName = "abFiles";
        public static string ABVersionFileAssetBundleName = "abresourcemapasset/version.unity3d";
        public static string ABVersionFileName = "version";
        /// <summary>
        /// AB文件存储总目录
        /// </summary>
        private static string ABSavedPath
        {
            get
            {
                string dic = ApplicationRootPath + ABFilesDicName;
                if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
                return dic;
            }
        }

        public static string ApplicationRootPath
        {
            get
            {
                string path = ProjectFullPath;
                path = path.Substring(0, path.LastIndexOf("Assets"));
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static string UrlFileStr = "file:///";
#if UNITY_EDITOR
        public static string UrlPrefixInternal = ABSavedPath + "/";
#if !USECDN
        public static string UrlPrefixCdn
        {
            get
            {
                string dic = ApplicationRootPath + "Cdn/" + ABResPathConfig.ABFilesDicName + "/";
                if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
                return dic;
            }
        }
#else
        public static string UrlPrefixCdn
        {
            get
            {
                string result = AsgardConst.UPDATE_RES_URL_ROOT + ABResPathConfig.ABFilesDicName + "_" + ABResChecker.Instance.cdnVersion + "/";
                return result;
            }
        }
#endif
        public static string UrlPrefixExtend
        {
            get
            {
                string dic = ApplicationRootPath + "Extend/" + ABResPathConfig.ABFilesDicName + "/";
                if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
                return dic;
            }
        }
#else
        public static string UrlPrefixInternal
        {
            get
            {
                string dic = Application.streamingAssetsPath + "/" + ABResPathConfig.ABFilesDicName + "/";
                if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
                return dic;
            }
        }

        public static string UrlPrefixCdn
        {
            get
            {
                string result = AsgardConst.UPDATE_RES_URL_ROOT + ABResPathConfig.ABFilesDicName + "_" + ABResChecker.Instance.cdnVersion + "/";
                return result;
            }
        }
        public static string UrlPrefixExtend
        {
            get
            {
                string dic = Application.persistentDataPath + "/" + ABResPathConfig.ABFilesDicName + "/";
                if (!Directory.Exists(dic)) Directory.CreateDirectory(dic);
                return dic;
            }
        }
#endif


        public static void DelectDir(string srcPath)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录
                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (System.Exception e)
            {
                throw;
            }
        }
    }
}
