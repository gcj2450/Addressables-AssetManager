using UnityEngine;
using ZionGame;

namespace ReachableGames
{
	public class testSpawnSomePrefabs : MonoBehaviour
	{
		public string[] prefabs;
		public int numStillLoading = 0;
		public GameObject showWhenDoneLoading = null;

		void Start()
		{
			// Kick off a load for each prefab in the array
			for (int i=0; i<prefabs.Length; i++)
			{
				numStillLoading++;
				ABAssetLoader.LoadAssetWithSubAssetsAsync(prefabs[i], PrefabsLoaded);
			}
		}

		// Pop up some text telling us we finished loading
		private void Update()
		{
			if (numStillLoading==0)
			{
				if (showWhenDoneLoading!=null && showWhenDoneLoading.activeSelf==false)
				{
					showWhenDoneLoading.SetActive(true);
					Debug.Log("Application version is "+Application.version);
				}
			}
		}

		void PrefabsLoaded(AsyncOperation op)
		{
			AssetBundleRequest abr = op as AssetBundleRequest;
			if (op.progress==1.0f && op.isDone)
			{
				numStillLoading--;
				GameObject goa = abr.asset as GameObject;
				if (abr.asset==null)
				{
					Debug.Log("Asset loaded as null.");
				}
				else if (goa==null)
				{
					Debug.Log("Asset is not a GameObject: "+abr.asset.name);
				}
				else  // instantiate the game object
				{
					Debug.Log("Instantiating "+abr.asset.name);
					/*GameObject go = */Instantiate<GameObject>(goa);
				}
			}
			else
			{
				Debug.Log("Completed called but progress is "+op.progress+" and done=="+op.isDone);
			}
		}
	}
}
