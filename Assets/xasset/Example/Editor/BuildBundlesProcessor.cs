using UnityEditor;
using UnityEngine;
using ZionGame.Editor;

namespace ZionGame.Example.Editor
{
    [InitializeOnLoad]
    public static class BuildBundlesProcessor
    {
        static BuildBundlesProcessor()
        {
            BuildScript.preprocessBuildBundles += PreprocessBuildBundles;
            BuildScript.postprocessBuildBundles += PostprocessBuildBundles;
        }

        private static void PreprocessBuildBundles(BuildTask task)
        {
            Debug.LogFormat("Prepare build bundles for {0}", task.name);
        }

        private static void PostprocessBuildBundles(BuildTask task)
        {
            Settings.GetDefaultSettings().Initialize();
            Debug.LogFormat("Post build bundles for {0} with files: {1}", task.name,
                string.Join("\n", task.changes.ConvertAll(Settings.GetBuildPath)));
        }
    }
}