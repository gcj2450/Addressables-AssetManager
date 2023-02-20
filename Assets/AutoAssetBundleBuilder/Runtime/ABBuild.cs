//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System;
using UnityEngine;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		// Note, there is ONE of these in a build, injected at build-time, in a Resources folder.  
		// Normally, there is NOT one of these in your project during development.  However, if you
		// want to run with fully built asset bundles and test the download and execution inside the
		// editor, you have to have one to run properly.  There's a Menu Item for doing exactly that.
		public class ABBuild : ScriptableObject, ABIData
		{
			static public readonly string kBuildPath = "ABBuild";
			static public readonly string kEditorConfigPath = "ABEditorConfig";  // only used for running in the editor

			[Tooltip("To run with bundles in the Editor, the version string must match a currently web-hosted version, or a set of bundles built locally and in the Cached Bundle folder.")]
			public string buildVersion;

			[Tooltip("Valid choices are:\n"
				+"  win32\n"
				+"  win64\n"
				+"  winstore\n"
				+"  linux32\n"
				+"  linux64\n"
				+"  linuxuniv\n"
				+"  osx\n"
				+"  android\n"
				+"  ios\n"
				+"  webgl\n"
				+"  tvos\n"
				+"  ps4\n"
				+"  switch\n"
				+"  xboxone\n"
				+"  magicleap\n"
				+"  stadia\n"
				+"  ps5\n"
				+"  cloud\n"
				+"  gamecorexboxone\n"
				+"  gamecorexboxseries\n"
				)]
			public string platform;

			[Tooltip("This is automatically configured during the build process based on the setting in ABBuildConfig for this platform.\nIf you manually enable it in the ABEditorConfig, it will only load asset bundles from the /StreamingAssets/ folder while in the Editor as well.\nCareful, as you are responsible for keeping those bundles up to date.")]
			public bool selfContained;
		}
	}
}
