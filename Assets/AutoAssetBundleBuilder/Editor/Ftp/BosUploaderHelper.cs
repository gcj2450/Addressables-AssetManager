using BaiduBce.Auth;
using BaiduBce.Services.Bos;
using BaiduBce;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using BaiduBce.Services.Bos.Model;

public class BosUploaderHelper
{
    /// <summary>
    /// �ϴ��ļ��е�Bosƽ̨,platformStrȡֵ��Χ��ios,android,windows
    /// </summary>
    /// <param name="_localFolderName">�����ļ�������</param>
    /// <param name="_ftpFolderName"></param>
    /// <param name="platformStr"></param>
    public void UploadToBos(string _localFolderName, string _ftpFolderName,string platformStr)
    {
        string url = "http://test-bd-b-rd0001.bcc-bddwd.baidu.com:8090/api/v1/client/editor/resource/dev/bos/acquire";
        //platform��ѡ��ios,android,windows
        string json = "{\"unique_path\": \"baidu\",\"engine\": \"unity\",\"platform\": \""+ platformStr + "\"}";

        string data = HttpPost(url, json);
        Debug.Log(data);

        JObject jObj = JObject.Parse(data);
        JToken codestatus = jObj["code"];
        JToken message = jObj["message"];

        if ((int)(codestatus) == 0)
        {
            string access_key = (string)jObj["data"]["access_key"];
            string secret_key = (string)jObj["data"]["secret_key"];
            string session_token = (string)jObj["data"]["session_token"];
            string expiration = (string)jObj["data"]["expiration"];
            string bos_path_prefix = (string)jObj["data"]["bos_path_prefix"];
            string bos_domain_cdn = (string)jObj["data"]["bos_domain_cdn"];

            Debug.Log(access_key + "__" + secret_key + "__" +
                session_token + "__" + expiration + "__" +
                bos_path_prefix + "__" + bos_domain_cdn);
            string endpoint = "https://bj.bcebos.com";

            BosClient client = GenerateBosClient(access_key, secret_key, session_token, endpoint);

            //��ȡbucketName: xirang-client-editor-res-dev
            string bucketName = bos_path_prefix;
            bucketName = bucketName.Substring(0, bucketName.IndexOf("/"));
            Debug.Log("bucketName: " + bucketName);

            //bool result = false;
            //result = client.DoesBucketExist(bucketName);
            //Debug.Log(result);

            //bos_path_prefix : "xirang-client-editor-res-dev/AssertBundle/baidu/unity/ios",

            //ȡ���ַ���:   /AssertBundle/baidu/unity/ios
            //string objectNameFile = bos_path_prefix.Substring(bos_path_prefix.IndexOf("/"));
            //Debug.Log("objectNameFile: " + objectNameFile);

            //bos_path_prefix: xirang-client-editor-res-dev/AssertBundle/baidu/unity/ios
            Debug.Log("bos_path_prefix: " + bos_path_prefix);

            Thread thread = new Thread(() =>
            {
                //DownloadObject(client, bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt", "D:/GitHub/Addressables-AssetManager/BaseBundlesBuild/0.1.19/config_win64.txt");
                //string url = GeneratePresignedUrl(client, bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt");
                //GetObject(client, bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt");

                //ƴ�Ӻ�ĵ�ַ: https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/AssertBundle/baidu/unity/ios/config_win64.txt
                //PutObjectResponse putObjectFromFileResponse =
                //client.PutObject(bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt", new FileInfo(fileName));
                //Debug.Log(putObjectFromFileResponse.ETAG);


                List<string> files = new List<string>();
                RecursiveFile(_localFolderName, ref files);
                Debug.Log(files.Count);
                for (int i = 0, cnt = files.Count; i < cnt; i++)
                {
                    FileInfo fileInfo = new FileInfo(files[i]);
                    string dirName = fileInfo.DirectoryName;
                    //Debug.Log(dirName);
                    dirName = dirName.Replace("\\", "/") + "/";
                    //Debug.Log(_localFolderName);
                    //�ļ�Ŀ¼ȥ��ǰ���AssetsĿ¼һ�µ��ַ���
                    string remoteFolderName = dirName.Replace(_localFolderName, "");

                    Debug.Log("remoteFolderName: "+remoteFolderName);
                    Debug.Log("AA: " + _ftpFolderName + remoteFolderName);
                    
                    if (!_ftpFolderName.EndsWith("/"))
                        _ftpFolderName = _ftpFolderName + "/";

                    //���ﷵ�ص���ÿ���ļ����ļ���·��ȥ����Ŀ�ļ��еĲ���
                    //���磺/BaseBundlesBuild/0.1.19//bundles_win64/
                    string objecyKey = _ftpFolderName + remoteFolderName+ fileInfo.Name;
                    objecyKey = objecyKey.Replace("//", "/");
                    Debug.Log("objecyKey: " + objecyKey+"___"+ fileInfo.Name);

                    PutObjectResponse putObjectFromFileResponse =
                client.PutObject(bos_path_prefix, objecyKey, fileInfo);

                }

            });
            thread.Start();

        }

    }



