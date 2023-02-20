using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AssetBundleManager
{
    public static AssetBundleManager Instace
    {
        get
        {
            if (_instace == null) _instace = new AssetBundleManager();
            return _instace;
        }
    }
    private static AssetBundleManager _instace = null;

    private AssetBundleManifest manifest = null;
    private Dictionary<string, AssetBundle> dicAssetBundle = new Dictionary<string, AssetBundle>();

    // filename : Assets全路径，比如Assets/Prefab/***.prefab
    public AssetBundle GetAssetBundle(string filePath)
    {
        AssetBundle ab = null;
        dicAssetBundle.TryGetValue(AssetsNameToBundleName(filePath), out ab);
        return ab;
    }

    // 加载manifest，用来处理关联资源
    public void LoadManifest()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(MainifestFilePath());
        manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        // 压缩包直接释放掉
        bundle.Unload(false);
        bundle = null;
    }

    // filename : Assets全路径，比如Assets/Prefab/***.prefab
    public AssetBundle Load(string filename)
    {
        string bundleName = AssetsNameToBundleName(filename);
        if (dicAssetBundle.ContainsKey(bundleName))
        {
            return dicAssetBundle[bundleName];
        }

        string[] dependence = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < dependence.Length; ++i)
        {
            LoadInternal(dependence[i]);
        }

        return LoadInternal(bundleName);
    }

    // filename : Assets全路径，比如Assets/Prefab/***.prefab
    public IEnumerator LoadAsync(string filename)
    {
        string bundleName = AssetsNameToBundleName(filename);
        if (dicAssetBundle.ContainsKey(bundleName))
        {
            yield break;
        }

        string[] dependence = manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < dependence.Length; ++i)
        {
            yield return LoadInternalAsync(dependence[i]);
        }

        yield return LoadInternalAsync(bundleName);
    }

    public void Unload(string filename, bool force = false)
    {
        string bundleName = AssetsNameToBundleName(filename);

        AssetBundle ab = null;
        if (dicAssetBundle.TryGetValue(bundleName, out ab) == false) return;

        if (ab == null) return;

        ab.Unload(force);
        ab = null;
        dicAssetBundle.Remove(bundleName);
    }

    public void UnloadUnusedAssets()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    public void Release()
    {
        foreach (var pair in dicAssetBundle)
        {
            var bundle = pair.Value;
            if (bundle != null)
            {
                bundle.Unload(true);
                bundle = null;
            }
        }
        dicAssetBundle.Clear();
    }

    private AssetBundle LoadInternal(string bundleName)
    {
        if (dicAssetBundle.ContainsKey(bundleName))
        {
            return dicAssetBundle[bundleName];
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(BundleNameToBundlePath(bundleName));
        dicAssetBundle.Add(bundleName, bundle);
        return bundle;
    }

    private IEnumerator LoadInternalAsync(string bundleName)
    {
        if (dicAssetBundle.ContainsKey(bundleName))
        {
            yield break;
        }

        AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(BundleNameToBundlePath(bundleName));
        yield return req;

        dicAssetBundle.Add(bundleName, req.assetBundle);
    }

    // 名字依赖于存放目录
    private string MainifestFilePath()
    {
        return Application.dataPath + "/StreamingAssets/StreamingAssets";
    }

    // Assets/Prefab/***.prefab --> assets.prefab.***.prefab.assetbundle
    private string AssetsNameToBundleName(string file)
    {
        string f = file.Replace('/', '.');
        f = f.ToLower();
        f += ".assetbundle";
        return f;
    }

    // assets.prefab.***.prefab.assetbundle --> C:/***path***/assets.prefab.***.prefab.assetbundle
    private string BundleNameToBundlePath(string bundleFilename)
    {
        return System.IO.Path.Combine(Application.dataPath + "/StreamingAssets/", bundleFilename);
    }

}