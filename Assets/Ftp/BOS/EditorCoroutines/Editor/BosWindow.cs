//using UnityEngine;
//using System.Collections;
//using UnityEditor;
//using System.Collections.Generic;
//using System;
//using System.IO;
//using Newtonsoft.Json.Linq;
//using BaiduBce.Services.Bos;
//using BaiduBce.Services.Bos.Model;
//using System.Threading;
//using BaiduBce.Auth;
//using BaiduBce;
//using System.Net;
//using System.Text;
//using BaiduBce.Services.Sts.Model;
//using System.Runtime.InteropServices.ComTypes;
//using UnityEngine.Experimental.Rendering;

//public class BosWindow : EditorWindow
//{
//    [MenuItem("Window/BosUploader")]
//    public static void ShowWindow()
//    {
//        EditorWindow.GetWindow(typeof(BosWindow));

//        //Initialize("Default");
//    }

//    void OnGUI()
//    {
//        if (GUILayout.Button("GetAccessKey"))
//        {
//            //this.StartCoroutine(Example());

//            string url = "http://test-bd-b-rd0001.bcc-bddwd.baidu.com:8090/api/v1/client/editor/resource/dev/bos/acquire";
//            //platform可选：ios,android,windows
//            string json = "{\"unique_path\": \"baidu\",\"engine\": \"unity\",\"platform\": \"android\"}";

//            string data = HttpPost(url, json);
//            Debug.Log(data);

//            JObject jObj = JObject.Parse(data);
//            JToken codestatus = jObj["code"];
//            JToken message = jObj["message"];

//            if ((int)(codestatus) == 0)
//            {
//                string access_key = (string)jObj["data"]["access_key"];
//                string secret_key = (string)jObj["data"]["secret_key"];
//                string session_token = (string)jObj["data"]["session_token"];
//                string expiration = (string)jObj["data"]["expiration"];
//                string bos_path_prefix = (string)jObj["data"]["bos_path_prefix"];
//                string bos_domain_cdn = (string)jObj["data"]["bos_domain_cdn"];

//                Debug.Log(access_key + "__" + secret_key + "__" +
//                    session_token + "__" + expiration + "__" +
//                    bos_path_prefix + "__" + bos_domain_cdn);

//                string endpoint = "https://bj.bcebos.com";

//                BosClient client = GenerateBosClient(access_key, secret_key, session_token, endpoint);

//                //获取bucketName: xirang-client-editor-res-dev
//                string bucketName = bos_path_prefix;
//                bucketName = bucketName.Substring(0, bucketName.IndexOf("/"));        //您的Bucket名称
//                Debug.Log("bucketName: " + bucketName);

//                bool result = false;
//                result = client.DoesBucketExist(bucketName);
//                Debug.Log(result);
//                //bos_path_prefix : "xirang-client-editor-res-dev/AssertBundle/baidu/unity/ios",

//                //取出字符串:   /AssertBundle/baidu/unity/ios
//                string objectNameFile = bos_path_prefix.Substring(bos_path_prefix.IndexOf("/"));
//                Debug.Log("objectNameFile: " + objectNameFile);

//                string fileName = "D:/GitHub/Addressables-AssetManager/BaseBundlesBuild/0.1.19/config_win64.json";//填物体名字
//                FileInfo fileInfo = new FileInfo(fileName);
//                if (!fileInfo.Exists)
//                {
//                    Debug.Log("AAAAA");
//                }

//                objectNameFile = objectNameFile + "/" + fileInfo.Name;
//                Debug.Log("objectNameFile: " + objectNameFile);

//                //bos_path_prefix: xirang-client-editor-res-dev/AssertBundle/baidu/unity/ios
//                Debug.Log("bos_path_prefix: " + bos_path_prefix);

//                Thread thread = new Thread(() =>
//                {
//                    //DownloadObject(client, bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt", "D:/GitHub/Addressables-AssetManager/BaseBundlesBuild/0.1.19/config_win64.txt");

//                   string url= GeneratePresignedUrl(client, bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt");
//                    Debug.Log(url);
//                    //GetObject(client, bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt");

//                    //拼接后的地址: https://xirang-client-editor-res-dev.cdn.bcebos.com/AssertBundle/baidu/unity/ios/AssertBundle/baidu/unity/ios/config_win64.txt
//                    //PutObjectResponse putObjectFromFileResponse =
//                    //client.PutObject(bos_path_prefix, "/AssertBundle/baidu/unity/ios/config_win64.txt", new FileInfo(fileName));
//                    //Debug.Log(putObjectFromFileResponse.ETAG);
//                });
//                thread.Start();

