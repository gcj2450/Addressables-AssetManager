﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Baidu.Meta.ComponentsBundleLoader.Runtime
{
    public class BundleFileDownloaderTest : MonoBehaviour
    {
        private BundleFileDownloader _downloadFile;
        // Start is called before the first frame update
        private void Start()
        {
            SingleThreadDownload();
        }


        void SingleThreadDownload()
        {
            var savePath = System.IO.Path.Combine(Application.dataPath, "../SaveFiles");
            string filePath;
            var url = "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/windows/123/com_baidu_meta_inituipackage_0.2.3/com_baidu_meta_inituipackage_0.2.3_win64/3dmodels-a0d685f0a9be080a21124f627b6a9ddc.assetbundle";

            _downloadFile = new BundleFileDownloader(url, 1);

            string filename = Path.GetFileName(url);
            // 多线程下载文件至本地 支持断点续传
            filePath = System.IO.Path.Combine(savePath, "./" + filename);

            _downloadFile.SingleThreadDownload(url,
                filePath,
                (count) =>
                {
                    UnityEngine.Debug.Log($"多线程下载至本地, 下载进度 >>> {count}");
                },
                (data,msg) =>
                {
                    UnityEngine.Debug.Log($"多线程下载至本地,下载完毕 {data},{msg}");
                }
            );

        }

        void MultiThreadDownload()
        {
            var savePath = System.IO.Path.Combine(Application.dataPath, "../SaveFiles");
            string filePath;
            var url = "https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/windows/123/com_baidu_meta_inituipackage_0.2.3/com_baidu_meta_inituipackage_0.2.3_win64/3dmodels-a0d685f0a9be080a21124f627b6a9ddc.assetbundle";

            _downloadFile = new BundleFileDownloader(url,1);

            _downloadFile.OnError += (ex) =>
            {
                UnityEngine.Debug.Log("捕获异常 >>> " + ex);
            };

            string filename = Path.GetFileName(url);
            // 多线程下载文件至本地 支持断点续传
            filePath = System.IO.Path.Combine(savePath, "./" + filename);
            _downloadFile.DownloadToFile(
                filePath,
                (size, count) =>
                {
                    UnityEngine.Debug.LogFormat("[{0}]下载进度 >>> {1}/{2}", "多线程下载至本地", size, count);
                },
                (data) =>
                {
                    UnityEngine.Debug.LogFormat("[{0}]下载完毕>>>{1}", "多线程下载至本地", data.Length);
                }
            );

        }

        private void OnDestroy()
        {
            _downloadFile.Close();
        }

    }
}