using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/*ReadMe Tutorial:
 * 最简单流程：安装Addressables Package，打开Addressable Group窗口，新建一个Build Group
 * 如果是需要上传至服务器，需要设置AddressableAssetSettings里的RemoteBuildCatalog为true，
 * 并设置Remote Build/Load Path，把需要打包的预制体或者图片或者音频文件标记为Addressable，
 * 并设置后面的名称比如：Prefabs/BigExplosionEffect.prefab
 * 在Addressable Group里设置Label(可以不设置)，然后点击Group窗口里的Build-> New Build->Default Build Script
 * Build 完成后就可以在设置的路径下找到对应平台的Bundles和Catalog json 文件，把这些文件上传到服务器，然后编写代码下载就可以了
 * 多项目公用Catalog的要求是Unity版本一致，Addressables插件版本一致
 * 用于打包资源的项目需要设置need to have all remote Addressable Groups and Build Remote Catalog turned on. 
 * Concepts：
 * Asset Bundle: A container that holds non-code assets that can be loaded at runtime. 
 * Among other things, they are commonly used for downloadable content (DLC) and
 * loading optimized assets for different target platforms.
 * Addressable Asset: An asset that is marked as “Addressable” and will now be picked up by the Addressable System.
 * Content Catalog: The data store that is used to look up an asset’s physical location via the address.
 * Address: Property of the asset that represents how it can be looked up in the content catalog (the key).
 * Group: How you organize the addressable assets into asset bundles.
 * Label: An optional property that provides an extra layer of control over querying for addressable assets.

 * In Asset packing project, Have AddressableAssetSettings's Remote Catalog Build path to RemoteBuildPath and
 * Remote Catalog Load path to RemoteLoadPath. RemoteBuildPath will have default value as ServerData/[Build Target].
 * For the AssetGroups, BuildPath has to be RemoteBuildPath and Load Path has to be RemoteLoadPath.
 * Build Player Content in the Addressables Window. 
 * After Building, you will get files in [Project folder]/ServerData/[Build Target] folder. 
 * Upload all the files in the CDN server(We are using MicroSoft Azure blobs Storage].
 * 
 * Project 1 (the project to generate the assets)
 * Add the assets to group(s) in the Addressables window.
 * In the AddressableAssetSettings ScriptableObject, 
 * set the remote path to the server you'll be hosting them on. 
 * In my case I tested with a public S3 bucket, so my remote path looked like:
 * https://s3-us-west-2.amazonaws.com/my-s3-bucket-name/Addressables/[BuildTarget]
 * For each of the groups, set:
 * LoadPath to RemoteLoadPath
 * BundleMode to Pack Separately (this might not be important, but I need separate bundles for individual assets)
 * In an editor script I call BuildScript.PrepareRuntimeData().
 * NOTE : This script is marked obsolete and will be replaced in the next version apparently, however it is what I had to work with here.
 * DEVS: There is a problem with calling this directly in regards to switching platforms.
 * e.g. If I'm on Android, and I call BuildScript.PrepareRuntimeData() passing in iOS as the Build Target, 
 * The editor switches to iOS, however the BuildTarget used for the output file paths is left as Android. 
 * I haven't tested whether the output bundles are iOS or Android formatted.
 * In order to work around this I would manually change platform in script, 
 * then using EditorPrefs and UnityEditor.Callbacks.DidReloadScripts work out if I had to call BuildScript.
 * PrepareRuntimeData(). Very messy, but it worked.
 * DEVS: Second issue, is that I can't configure the settings.json output file location or name. 
 * I want to build each asset set out for iOS and Android, 
 * and so when doing this, the settings.json and catalog.json files are overwritten.
 * After each platform build, I would upload the output files to S3,
 * maintaining folder structure, and renaming the settings.json to settings_platform.json so we could fetch them separately.
 * The files would be output to Assets/StreamingAssets/com.unity.addressables
 * 
 * Project 2 (the project to fetch the assets)
 * Here is the example script I used to pull the settings.json and then load an addressable by string key. 
 * 
 * StandaloneWindows64文件夹放的是Addressable打包出来的资源，在打包的时候设置如下：设置Build Remote Catalog为True,
 * 设置Build Path为RemoteBuildPath，设置LoadPath为RemoteLoadPath，地址的设置在Addressable菜单Window > Asset Management > Addressables > Profiles.
 * 注意路径，这里打包的工程和加载Addressable的工程要一致，不然好像找不到，加载资源的项目只需要设置一下这个就行，不需要设置group等其他项
 */