    /// <summary>
    /// ����ָ���ļ����е��ļ��������ļ��е��ļ�
    /// </summary>
    /// <param name="result">����֮��Ľ��</param>
    /// <returns></returns>
    public void RecursiveFile(string filePathByForeach, ref List<string> results)
    {
        DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
        DirectoryInfo[] dirInfo = theFolder.GetDirectories();//��ȡ����Ŀ¼���ļ���
        FileInfo[] file = theFolder.GetFiles();//��ȡ����Ŀ¼���ļ�

        foreach (FileInfo fileItem in file) //�����ļ�
        {
            //Debug.Log(fileItem.FullName);
            results.Add(fileItem.FullName);
        }
        //�����ļ���
        foreach (DirectoryInfo NextFolder in dirInfo)
        {
            RecursiveFile(NextFolder.FullName, ref results);
        }
    }

    /// <summary>
    /// ����һ��http����
    /// </summary>
    /// <param name="url"></param>
    /// <param name="postDataStr"></param>
    /// <returns></returns>
    public string HttpPost(string url, string postDataStr)
    {
        WebRequest request = WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";
        byte[] buf = Encoding.UTF8.GetBytes(postDataStr);
        byte[] byteArray = System.Text.Encoding.Default.GetBytes(postDataStr);
        request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
        request.GetRequestStream().Write(buf, 0, buf.Length);
        WebResponse response = request.GetResponse();
        Stream myResponseStream = response.GetResponseStream();
        StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
        string retString = myStreamReader.ReadToEnd();
        myStreamReader.Close();
        myResponseStream.Close();
        return retString;
    }

    /// <summary>
    /// ����һ��Bos�ͻ���
    /// </summary>
    /// <param name="accessKeyId"></param>
    /// <param name="secretAccessKey"></param>
    /// <param name="sessionToken"></param>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    BosClient GenerateBosClient(string accessKeyId, string secretAccessKey, string sessionToken, string endpoint)
    {
        //const string accessKeyId = "AccessKeyID"; // ����Access Key ID
        //const string secretAccessKey = "SecretAccessKey "; // ����Secret Access Key
        //const string endpoint = "https://bj.bcebos.com"; // ָ��BOS��������

        // ��ʼ��һ��BosClient
        BceClientConfiguration config = new BceClientConfiguration();
        //config.Credentials = new DefaultBceCredentials(accessKeyId, secretAccessKey);
        config.Credentials = new DefaultBceSessionCredentials(accessKeyId, secretAccessKey, sessionToken);
        config.Endpoint = endpoint;

        //GetSessionTokenResponse response = new GetSessionTokenResponse();
        //BceCredentials bosstsCredentials = new DefaultBceSessionCredentials(
        //response.getAccessKeyId(),
        //response.getSecretAccessKey(),
        //response.getSessionToken());

        return new BosClient(config);
    }

}
