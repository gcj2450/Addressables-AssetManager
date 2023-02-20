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
		// This is generated as a "config_{PLATFORM}.json" file with each build, and is intended to allow you 
		// to redirect logins from a fast-updating location (a website you control) to a set of slower-updated
		// but faster/closer-to-the-user cached CDN files.  That way you have control over your live service
		// with a trivial change of one file pushed out. 
		// TL;DR: Host the config_*.json files on a regular web server, and host assetbundles on a CDN.
		[Serializable]
		public class ABBootstrap : ABIData
		{
			public ABBootstrap() {}
			public ABBootstrap(ABBootstrap copyFrom)
			{
				cdnBundleUrl = copyFrom.cdnBundleUrl;
			}

			[Tooltip("All this data will be stored in a config_{platform}.json file which you will need to host on a website.  It gets fetched first as a means to finding the rest of the game assets.  Specifically, this URL should point to where you host asset bundles, something like https://somecdn.cloudfront.net/game/{PLATFORM}/Bundles/")]
			public string cdnBundleUrl;

			// add anything you might need here, and just edit the ABConfig asset.
		//	public string authUrl;
		//	public string statsUrl;
		//	public string matchmakingUrl;
		//	public string anythingElseYouMightWant;
		}
	}
}