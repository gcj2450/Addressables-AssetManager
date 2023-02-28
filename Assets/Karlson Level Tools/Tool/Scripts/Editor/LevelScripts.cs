using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class LevelScripts
{
    public List<ScriptedObject> objects = new List<ScriptedObject>();

    public void AddGameObject(GameObject obj)
    {
        //objects.Add(new ScriptedObject(obj));
    }

    [Serializable]
    public class ScriptedObject
    {
        public string path;
      

        /// <summary>
        /// Returns the path to a GameObject.
        /// Source: http://answers.unity.com/answers/8502/view.html
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
    }
}
