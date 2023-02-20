using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.Networking;
//using UnityEngine.Experimental.Networking;

//
//public static RequestData GetHomeConfigureRequestData()
//  {
//      string url = string.Format("{0}/player/get-player-configure.html", Prefix);
//      var parameters = new Dictionary<string, string>();
//      parameters.Add("version", Version);
//      parameters.Add("platform", Platform);
//      parameters.Add("sub_time", TimeStamp);
//      parameters.Add("sign", GetSign(parameters));
//      return new RequestData(url, false)
//      {
//          PostData = parameters
//      };
//  }
//public void LoadConfiguration()
//{
//    var requestData = UrlHelper.GetHomeConfigureRequestData();
//    requestData.Callback = OnLoadConfigurationCallback;
//    RequestManager.Instance.AddRequest(requestData);
//}
/// <summary>
/// 网络请求管理类
/// </summary>
public class RequestManager : MonoBehaviour
{
    private static RequestManager _instance;
    public static RequestManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(RequestManager)) as RequestManager;
                if (_instance == null)
                {
                    var requestManager = new GameObject("RequestManager");
                    _instance = requestManager.AddComponent<RequestManager>();
                }
            }

            return _instance;
        }
    }

    private List<RequestData> _requestList = new List<RequestData>();
    private const int MaxRequestThread = 2;
    private const int MaxRetryTimes = 2;
    private int _currentRequestNumber;

    private string CacheFolder
    {
        get
        {
            return string.Format("{0}/RequestCache/{1}", Application.persistentDataPath, _cacheFolder);
        }
    }

    private string CacheFile
    {
        get
        {
            return string.Format("{0}/Request.dat", CacheFolder);
        }
    }

    private string _cacheFolder = "";
    private Dictionary<string, string> _cacheDictionary;

    void Awake()
    {
        Initialize("Default");
    }

    public void Initialize(string cacheFolder)
    {
        _requestList = new List<RequestData>();
        _cacheFolder = cacheFolder;
        LoadCache();
    }

    public void AddRequest(RequestData request)
    {
        if (request == null) return;
        if (request.UseCache && request.RetryTimes == 0)
        {
            string cacheResult = GetFromCache(request.Url);
            if (!string.IsNullOrEmpty(cacheResult))
            {
                request.Callback(request, cacheResult);
            }
        }

        _requestList.Add(request);
        DoRequest();
    }

    private void DoRequest()
    {
        if (_requestList.Count > 0 && _currentRequestNumber < MaxRequestThread)
        {
            _currentRequestNumber++;
            RequestData curData = _requestList[0];
            StartCoroutine(DoRequestAsync(curData));
            _requestList.RemoveAt(0);
        }
    }

    private IEnumerator DoRequestAsync(RequestData request)
    {
        UnityWebRequest webRequest;
        if (request.PostData != null)
        {
            var formData = request.PostData as WWWForm;
            if (formData != null)
            {
                webRequest = UnityWebRequest.Post(request.Url, formData);
            }
            else
            {
                var dic = request.PostData as Dictionary<string, string>;
                if (dic != null)
                {
                    webRequest = UnityWebRequest.Post(request.Url, dic);
                }
                else
                {
                    webRequest = UnityWebRequest.Post(request.Url, request.PostData.ToString());
                }
            }
        }
        else
        {
            webRequest = UnityWebRequest.Get(request.Url);
        }
        yield return webRequest.SendWebRequest();
        _currentRequestNumber--;
        if (webRequest.error == null)
        {
            Debug.Log(string.Format("url:{0}, callback:{1}", webRequest.url, webRequest.downloadHandler.text));
            if (request.Callback != null)
            {
                request.Callback(request, webRequest.downloadHandler.text);
            }

            if (request.UseCache)
            {
                SaveIntoCache(request.Url, webRequest.downloadHandler.text);
            }
            DoRequest();
        }
        else
        {
            if (request.RetryTimes < MaxRetryTimes)
            {
                request.RetryTimes++;
                AddRequest(request);
            }
            else
            {
                if (GetFromCache(request.Url) == null)
                {
                    if (request.Callback != null)
                    {
                        request.Callback(request, null);
                    }

                }
                DoRequest();
            }
        }
    }

    private void SaveIntoCache(string url, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (_cacheDictionary.ContainsKey(url))
        {
            _cacheDictionary[url] = text;
        }
        else
        {
            _cacheDictionary.Add(url, text);
        }
        SaveCache();
    }

    public string GetFromCache(string url)
    {
        if (_cacheDictionary.ContainsKey(url))
        {
            return _cacheDictionary[url];
        }

        return null;
    }

    private void LoadCache()
    {
        var cacheFile = CacheFile;
        if (File.Exists(cacheFile))
        {
            FileStream readstream = null;
            try
            {
                readstream = new FileStream(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                var formatter = new BinaryFormatter();
                _cacheDictionary = (Dictionary<string, string>)formatter.Deserialize(readstream);
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("load cache exception:{0}-----------ex:{1}", cacheFile, ex.Message));
                _cacheDictionary = new Dictionary<string, string>();
            }
            finally
            {
                if (readstream != null)
                {
                    readstream.Close();
                }
            }
        }
        else
        {
            _cacheDictionary = new Dictionary<string, string>();
        }
    }

    private void SaveCache()
    {
        var cacheFile = CacheFile;
        if (!Directory.Exists(CacheFolder))
        {
            Directory.CreateDirectory(CacheFolder);
        }

        FileStream fileStream = null;
        try
        {
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
            fileStream = new FileStream(cacheFile, FileMode.Create, FileAccess.Write, FileShare.None);
            var formatter = new BinaryFormatter();
            formatter.Serialize(fileStream, _cacheDictionary);
        }
        catch (Exception ex)
        {
            Debug.Log(string.Format("save cache exception:{0}-----------ex:{1}", cacheFile, ex.Message));
        }
        finally
        {
            if (fileStream != null)
            {
                fileStream.Close();
            }
        }
    }
}

public class RequestData
{
    public string Url { get; private set; }
    public bool UseCache { get; private set; }
    public object PostData { get; set; }
    public Action<RequestData, string> Callback { get; set; }
    public int RetryTimes { get; set; }
    public object Parameter { get; set; }

    public RequestData(string url, bool useCache = true)
    {
        Url = url;
        UseCache = useCache;
    }
    public RequestData(string url, Action<RequestData, string> callback = null, bool useCache = true)
    {
        Url = url;
        UseCache = useCache;
        Callback = callback;
    }
}
