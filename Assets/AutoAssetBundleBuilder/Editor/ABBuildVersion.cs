//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using UnityEngine;
using UnityEditor;
using System;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		// Place one of these will live in your Editor/Resources folder and every time a Build happens, 
		// it will be rewritten with an updated version number. During the build, the version string is 
		// produced and used by the player to load the correct version of asset bundles, along with the 
		// platform string.  This object itself is NOT part of the build.
		public class ABBuildVersion : ScriptableObject
		{
			static public string kVersionPath = "ABBuildVersion";

			public enum IncrementType
			{
				Manual,
				AutoIncrement,
				TimeInTicks,
			};

			[Tooltip("All three require you to set the major/minor number manually.  This drop-down only controls how the Build value is set.  TimeInTicks is good in case many people can run builds or different build machines might start builds.  Auto just increments every build.  Manual is, well, manual.")]
			public IncrementType incrementType = IncrementType.AutoIncrement;
			public long major = 0;  // hand-edit this
			public long minor = 0;  // hand-edit this
			[Tooltip("This is generally set automatically by the IncrementType every time you build.")]
			public long build = 0;  // this is typically auto-generated, one way or another

			public override string ToString() 
			{
				switch (incrementType)
				{
					case IncrementType.TimeInTicks:
						return string.Format("{0}.{1}.{2:X}", major, minor, build); 

					case IncrementType.AutoIncrement:
					case IncrementType.Manual:
					default:
						return string.Format("{0}.{1}.{2}", major, minor, build); 
				}
			}

			static public ABBuildVersion LoadFromResource()
			{
				ABBuildVersion version = Resources.Load<ABBuildVersion>(ABBuildVersion.kVersionPath);
				if (version==null) Debug.LogError("<color=#ff8080>No ABVersion object found in build.</color>");
				return version;
			}

			//-------------------
			// Convenient way to get the build version asset.
			static public string GetUpdatedVersion()
			{
				// Load up the version object and deal with incrementing as specified, and write out the object again.
				ABBuildVersion version = LoadFromResource();

				switch (version.incrementType)
				{
					case ABBuildVersion.IncrementType.Manual:
						break;
					case ABBuildVersion.IncrementType.AutoIncrement:
						version.build++;
						EditorUtility.SetDirty(version);  // this has to be saved, or you end up with the same version over and over again.  The others do not have this problem.
						AssetDatabase.SaveAssets();
						break;
					case ABBuildVersion.IncrementType.TimeInTicks:
						version.build = DateTime.UtcNow.Ticks;
						break;
				}
				return version.ToString();
			}
		}
	}
}