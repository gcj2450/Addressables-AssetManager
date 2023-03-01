using System;
using System.Collections.Generic;

namespace ZionGame
{
    // This is a base class strictly to let us make a generic list of anything, so we can stick anything in the config dictionary.
    public interface ABIData
    {
    }

    // To be a loading state, you have to implement these functions.  Each state in the load is sequential, or can early out.
    // The next state always happens unless IsError()==true.
    public interface ABILoadingState
    {
        event EventHandler OnError;

        /// <summary>
        /// This is how the state starts, by accepting the output of the previous state.
        /// </summary>
        /// <param name="configData"></param>
        void Begin(Dictionary<string, ABIData> configData);
        /// <summary>
        /// If successful, End is called to dispose of things.  The stateData dictionary should have more stuff in it too.
        /// </summary>
        void End();

        /// <summary>
        /// This can change as often as you like.
        /// </summary>
        /// <returns></returns>
        string GetStateText();
        /// <summary>
        /// Return true when this state either completes successfully or errors out.
        /// </summary>
        /// <returns></returns>
        bool IsDone();
        /// <summary>
        /// 0到1的进度
        /// </summary>
        /// <returns></returns>
        float GetProgress();
        /// <summary>
        /// 重试
        /// </summary>
        void Retry();
    }
}
