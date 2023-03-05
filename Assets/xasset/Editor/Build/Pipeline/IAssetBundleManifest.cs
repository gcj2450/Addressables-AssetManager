namespace ZionGame.Editor
{
    public interface IAssetBundleManifest
    {
        string[] GetAllAssetBundles();
        string[] GetAllDependencies(string assetBundle);

        string GetAssetBundleHash(string assetBundle);
    }
}