//                //Debug.Log("bos_path_prefix: " + bos_path_prefix);
//                //List<string> files = ListObjects(client, bos_path_prefix, " xirang-client-editor-res-dev/AssertBundle/baidu/unity/ios");
//                //Debug.Log(files.Count);
//                //const string objectNameFile = "文件形式上传的Object名称";  //文件形式上传的Object名称
//            }

//        }

//        //if (GUILayout.Button("Start WWW"))
//        //{
//        //	this.StartCoroutine(ExampleWWW());
//        //}

//        //if (GUILayout.Button("Start Nested"))
//        //{
//        //	this.StartCoroutine(ExampleNested());
//        //}

//        //if (GUILayout.Button("Stop"))
//        //{
//        //	this.StopCoroutine("Example");
//        //}
//        //if (GUILayout.Button("Stop all"))
//        //{
//        //	this.StopAllCoroutines();
//        //}

//        //if (GUILayout.Button("Also"))
//        //{
//        //	this.StopAllCoroutines();
//        //}

//        //if (GUILayout.Button("WaitUntil/WaitWhile"))
//        //{
//        //	status = false;
//        //	this.StartCoroutine(ExampleWaitUntilWhile());
//        //}

//        //if (GUILayout.Button("Switch For WaitUntil/WaitWhile:" + (status ? "On" : "Off")))
//        //{
//        //	status = !status;
//        //	EditorUtility.SetDirty(this);
//        //}
//    }


//    public string HttpPost(string url, string postDataStr)
//    {
//        WebRequest request = WebRequest.Create(url);
//        request.Method = "POST";
//        request.ContentType = "application/json";
//        byte[] buf = Encoding.UTF8.GetBytes(postDataStr);
//        byte[] byteArray = System.Text.Encoding.Default.GetBytes(postDataStr);
//        request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
//        request.GetRequestStream().Write(buf, 0, buf.Length);
//        WebResponse response = request.GetResponse();
//        Stream myResponseStream = response.GetResponseStream();
//        StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
//        string retString = myStreamReader.ReadToEnd();
//        myStreamReader.Close();
//        myResponseStream.Close();
//        return retString;
//    }

//    public BosClient GenerateBosClient(string accessKeyId, string secretAccessKey, string sessionToken, string endpoint)
//    {
//        //const string accessKeyId = "AccessKeyID"; // 您的Access Key ID
//        //const string secretAccessKey = "SecretAccessKey "; // 您的Secret Access Key
//        //const string endpoint = "https://bj.bcebos.com"; // 指定BOS服务域名

//        // 初始化一个BosClient
//        BceClientConfiguration config = new BceClientConfiguration();
//        //config.Credentials = new DefaultBceCredentials(accessKeyId, secretAccessKey);
//        config.Credentials = new DefaultBceSessionCredentials(accessKeyId, secretAccessKey, sessionToken);
//        config.Endpoint = endpoint;

//        //GetSessionTokenResponse response = new GetSessionTokenResponse();
//        //BceCredentials bosstsCredentials = new DefaultBceSessionCredentials(
//        //response.getAccessKeyId(),
//        //response.getSecretAccessKey(),
//        //response.getSessionToken());

//        return new BosClient(config);
//    }

//    public List<string> ListObjects(BosClient client, string bucketName, string prefix)
//    {
//        List<string> objs = new List<string>();

//        // 构造ListObjectsRequest请求
//        ListObjectsRequest listObjectsRequest = new ListObjectsRequest() { BucketName = bucketName };

//        // 递归列出fun目录下的所有文件
//        listObjectsRequest.Prefix = prefix;

//        // List Objects
//        ListObjectsResponse listObjectsResponse = client.ListObjects(listObjectsRequest);

//        // 遍历所有Object
//        foreach (BosObjectSummary objectSummary in listObjectsResponse.Contents)
//        {
//            Debug.Log("ObjectKey: " + objectSummary.Key);
//        }

//        //// 获取指定Bucket下的所有Object信息
//        //ListObjectsResponse listObjectsResponse = client.ListObjects(bucketName);

//        //// 遍历所有Object
//        //for (int i = 0,cnt= listObjectsResponse.Contents.Count; i < cnt; i++)
//        //{
//        //    BosObjectSummary objectSummary = listObjectsResponse.Contents[i];
//        //    Debug.Log("ObjectKey: " + objectSummary.Key);
//        //    objs.Add(objectSummary.Key);
//        //}
//        return objs;
//    }