/*
 * Although, you should typically assign unique addresses to your assets, 
 * an asset address is not required to be unique. 
 * You can assign the same address string to more than one asset when useful. 
 * For example, if you have variants of an asset, 
 * you could assign the same address to all the variants and use labels to distinguish between the variants:
 * 
 * Asset 1: address: "plate_armor_rusty", label: "hd"
 * Asset 2: address: "plate_armor_rusty", label: "sd"
 * 
 * Addressables API functions that only load a single asset, such as LoadAssetAsync, 
 * load the first instance found if you call them with an address assigned to multiple assets. 
 * Other functions, like LoadAssetsAsync, 
 * load multiple assets in one operation and load all the assets with the specified address.
 * 
 * In addition to the base AssetReference type, Addressables provides a few more specialized types,
 * such as AssetReferenceGameObject and AssetReferenceTexture. 
 * You can use these specialized subclasses to eliminate the possiblity of assigning the wrong type of asset to an AssetReference field. 
 * In addition, you can use the AssetReferenceUILabelRestriction attribute to limit assignment to Assets with specific labels
 * See Using AssetReferences for more information.
 * 在AddressablesAssets不被使用的时候，它会自动卸载，要求就是用户主动加载的Assets，需要在不用的时候卸载一下
 * 
 */
public class TestLoadAddressable : MonoBehaviour
{
    /// <summary>
    /// 这个地址要和打包时候AddressableAssetSettings中LoadPath一致
    /// </summary>
    string catalogPath = "http://localhost/StandaloneWindows64/catalog_2022.09.22.04.18.38.json";

    // Start is called before the first frame update
    void Start()
    {
        //初始化Addressables
        Addressables.InitializeAsync().Completed += (initOp) =>
        {
            Debug.Log("Initialization Complete ==> " + initOp.Status);
            if (initOp.Status == AsyncOperationStatus.Succeeded)
            {
                //初始化完成加载Catalog

                //官方最佳实践提示传True
                Addressables.LoadContentCatalogAsync(catalogPath, true).Completed += (op) =>
                {
                    Debug.Log("loadCatalogsCompleted ==> " + op.Status);
                    //=======================================================
                    //第一种直接加载资源，生成物体
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        Addressables.LoadAssetAsync<GameObject>("Prefabs/BigExplosionEffect.prefab").Completed += (asscom) =>
                        {
                            Addressables.InstantiateAsync("Prefabs/BigExplosionEffect.prefab", Vector3.right * 5, Quaternion.identity);
                        };
                    }
                    else
                    {
                        Debug.LogError("LoadCatalogsCompleted is failed");
                    }

                    //=======================================================
                    //第二种加载group，加载依赖，再加载资源，再生成物体
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        Debug.Log("loadResourceLocation");
                        //这个Default Local Group是打包的时候设置的组名称
                        AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync("Default Local Group");
                        handle.Completed += groupLoaded;
                    }
                    else
                    {
                        Debug.LogError("LoadCatalogsCompleted is failed");
                    }

                };
            }
        };
    }

    /// <summary>
    /// 加载Group完成
    /// </summary>
    /// <param name="obj"></param>
    void groupLoaded(AsyncOperationHandle<IList<IResourceLocation>> obj)
    {
        Debug.Log("locationsLoaded ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            //加载依赖
            Debug.Log("loadDependency");
            Addressables.DownloadDependenciesAsync("Prefabs/PlasmaExplosionEffect.prefab").Completed += (asop) =>
            {
                dependencyLoaded(asop);
            };
        }
        else
        {
            Debug.LogError("locationsLoaded is failed");
        }
    }

    /// <summary>
    /// 依赖加载完成
    /// </summary>
    /// <param name="obj"></param>
    void dependencyLoaded(AsyncOperationHandle obj)
    {
        Debug.Log("dependencyLoaded ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            //加载Asset
            Addressables.LoadAssetAsync<GameObject>("Prefabs/PlasmaExplosionEffect.prefab").Completed += (ops) =>
            {
                Debug.Log(ops.Status);
                //生成GameObject
                AsyncOperationHandle<GameObject> asyncOperation = Addressables.InstantiateAsync("Prefabs/PlasmaExplosionEffect.prefab", Vector3.zero, Quaternion.identity);
                StartCoroutine(progressAsync(asyncOperation));
                Debug.Log(asyncOperation.Status);
            };
        }
        else
        {
            Debug.LogError("dependencyLoaded is Failed");
        }
    }

    /// <summary>
    /// 输出进度
    /// </summary>
    /// <param name="asyncOperation"></param>
    /// <returns></returns>
    private IEnumerator progressAsync(AsyncOperationHandle<GameObject> asyncOperation)
    {
        float percentLoaded = asyncOperation.PercentComplete;
        while (!asyncOperation.IsDone)
        {
            Debug.Log("Progress = " + percentLoaded + "%");
            yield return 0;
        }
        Debug.Log("Progress Done= " + percentLoaded + "%");
    }

}
