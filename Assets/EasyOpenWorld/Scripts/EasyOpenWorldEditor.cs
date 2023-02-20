#if UNITY_EDITOR	
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyOpenWorld {
    [InitializeOnLoad]
    public class EasyOpenWorldEditor : Editor {

         static EasyOpenWorldEditor() {
            if (EasyOpenWorld.instance == null) {
                EasyOpenWorld.instance = FindObjectOfType<EasyOpenWorld>();
                if (EasyOpenWorld.instance != null) {
                    EasyOpenWorld.instance.LoadFavorites();
                }
            }
            SceneView.duringSceneGui += OnScene;
            
        }
        public static bool showTools;
        public static bool showMap;
        public static bool showNavigation;
        public static string messageLog;
        
        public static List<Vector2> loadedTiles = new List<Vector2>();
        public static List<Vector2> favoritedTiles = new List<Vector2>();
        static void OnScene(SceneView sceneView) {
            Handles.BeginGUI();
            if (EasyOpenWorld.instance == null) {
                EasyOpenWorld.instance = FindObjectOfType<EasyOpenWorld>();
                if (EasyOpenWorld.instance != null) {
                    EasyOpenWorld.instance.LoadFavorites();
                }
            }
            if (EasyOpenWorld.instance != null) {
                GUILayout.BeginArea(new Rect(EasyOpenWorld.instance.UIOffset.x, EasyOpenWorld.instance.UIOffset.y, 1000, 1000));
                if (GUILayout.Button("Play", GUILayout.MaxWidth(50))) {
                    loadedTiles.Clear();
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    EditorSceneManager.OpenScene(EasyOpenWorld.PathToBaseScene + "Base.unity");
                    EditorApplication.isPlaying = true;
                }
                #region map
                var mainBox = EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
                if (showMap) { // draw background box
                    GUI.color = new Color(1, 1, 1, 0.5f); // bg box color
                    GUI.Box(mainBox, GUIContent.none);
                    GUI.color = Color.white;
                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Map " + (showMap ? "<" : ">"), GUILayout.MaxWidth(50))) {
                    showMap = !showMap;
                }
                string scenesTooltipList = "";
                foreach (Vector2 s in favoritedTiles) scenesTooltipList += "" + s + ", ";
                if (GUILayout.Button(new GUIContent() { text = "a", tooltip = "Loads all favorited scenes " + scenesTooltipList }, GUILayout.MaxWidth(17))) {
                    foreach (Vector2 s in favoritedTiles) {
                        EasyOpenWorld.instance.LoadTileEditor((int)s.x, (int)s.y);
                    }
                }
                if (showMap) {
                    if (GUILayout.Button("Unload all", GUILayout.MaxWidth(150))) {
                        EasyOpenWorld.instance.UnloadAll();
                    }
                    if (GUILayout.Button(new GUIContent() { text = "Fill", tooltip = "Adds missing empty scenes" }, GUILayout.MaxWidth(150))) {
                        List<string> scenes = new List<string>();
                        List<string> existingScenes = new List<string>();

                        var sceneNumber = SceneManager.sceneCountInBuildSettings;
                        List<string> listOfSceneNamesInBuild = new List<string>();
                        for (int i = 0; i < sceneNumber; i++) {
                            listOfSceneNamesInBuild.Add(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)));
                        }


                        for (int x = 0; x < EasyOpenWorld.instance.worldSize; x++) {
                            for (int y = 0; y < EasyOpenWorld.instance.worldSize; y++) {
                                string sceneName = x + "_" + y;

                                string path = Application.dataPath + "/" + EasyOpenWorld.PathToTiles.Replace("Assets/", "") + sceneName + ".unity";
                                bool isInBuild = listOfSceneNamesInBuild.Contains(sceneName);
                                bool fileExists = File.Exists(path);
                                if (fileExists == false) {
                                    Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                                    newScene.name = "" + sceneName;
                                    EditorSceneManager.SaveScene(newScene, "" + EasyOpenWorld.PathToTiles + sceneName + ".unity");
                                    Debug.Log("New scene " + sceneName + " created");
                                    EasyOpenWorld.instance.LoadTileEditor(x, y);
                                }
                                //Debug.Log(AssetDatabase.FindAssets("*.unity", new List<string>(){ EasyOpenWorld.PathToTiles}.ToArray()).Length);
                            }
                        }
                    }
                }

                GUILayout.EndHorizontal();

                int btnSize = 28;
                int oldButtonFontSize = GUI.skin.button.fontSize;
                if (showMap) {


                    GUI.skin.button.fontSize = 9;
                    GUILayout.BeginHorizontal(); // begin x axis

                    GUILayout.BeginVertical(); 

                    GUILayout.EndVertical();
                    string activeSceneName = EditorSceneManager.GetActiveScene().name;
                    for (int x = 0; x < EasyOpenWorld.instance.worldSize; x++) {
                        GUILayout.BeginVertical();
                        for (int y = EasyOpenWorld.instance.worldSize - 1; y >= 0; y--) {
                            string sceneName = x + "_" + y;
                            MakeMapTileButton(x, y, activeSceneName, btnSize);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.BeginVertical(); 
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal(); // end of x axis 

                    
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(10);
                    var favoritesBox = EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
                    GUI.color = new Color(1, 1, 1, 0.5f); // bg box color
                    GUI.Box(favoritesBox, GUIContent.none);
                    GUI.color = Color.white;
                    GUILayout.BeginHorizontal();
                    GUILayout.Box("Favorites (Left click to load, right click to remove)");
                    if (GUILayout.Button(new GUIContent() { text = "x", tooltip = "Clear favorites" }, GUILayout.MaxWidth(17))) {
                        EasyOpenWorld.instance.ClearFavorites();
                    }
                    if (GUILayout.Button(new GUIContent() { text = "s", tooltip = "Save currently loaded scenes to favorites" }, GUILayout.MaxWidth(17))) {
                        foreach (Vector2 loaded in loadedTiles) {
                            EasyOpenWorld.instance.AddFavorite(loaded);
                        }
                    }
                    if (GUILayout.Button(new GUIContent() { text = "a", tooltip = "Loads all favorited scenes" }, GUILayout.MaxWidth(17))) {
                        foreach (Vector2 s in favoritedTiles) {
                            EasyOpenWorld.instance.LoadTileEditor((int)s.x, (int)s.y);
                        }
                    }
                    GUILayout.EndHorizontal();



                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(200));

                    List<Vector2> scenesToRemove = new List<Vector2>();
                    foreach (Vector2 s in favoritedTiles) {
                        if (GUILayout.Button((int)s.x+"  \n   "+(int)s.y, GUILayout.MaxWidth(btnSize), GUILayout.MinHeight(btnSize))) {

                            if (Event.current.button == 1/*right click*/) {
                                scenesToRemove.Add(s);
                            } else {
                                EasyOpenWorld.instance.LoadTileEditor((int)s.x, (int)s.y);
                            }
                        }
                    }
                    foreach (Vector2 s in scenesToRemove)
                        EasyOpenWorld.instance.RemoveFavorite(s);

                    GUILayout.EndHorizontal();

                    GUI.skin.button.fontSize = oldButtonFontSize;
                }


                EditorGUILayout.EndVertical();
                #endregion

                #region navigation
                var navigationBox = EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Nav " + (showNavigation ? "<" : ">"), GUILayout.MaxWidth(50))) {
                    showNavigation = !showNavigation;
                }

                GUILayout.EndHorizontal();


                btnSize = 28;
                oldButtonFontSize = GUI.skin.button.fontSize;
                if (showNavigation) {


                    string activeSceneName = EditorSceneManager.GetActiveScene().name;

                    if (activeSceneName != "Base") {

                        GUI.skin.button.fontSize = 9;

                        int x = System.Convert.ToInt32(activeSceneName.Split('_')[0]);
                        int y = System.Convert.ToInt32(activeSceneName.Split('_')[1]);

                        GUILayout.BeginHorizontal(); // begin x axis
                        MakeMapTileButton(x - 1, y + 1, activeSceneName, btnSize);
                        MakeMapTileButton(x, y + 1, activeSceneName, btnSize);
                        MakeMapTileButton(x + 1, y + 1, activeSceneName, btnSize);
                        GUILayout.EndHorizontal(); // end of x axis 
                        GUILayout.BeginHorizontal(); // begin x axis
                        MakeMapTileButton(x - 1, y, activeSceneName, btnSize);
                        MakeMapTileButton(x, y, activeSceneName, btnSize);
                        MakeMapTileButton(x + 1, y, activeSceneName, btnSize);
                        GUILayout.EndHorizontal(); // end of x axis
                        GUILayout.BeginHorizontal(); // begin x axis
                        MakeMapTileButton(x - 1, y - 1, activeSceneName, btnSize);
                        MakeMapTileButton(x, y - 1, activeSceneName, btnSize);
                        MakeMapTileButton(x + 1, y - 1, activeSceneName, btnSize);
                        GUILayout.EndHorizontal(); // end of x axis 

                        GUI.skin.button.fontSize = oldButtonFontSize;
                        GUILayout.Space(btnSize * 0.5f);
                    } else {
                        GUILayout.Box("Base scene active\nNavigation unavailable");
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent() { text = "u", tooltip = "Unload all scenes" }, GUILayout.MaxWidth(30))) {
                        EasyOpenWorld.instance.UnloadAll();
                    }
                    if (GUILayout.Button(new GUIContent() { text = "u o", tooltip = "Unload other scenes (except active)" }, GUILayout.MaxWidth(40))) {
                        string currentActive = activeSceneName;
                        EasyOpenWorld.instance.UnloadAll();
                        EasyOpenWorld.instance.LoadTileEditor(System.Convert.ToInt32(currentActive.Split('_')[0]), System.Convert.ToInt32(currentActive.Split('_')[1]));
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                #endregion

                GUILayout.EndArea();
            }
            if (messageLog != null && messageLog != "") { // error messages
                var messageLogBox = EditorGUILayout.BeginVertical();
                GUI.color = new Color(1, 0.5f, 0.5f, 0.5f); // bg box color
                GUI.Box(messageLogBox, GUIContent.none);
                GUI.color = Color.white;

                GUILayout.Label(messageLog);
                if (GUILayout.Button("Ok", GUILayout.MaxWidth(50))) {
                    messageLog = "";
                }
                EditorGUILayout.EndVertical();
            }


            Handles.EndGUI();
        }

        public static void AddMessageToLog(string s) {
            messageLog = s;
        }

        public static void MakeMapTileButton(int x, int y, string activeSceneName, int btnSize) {
            string sceneName = x + "_" + y;
            if (x < 0 || y < 0 || x > EasyOpenWorld.instance.worldSize || y > EasyOpenWorld.instance.worldSize) { // tile is out of bounds > draw blank box
                GUILayout.Box("", GUILayout.MaxWidth(btnSize), GUILayout.MinHeight(24), GUILayout.MaxHeight(24));
            } else {
                bool loaded = false;
                if (loadedTiles.Contains(new Vector2(x, y))) loaded = true;

                if (activeSceneName == sceneName)
                    GUI.color = new Color(0, 1f, 0, 1f);
                else if (loaded)
                    GUI.color = new Color(0.4f, 0.9f, 0, 0.65f);
                if (GUILayout.Button(x + "  \n   " + y, GUILayout.MaxWidth(btnSize), GUILayout.MinHeight(24), GUILayout.MaxHeight(24))) {
                    if (Event.current.button == 1/*right click*/) {
                        EasyOpenWorld.instance.FocusEditorCameraOnTile(x, y);
                        if (loaded) {
                            Scene scene = SceneManager.GetSceneByName(x + "_" + y);
                            EditorSceneManager.SetActiveScene(scene);
                        }
                    } else if (!loaded) {
                        EasyOpenWorld.instance.LoadTileEditor(x, y);
                    } else {//unload
                        EasyOpenWorld.instance.UnloadTile(x, y);
                    }
                }
                GUI.color = Color.white;
            }
            
        }
    }
}
#endif