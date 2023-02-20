//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using UnityEngine;

namespace ReachableGames
{
	public class testRotateObj : MonoBehaviour
	{
		public Vector3 eulers;

		void Update()
		{
			transform.Rotate(eulers * Time.deltaTime);
		}
	}
}