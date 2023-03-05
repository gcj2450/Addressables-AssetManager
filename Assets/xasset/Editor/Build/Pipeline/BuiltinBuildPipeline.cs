using UnityEditor;

namespace ZionGame.Editor
{
    public class BuiltinBuildPipeline : ABuildPipeline
    {
        public override IAssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions options, BuildTarget target)
        {
            var manifest = BuildPipeline.BuildAssetBundles(outputPath, builds, options, EditorUserBuildSettings.activeBuildTarget);
            if (manifest != null)
            {
                return new BuiltinAssetBundleManifest(manifest);
            }
            return null;
        }
    }
}