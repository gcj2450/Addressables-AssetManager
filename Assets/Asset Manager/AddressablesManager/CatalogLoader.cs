using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class CatalogLoader : MonoBehaviour
{
    /// <summary>
    /// Unity CCD dashboard
    /// You will need permissions to access or make your own.
    /// https://dashboard.unity3d.com/organizations/4673310068612/projects/a6db56ab-d919-4645-8f08-1523bb6b918a/cloud-content-delivery/buckets/2c9b4434-d871-41ff-942e-36b86667fbdf/latest
    /// 
    /// Latest content
    /// https://(ProjectID).client-api.unity3dusercontent.com/client_api/v1/buckets/(BucketID)/entry_by_path/content/?path=    
    /// https://a6db56ab-d919-4645-8f08-1523bb6b918a.client-api.unity3dusercontent.com/client_api/v1/buckets/2c9b4434-d871-41ff-942e-36b86667fbdf/release_by_badge/latest/entry_by_path/content/?path=
    /// especific badge
    /// https://(ProjectID).client-api.unity3dusercontent.com/client_api/v1/buckets/(BucketID)/release_by_badge/(BadgeName)/entry_by_path/content/?path=
    /// 
    ///</summary>

    [TextArea(3,5)]
    public string bucketURL = "url_to_bucket";

    public string catalogPath = "path_in_bucket";

    void Start()
    {
        Addressables.InitializeAsync();
    }

    IEnumerator GetDownloadSize ( List<string> _locations )
    {
        AsyncOperationHandle<long> getDownloadSize = Addressables.GetDownloadSizeAsync( _locations );
        yield return getDownloadSize;

        //If the download size is greater than 0, download all the dependencies.
        if( getDownloadSize.Result > 0 )
        {
            AsyncOperationHandle downloadDependencies = Addressables.DownloadDependenciesAsync( _locations );
            yield return downloadDependencies;
        }
    }

    public async void LoadCatalog (/*System.Action<long> downloadSize*/)
    {
        //Load a catalog and automatically release the operation handle.
        AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(bucketURL + catalogPath, false);
        await handle.Task;

        var locator = handle.Result;

        string msg = "***LoadCatalog >> id: " + locator.LocatorId + "\nKeys >>\n";
        foreach (var key in locator.Keys)
            msg += key.ToString() + "\n";
        Debug.Log(msg);


        AsyncOperationHandle<long> getDownloadSize = Addressables.GetDownloadSizeAsync(locator.Keys);
        await getDownloadSize.Task;

        long m_CatalogDownloadSize = getDownloadSize.Result;
        Debug.Log($"LoadCatalog >> Download size: {m_CatalogDownloadSize}");
    }

    public async void SpawnPrefab (string _address)
    {
        var handle = Addressables.InstantiateAsync( _address );
        await handle.Task;

        LogHandle("SpawnPrefab", handle);

        Addressables.Release(handle);
    }

    public async void LoadScene(string _address)
    {
        if (string.IsNullOrEmpty(_address))
            return;

        var handle = Addressables.LoadSceneAsync(_address);
        await handle.Task;

        LogHandle("LoadScene", handle);

        Addressables.Release(handle);
    }

    public async void LoadGameObject ( string _address )
    {
        var handle = Addressables.LoadAssetAsync<GameObject>( _address );
        await handle.Task;

        LogHandle("LoadGameObject", handle);

        Addressables.Release(handle);
    }

    public async void LoadTexture ( string _address )
    {
        var handle = Addressables.LoadAssetAsync<Texture2D>( _address );
        await handle.Task;

        LogHandle("LoadTexture",handle);

        Addressables.Release(handle);
    }

    public async void LoadMesh ( string _address )
    {
        var handle = Addressables.LoadAssetAsync<Mesh>( _address );
        await handle.Task;

        LogHandle("LoadMesh", handle);

        Addressables.Release(handle);
    }

#if ASYNC
    public async void LoadMaterial(string _address)
    {
        var handle = Addressables.LoadAssetAsync<Material>(_address);
        await handle.Task;

        LogHandle("LoadMaterial", handle);

        Addressables.Release(handle);
    }
#else
    public void LoadMaterial(string _address)
    {
        var handle = Addressables.LoadAssetAsync<Material>(_address);

        handle.Completed += op =>
        {
            LogHandle("LoadMaterial", handle);

            Addressables.Release(handle);
        };
    }
#endif

    public async void DownloadDependencies ( string _address )
    {
        if( string.IsNullOrEmpty( _address ) )
            return;

        var handle = Addressables.DownloadDependenciesAsync( _address );
        await handle.Task;

        LogHandle("DownloadDependencies",handle);

        Addressables.Release(handle);
    }

    void LogHandle(string invoker, AsyncOperationHandle handle)
    {
        Debug.Log($"***{invoker} >> status: {handle.Status}");

        if (handle.OperationException != null)
            Debug.LogError($"***{invoker} >> {handle.OperationException.Message}");
        else
            Debug.Log($"***{invoker} >> {handle.Result}");
    }

    public void ClearCache ()
    {
        Caching.ClearCache();

        Debug.Log("Deleted ALL cache");
        //Addressables.ClearDependencyCacheAsync( key );
    }
}