//    /// <summary>
//    /// 获取一个文件
//    /// </summary>
//    /// <param name="client"></param>
//    /// <param name="bucketName"></param>
//    /// <param name="objectKey"></param>
//    public void GetObject(BosClient client, String bucketName, String objectKey, string localFilePath)
//    {
//        Debug.Log(objectKey);
//        // 获取Object，返回结果为BosObject对象
//        BosObject bosObject = client.GetObject(bucketName, objectKey);
//        // 获取ObjectMeta
//        ObjectMetadata meta = bosObject.ObjectMetadata;
//        // 获取Object的输入流
//        Stream objectContent = bosObject.ObjectContent;

//        using (Stream file = File.Create(localFilePath))
//        {
//            byte[] buffer = new byte[1024];
//            int len;
//            while ((len = objectContent.Read(buffer, 0, buffer.Length)) > 0)
//            {
//                file.Write(buffer, 0, len);
//            }
//        }

//        objectContent.Close();
//    }

//    /// <summary>
//    /// 下载远程文件到本地
//    /// </summary>
//    /// <param name="client">客户端</param>
//    /// <param name="bucketName">bucketName</param>
//    /// <param name="objectKey">远程路径名</param>
//    /// <param name="localFilePath">本地文件路径</param>
//    public void DownloadObject(BosClient client, string bucketName, string objectKey, string localFilePath)
//    {
//        // 新建GetObjectRequest
//        GetObjectRequest getObjectRequest = new GetObjectRequest() { BucketName = bucketName, Key = objectKey };
//        // 下载Object到文件
//        ObjectMetadata objectMetadata = client.GetObject(getObjectRequest, new FileInfo(localFilePath));
//    }

//    //ExpirationInSeconds为指定的URL有效时长，时间从当前时间算起，为可选参数，
//    //不配置时系统默认值为1800秒。如果要设置为永久不失效的时间，
//    //可以将ExpirationInSeconds参数设置为 -1，不可设置为其他负数。
//    //您可以通过如下代码获取指定Object的URL，该功能通常用于您将Object的URL临时分享给其他用户的场景。
//    /// <summary>
//    /// 为指定文件生成一个URL地址
//    /// </summary>
//    /// <param name="client"></param>
//    /// <param name="bucketName"></param>
//    /// <param name="objectKey"></param>
//    /// <param name="expirationInSeconds"></param>
//    /// <returns></returns>
//    public string GeneratePresignedUrl(BosClient client, string bucketName, string objectKey,
//          int expirationInSeconds=-1)
//    {
//        //指定用户需要获取的Object所在的Bucket名称、该Object名称、时间戳、URL的有效时长
//        Uri url = client.GeneratePresignedUrl(bucketName, objectKey, expirationInSeconds);
//        return url.AbsoluteUri;
//    }

//    //==============
//    //下面示例代码演示了简单获取Object、通过GetObjectRequest获取Object、
//    //直接下载Object到指定路径、只获取ObjectMetadata以及获取Object的URL的完整过程：
//    private void Test()
//    {
//        BosClient client = GenerateBosClient("accKey", "secretKey", "session_token", "url");
//        const string bucketName = "BucketName"; //您的Bucket名称

//        // 初始化，创建Bucket和Object
//        client.CreateBucket(bucketName);
//        string objectName = "ObjectName";//填物体名字
//        //client.PutObject(bucketName, objectName, < SampleData >);

//        // 获取BosObject对象并通过BosObject的输入流获取内容
//        BosObject bosObject = client.GetObject(bucketName, objectName);
//        Stream objectContent = bosObject.ObjectContent;
//        string content = new StreamReader(objectContent).ReadToEnd();
//        Debug.Log(content); // 您传入的<SampleData>

//        // 通过GetObjectRequest只获取部分数据
//        GetObjectRequest getObjectRequest = new GetObjectRequest() { BucketName = bucketName, Key = objectName };
//        getObjectRequest.SetRange(0, 5);
//        bosObject = client.GetObject(getObjectRequest);
//        objectContent = bosObject.ObjectContent;
//        content = new StreamReader(objectContent).ReadToEnd();
//        Debug.Log(content); // 您传入的<SampleData>

//        // 直接通过GetObjectContent获取byte[]内容
//        byte[] bytes = client.GetObjectContent(bucketName, objectName);
//        content = Encoding.Default.GetString(bytes);
//        Debug.Log(content); // 您传入的<SampleData>

