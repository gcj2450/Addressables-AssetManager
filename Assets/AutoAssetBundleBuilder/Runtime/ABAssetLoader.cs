//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System;
using UnityEngine;
using System.Collections.Generic;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		// Note, this is just an example of how you might load things at runtime.  
		// You are always welcome to use AssetBundle.GetAllLoadedAssetBundles() directly and do your own thing, of course.
		public class ABAssetLoader
		{
			// Public access to the configuration data that we loaded on startup.  There might be useful stuff stashed 
			// in the Bootstrap object that you want to get to, or additional steps to the loader that are custom, perhaps.
			static public Dictionary<string, ABIData> _configData = null;

			// Cached manifest object, which actually lives in _configData.
			static private ABManifest _manifest = null;
		
			// this is a quick way to get an asset bundle by name
			static private Dictionary<string, AssetBundle> _bundleLookup = new Dictionary<string, AssetBundle>();

			//-------------------

			static public void Initialize(Dictionary<string, ABIData> configData)
			{
				_configData = configData;
				_manifest = _configData["Manifest"] as ABManifest;
				if (_manifest==null) Debug.LogError("<color=#ff8080>ABManifestData not loaded into ConfigDataHolder.</color>");

				// Make a quick dictionary for the asset bundles
				for (int i=0; i<_manifest.assetBundles.Length; i++)
				{
					_bundleLookup.Add(_manifest.assetBundles[i].bundleName, _manifest.assetBundles[i].assetBundle);
				}
			}

			//-------------------
			// You can call this manually if you want to dump everything for some reason.
			static public void CleanUp()
			{
				_configData = null;
				_manifest = null;
				_bundleLookup = null;
			}

			//-------------------
			// Load a specific named asset.  May be of any type.
			static public AsyncOperation LoadAssetAsync(string assetName, Action<AsyncOperation> completionCallback)
			{
				string trueAssetName;
				AssetBundle bundle = FindBundleByAssetName(assetName, out trueAssetName);
				AsyncOperation op = bundle.LoadAssetAsync(trueAssetName);
				op.completed += completionCallback;
				return op;
			}

			//-------------------
			// Load a specific named asset and its child assets.  May be of any type.
			static public AsyncOperation LoadAssetWithSubAssetsAsync(string assetName, Action<AsyncOperation> completionCallback)
			{
				string trueAssetName;
				AssetBundle bundle = FindBundleByAssetName(assetName, out trueAssetName);
				AsyncOperation op = bundle.LoadAssetWithSubAssetsAsync(trueAssetName);
				op.completed += completionCallback;
				return op;
			}

			//-------------------
			// Load all the assets in a whole bundle.  You have to name the bundle, this finds it for you.
			static public AsyncOperation LoadAllAssetsFromBundleAsync(string bundleName, Action<AsyncOperation> completionCallback)
			{
				AssetBundle bundle = FindBundleByName(bundleName);
				AsyncOperation op = bundle.LoadAllAssetsAsync();
				op.completed += completionCallback;
				return op;
			}

			//-------------------
			// This dumps extra memory that the asset bundle itself uses if destroyObjectsToo is false.  If destroyObjectsToo is true,
			// this dumps extra memory AND destroys all the objects that were created from this asset bundle.  The former is just a memory
			// savings, the latter is kind of a messy way to manage area-based loading if each asset bundle represents a chunk of a level.
			static public void UnloadAllAssetsFromBundle(string bundleName, bool destroyObjectsToo)
			{
				AssetBundle ab = FindBundleByName(bundleName);
				ab.Unload(destroyObjectsToo);
			}

			//-------------------
			// Returns null if that bundle is not known.
			static public AssetBundle FindBundleByName(string bundleName)
			{
				// Look up the bundle (lower case only, that's how bundle names are anyway)
				AssetBundle bundle = null;
				if (_bundleLookup.TryGetValue(bundleName.ToLowerInvariant(), out bundle))
					return bundle;

				Debug.LogError("<color=#ff8080>FindBundleByName("+bundleName+") did not find bundle.</color>");
				return null;
			}

			//-------------------
			// Looks up the asset bundle and also figures out the name you need to use to load from it.
			static public AssetBundle FindBundleByAssetName(string assetName, out string trueAssetName)
			{
				if (assetName.Length==0) Debug.LogError("ABAssetLoader.FindBundleByAssetName - empty assetName requested.");

				// You can currently use a full name: "assets/bundles/bundlename/foo/bar/etc/asset.prefab"
				string lowerAssetName = assetName.ToLowerInvariant();

				// Walk backwards from most-recently-mounted back to oldest.
				// This does a standard full-name search.
				for (int i=_manifest.assetBundles.Length-1; i>=0; i--)
				{
					ABManifest.ABAssetBundleInfo bundleInfo = _manifest.assetBundles[i];
					if (bundleInfo.assetBundle!=null)
					{
						if (bundleInfo.assetBundle.Contains(lowerAssetName))  // found it straight off
						{
							trueAssetName = lowerAssetName;
							return bundleInfo.assetBundle;
						}
					}
				}

				// Ok, we failed to find the asset.  Let's try a slower, more permissive search.  But ONLY if it starts with a '/', 
				// because otherwise when looking for "asset.prefab", you might get "wrongasset.prefab".
				// This is a partial path name (which is great for reorganizing): "/foo/bar/etc/asset.prefab"
				if (lowerAssetName[0]=='/')
				{
					for (int i=_manifest.assetBundles.Length-1; i>=0; i--)
					{
						if (_manifest.assetBundles[i].assetBundle!=null)
						{
							ABManifest.ABAssetBundleInfo bundleInfo = _manifest.assetBundles[i];
							for (int j=0; j<bundleInfo.assetNames.Length; j++)  // have to do a slower linear search to find partial strings
							{
								if (bundleInfo.assetNames[j].EndsWith(lowerAssetName, StringComparison.InvariantCultureIgnoreCase))
								{
									trueAssetName = bundleInfo.assetNames[j];  // convert the request to the full name of the asset.
									return bundleInfo.assetBundle;
								}
							}
						}
					}
				}

				Debug.LogError("<color=#ff8080>FindBundleByAssetName("+assetName+") did not find asset anywhere.</color>");
				trueAssetName = "[INVALID]";
				return null;
			}
		}
	}
}
