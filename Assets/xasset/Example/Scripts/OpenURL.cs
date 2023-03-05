using UnityEngine;

namespace ZionGame.Example
{
    public class OpenURL : MonoBehaviour
    {
        public void Open(string url)
        {
            Application.OpenURL(url);
        }
    }
}