//        // 将Object内容下载到文件
//        FileInfo fileInfo = new FileInfo("my file path and name");
//        client.GetObject(bucketName, objectName, fileInfo);
//        content = File.ReadAllText(fileInfo.FullName);
//        Debug.Log(content); // 您传入的<SampleData>

//        // 只获取object的meta，不获取内容
//        ObjectMetadata objectMetadata = client.GetObjectMetadata(bucketName, objectName);
//        Debug.Log(objectMetadata.ContentLength);

//        // 生成url，并通过该url直接下载和打印对象内容
//        string url = client.GeneratePresignedUrl(bucketName, objectName, 60).AbsoluteUri;
//        using (WebClient webClient = new WebClient())
//        {
//            using (Stream stream = webClient.OpenRead(url))
//            using (StreamReader streamReader = new StreamReader(stream))
//            {
//                string response = streamReader.ReadToEnd();
//                Debug.Log(response);  // 您传入的<SampleData>
//            }
//        }
//    }

//    private void MaintTest()
//    {
//        BosClient client = GenerateBosClient("access_key", "secret_key", "session_token", "bos_domain_cdn");
//        //const string objectNameStream = "数据流形式上传的Object名称"; //数据流形式上传的Object名称
//        //const string objectNameString = "ObjectNameString"; //字符串形式上传的Object名称
//        //const string objectNameByte = "ObjectNameByte "; //二进制形式上传的Object名称
//        const string bucketName = "您的Bucket名称";        //您的Bucket名称
//        const string objectNameFile = "文件形式上传的Object名称";  //文件形式上传的Object名称
//        // 新建一个Bucket
//        client.CreateBucket(bucketName); //指定Bucket名称

//        // 设置待上传的文件名
//        const string fileName = "d:\\sample.txt";

//        // 以文件形式上传Object
//        PutObjectResponse putObjectFromFileResponse = client.PutObject(bucketName, objectNameFile,
//            new FileInfo(fileName));

//        //// 以数据流形式上传Object
//        //PutObjectResponse putObjectResponseFromInputStream = client.PutObject(bucketName, objectNameStream,
//        //    new FileInfo(fileName).OpenRead());

//        //// 以二进制串上传Object
//        //PutObjectResponse putObjectResponseFromByte = client.PutObject(bucketName, objectNameByte,
//        //    Encoding.Default.GetBytes("sampledata"));

//        //// 以字符串上传Object
//        //PutObjectResponse putObjectResponseFromString = client.PutObject(bucketName, objectNameString,
//        //    "sampledata");

//        // 打印四种方式的ETag。示例中，文件方式和stream方式的ETag相等，string方式和byte方式的ETag相等
//        Console.WriteLine(putObjectFromFileResponse.ETAG);
//        //Console.WriteLine(putObjectResponseFromInputStream.ETAG);
//        //Console.WriteLine(putObjectResponseFromByte.ETAG);
//        //Console.WriteLine(putObjectResponseFromString.ETAG);

//        // 上传Object并设置自定义参数
//        //ObjectMetadata meta = new ObjectMetadata();
//        //// 设置ContentLength大小
//        //meta.ContentLength = 10;
//        //// 设置ContentType
//        //meta.ContentType = "application/json";
//        //// 设置自定义元数据name的值为my-data
//        //meta.UserMetadata["name"] = "my-data";
//        // 上传Object并打印ETag
//        //putObjectResponseFromString = client.PutObject(bucketName, objectNameString, "sampledata", meta);
//        //Console.WriteLine(putObjectResponseFromString.ETAG);

//        //===========================

//        // 新建一个Bucket
//        client.CreateBucket(bucketName);

//        // 创建bos.jpg,fun/,fun/test.jpg,fun/movie/001.avi,fun/movie/007.avi五个文件
//        client.PutObject(bucketName, "bos.jpg", "sampledata");
//        client.PutObject(bucketName, "fun/", "sampledata");
//        client.PutObject(bucketName, "fun/test.jpg", "sampledata");
//        client.PutObject(bucketName, "fun/movie/001.avi", "sampledata");
//        client.PutObject(bucketName, "fun/movie/007.avi", "sampledata");

//        // 构造ListObjectsRequest请求
//        ListObjectsRequest listObjectsRequest = new ListObjectsRequest() { BucketName = bucketName };

//        // 1. 简单查询，列出Bucket下所有文件
//        ListObjectsResponse listObjectsResponse = client.ListObjects(listObjectsRequest);

//        // 输出：    
//        // Objects:
//        // bos.jpg
//        // fun/
//        // fun/movie/001.avi
//        // fun/movie/007.avi
//        // fun/test.jpg

