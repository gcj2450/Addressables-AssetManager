//-------------------
// ReachableGames
// Copyright 2019
//-------------------

using System.Collections.Generic;

namespace ReachableGames
{
	namespace AutoBuilder
	{
		// This is a base class strictly to let us make a generic list of anything, so we can stick anything in the config dictionary.
		public interface ABIData
		{
		}

		// To be a loading state, you have to implement these functions.  Each state in the load is sequential, or can early out.
		// The next state always happens unless IsError()==true.
		public interface ABILoadingState
		{
			void   Begin(Dictionary<string, ABIData> configData);  // This is how the state starts, by accepting the output of the previous state.
			void   End();                                          // If successful, End is called to dispose of things.  The stateData dictionary should have more stuff in it too.

			string GetStateText();  // This can change as often as you like.
			bool   IsDone();        // Return true when this state either completes successfully or errors out.
			float  GetProgress();   // 0..1
		}
	}
}