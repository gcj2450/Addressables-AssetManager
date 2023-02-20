using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownloadFileTest : MonoBehaviour
{
    private DownloadFile _downloadFile;
    // Start is called before the first frame update

    void Start()
    {
        var savePath = System.IO.Path.Combine(Application.dataPath, "../SaveFiles");
        string filePath;
        // var url = "https://dlc2.pconline.com.cn/filedown_1117483_12749837/gFETfFfp/pconline1552198052014.zip";
        // var url = "https://down.sandai.net/thunder11/XunLeiWebSetup11.2.4.1750dl.exe";
        var url = "http://127.0.0.1/Download/111.exe";

        _downloadFile = new DownloadFile(url);

        _downloadFile.OnError += (ex) =>
        {
            UnityEngine.Debug.Log("�����쳣 >>> " + ex);
        };


        // ���߳������ļ����ڴ� �޷��ϵ�����
        // filePath = System.IO.Path.Combine(savePath, "./���߳��������ڴ�.exe");
        // _downloadFile.DownloadToMemory(
        //     4,
        //     (size, count) =>
        //     {
        //         UnityEngine.Debug.LogFormat("[{0}]���ؽ��� >>> {1}/{2}", "���߳��������ڴ�", size, count);
        //     },
        //     (data) =>
        //     {
        //         UnityEngine.Debug.LogFormat("[{0}]�������>>>{1}", "���߳��������ڴ�", data.Length);

        //         // �������ڴ�󱣴浽�ļ�
        //         if (!System.IO.File.Exists(filePath))
        //         {
        //             System.IO.File.Create(filePath).Dispose();
        //         }
        //         System.IO.File.WriteAllBytes(filePath, data);

        //     }
        // );

        // ���߳������ļ������� ֧�ֶϵ�����
        filePath = System.IO.Path.Combine(savePath, "./���߳�����������.exe");
        _downloadFile.DownloadToFile(
            4,
            filePath,
            (size, count) =>
            {
                UnityEngine.Debug.LogFormat("[{0}]���ؽ��� >>> {1}/{2}", "���߳�����������", size, count);
            },
            (data) =>
            {
                UnityEngine.Debug.LogFormat("[{0}]�������>>>{1}", "���߳�����������", data.Length);
            }
        );

    }

    private void OnDestroy()
    {
        _downloadFile.Close();
    }

}