//        Console.WriteLine("Objects:");
//        foreach (BosObjectSummary objectSummary in listObjectsResponse.Contents)
//        {
//            Console.WriteLine("ObjectKey: " + objectSummary.Key);
//        }

//        // 2. 使用NextMarker分次列出所有文件
//        listObjectsRequest.MaxKeys = 2;
//        listObjectsResponse = client.ListObjects(listObjectsRequest);

//        // 输出：    
//        // Objects:
//        // bos.jpg
//        // fun/
//        // fun/movie/001.avi
//        // fun/movie/007.avi
//        // fun/test.jpg
//        Console.WriteLine("Objects:");
//        while (listObjectsResponse.IsTruncated)
//        {
//            foreach (BosObjectSummary objectSummary in listObjectsResponse.Contents)
//            {
//                Console.WriteLine("ObjectKey: " + objectSummary.Key);
//            }
//            listObjectsResponse = client.ListNextBatchOfObjects(listObjectsResponse);
//        }
//        foreach (BosObjectSummary objectSummary in listObjectsResponse.Contents)
//        {
//            Console.WriteLine("ObjectKey: " + objectSummary.Key);
//        }

//        // 3. 递归列出“fun/”下所有文件和子文件夹
//        listObjectsRequest.Prefix = "fun/";
//        listObjectsResponse = client.ListObjects(listObjectsRequest);

//        // 输出：    
//        // Objects:
//        // fun/
//        // fun/movie/001.avi
//        // fun/movie/007.avi
//        // fun/test.jpg

//        Console.WriteLine("Objects:");
//        foreach (BosObjectSummary objectSummary in listObjectsResponse.Contents)
//        {
//            Console.WriteLine("ObjectKey: " + objectSummary.Key);
//        }

//        // 4. 列出“fun”下的文件和子文件夹
//        listObjectsRequest.Delimiter = "/";
//        listObjectsResponse = client.ListObjects(listObjectsRequest);

//        // 输出：
//        // Objects:
//        // fun/
//        // fun/test.jpg

//        Console.WriteLine("Objects:");
//        foreach (BosObjectSummary objectSummary in listObjectsResponse.Contents)
//        {
//            Console.WriteLine("ObjectKey: " + objectSummary.Key);
//        }

//        // 遍历所有CommonPrefix，相当于获取fun目录下的所有子文件夹
//        // 输出：    
//        // CommonPrefixs:
//        // fun/movie

//        Console.WriteLine("\nCommonPrefixs:");
//        foreach (ObjectPrefix objectPrefix in listObjectsResponse.CommonPrefixes)
//        {
//            Console.WriteLine(objectPrefix.Prefix);
//        }
//    }



//    private bool status;

//    IEnumerator ExampleWaitUntilWhile()
//    {
//        yield return new WaitUntil(() => status);
//        Debug.Log("Switch On");
//        yield return new WaitWhile(() => status);
//        Debug.Log("Switch Off");
//    }

//    IEnumerator Example()
//    {
//        while (true)
//        {
//            Debug.Log("Hello EditorCoroutine!");
//            yield return new WaitForSeconds(2f);
//        }
//    }

//    IEnumerator ExampleNested()
//    {
//        while (true)
//        {
//            yield return new WaitForSeconds(2f);
//            Debug.Log("I'm not nested");
//            yield return this.StartCoroutine(ExampleNestedOneLayer());
//        }
//    }

//    IEnumerator ExampleNestedOneLayer()
//    {
//        yield return new WaitForSeconds(2f);
//        Debug.Log("I'm one layer nested");
//        yield return this.StartCoroutine(ExampleNestedTwoLayers());
//    }

//    IEnumerator ExampleNestedTwoLayers()
//    {
//        yield return new WaitForSeconds(2f);
//        Debug.Log("I'm two layers nested");
//    }

//    class NonEditorClass
//    {
//        public void DoSomething(bool start, bool stop, bool stopAll)
//        {
//            if (start)
//            {
//                EditorCoroutines.StartCoroutine(Example(), this);
//            }
//            if (stop)
//            {
//                EditorCoroutines.StopCoroutine("Example", this);
//            }
//            if (stopAll)
//            {
//                EditorCoroutines.StopAllCoroutines(this);
//            }
//        }

//        IEnumerator Example()
//        {
//            while (true)
//            {
//                Debug.Log("Hello EditorCoroutine!");
//                yield return new WaitForSeconds(2f);
//            }
//        }
//    }
//}
