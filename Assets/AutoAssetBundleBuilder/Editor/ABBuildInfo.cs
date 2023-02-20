//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using UnityEditor;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		// Struct to help collect configuration data.
		public struct ABBuildInfo
		{
			public ABBuildInfo(BuildTarget target, string targetString, BuildOptions playerOptions, BuildAssetBundleOptions bundleOptions, string destFolder, string exeFilename, string srcBundleFolder, 
								string destBundleFolder, string destConfigFolder, string configJson, bool selfContainedBuild, bool waitForDebugger, bool serverBuild, string embedBundleFolder, string buildVersion, bool doLogging, 
								string[] ignoreEndsWith, string[] ignoreContains, string[] ignoreExact)
			{
				Target = target;
				TargetString = targetString;
				PlayerOptions = playerOptions;
				BundleOptions = bundleOptions;
				DestFolder = destFolder;
				ExeFilename = exeFilename;
				SrcBundleFolder = srcBundleFolder;
				DestBundleFolder = destBundleFolder;
				DestConfigFolder = destConfigFolder;
				ConfigJson = configJson;
				SelfContainedBuild = selfContainedBuild;
				WaitForDebugger = waitForDebugger;
				ServerBuild = serverBuild;
				EmbedBundleFolder = embedBundleFolder;
				BuildVersion = buildVersion;

				Logging = doLogging;
				IgnoreEndsWith = ignoreEndsWith;
				IgnoreContains = ignoreContains;
				IgnoreExact = ignoreExact;
			}
			public BuildTarget             Target { get; private set; }
			public string                  TargetString { get; private set; }
			public BuildOptions            PlayerOptions { get; private set; }
			public BuildAssetBundleOptions BundleOptions { get; private set; }
			public string                  DestFolder { get; private set; }
			public string                  ExeFilename { get; private set; }
			public string                  SrcBundleFolder { get; private set; }
			public string                  DestBundleFolder { get; private set; }
			public string                  DestConfigFolder { get; private set; }
			public string                  ConfigJson { get; private set; }
			public bool                    SelfContainedBuild { get; private set; }
			public bool                    WaitForDebugger { get; private set; }
			public bool                    ServerBuild { get; private set; }
			public string                  EmbedBundleFolder { get; private set; }
			public string                  BuildVersion { get; private set; }

			public bool                    Logging { get; private set; }
			public string[]                IgnoreEndsWith { get; private set; }
			public string[]                IgnoreContains { get; private set; }
			public string[]                IgnoreExact { get; private set; }
		}
	}
}