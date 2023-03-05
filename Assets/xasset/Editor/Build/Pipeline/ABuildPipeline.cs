using UnityEditor;

namespace ZionGame.Editor
{
    public abstract class ABuildPipeline
    {
        public abstract IAssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions options, BuildTarget target);
    }
}