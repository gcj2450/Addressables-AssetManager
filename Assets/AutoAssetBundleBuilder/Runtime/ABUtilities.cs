//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System.IO;
using System.Text;
using UnityEngine;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		public class ABUtilities
		{
			//-------------------
			// This exists because of the horrible Byte-Order-Mark issue where the first three bytes of 
			// a file MAY contain indicators for little/big endian and UTF8/16 encodings.  We only try to handle UTF8 for now.
			static public string GetStringFromUTF8File(byte[] data)
			{
				// UTF8
				if (data[0]==0xEF && data[1]==0xBB && data[2]==0xBF)
				{
					return Encoding.UTF8.GetString(data, 3, data.Length - 3);  // BOM, skip them
				}
				return Encoding.UTF8.GetString(data, 0, data.Length);  // assume no BOM
			}

			//-------------------
			// Broke this out so we can use it in the editor too.
			static public string GetRuntimeCacheFolder()
			{
				string cachePath = Application.persistentDataPath + "/Bundles";
				return cachePath;
			}

			//-------------------
			// On windows, this places the runtime asset bundle cache in the folder: C:\Users\[username]\AppData\LocalLow\[YourCompany]\[YourGame]\Bundles
			// You can make it whatever you want, so long as it's relative to Application.persistentDataPath.
			static public void ConfigureCache()
			{
				// Configure the cache so we can use it
				string cachePath = GetRuntimeCacheFolder();
				if (Directory.Exists(cachePath)==false)
					Directory.CreateDirectory(cachePath);

				Cache cache = Caching.GetCacheByPath(cachePath);
				if (!cache.valid)
				{
					cache = Caching.AddCache(cachePath);
					while (cache.ready==false)
					{
					}
				}
				if (!cache.valid) Debug.LogError("<color=#ff8080>Cache is NOT valid at "+cachePath+"</color>");
				if (!cache.ready) Debug.LogError("<color=#ff8080>Cache is NOT ready at "+cachePath+"</color>");
				if (cache.readOnly) Debug.LogError("<color=#ff8080>Cache is read-only at "+cachePath+"</color>");
				Caching.currentCacheForWriting = cache;  // we assume the currentCacheForWriting is the context for caching from now on.
			}

			//-------------------
			// Android is a really, really picky environment when it comes to loading files from /StreamingAssets/
			static public string RemoveDoubleSlashes(string url)
			{
				int schemeIndex = url.IndexOf("://");
				if (schemeIndex!=-1)
				{
					string scheme = url.Substring(0, schemeIndex+3);
					string remainder = url.Substring(schemeIndex+3);
					string result = scheme + remainder.Replace("//", "/");
					return result;
				}
				else  // very simple, no scheme to worry about, so no place there SHOULD be double slashes we need to preserve.
				{
					return url.Replace("//", "/");
				}
			}
		}
	}
}
