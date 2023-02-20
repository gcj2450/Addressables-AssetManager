using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyOpenWorld {
    public class EasyOpenWorld : MonoBehaviour {

        public static EasyOpenWorld instance;
        private void Awake() {
            instance = this;
        }

        public Transform player;
        public int worldSize; // how many tiles in world
        public int tileSize; // how many units per tile
        public Vector2 worldOffset;
        public const string PathToTiles = "Assets/EasyOpenWorld/Tiles/";
        public const string PathToBaseScene = "Assets/EasyOpenWorld/";
        public Vector2 UIOffset = new Vector2(10,10);

        public enum Logging {
            none,
            errors,
            all
        }
        public Logging logging;

        [Tooltip("Y coordinate of the camera when a tile is focused")]
        public int cameraHeight = 100;
        public Dictionary<Vector2, string> sceneList = new Dictionary<Vector2, string>();
        public List<Vector2> visibleScenes = new List<Vector2>();

        void Start() {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            string[] scenes = new string[sceneCount];
            for (int i = 0; i < sceneCount; i++) {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                scenes[i] = sceneName;
                if (sceneName != "Base" && sceneName != "EmptyScene") {
                    try {
                        int x = System.Convert.ToInt32(sceneName.Split('_')[0]);
                        int y = System.Convert.ToInt32(sceneName.Split('_')[1]);
                        sceneList.Add(new Vector2(x, y), sceneName);
                        if (logging == Logging.all) Debug.Log("Found tile "+sceneName);
                    } catch {
                    }
                }
            }
        }

        void Update() {
            CheckAndLoad();
        }
        void CheckAndLoad() {
            bool loadNearbyScenes = true;
            int currentX = (int)((player.position.x-worldOffset.x) / tileSize);
            int currentY = (int)((player.position.z-worldOffset.y) / tileSize);
            Vector2 pos = new Vector2((int)currentX, (int)currentY);

            List<Vector2> oldScenes = new List<Vector2>();
            foreach (Vector2 s in visibleScenes) {
                oldScenes.Add(s);
            }
            List<Vector2> freshlyLoadedScenes = new List<Vector2>();
            for (int i = 0; i < 9; i++) {
                Vector2 currentPos = Vector2.zero;
                if (i == 0) {currentPos = pos + new Vector2(0, 0); Debug.Log("current "+currentPos); }
                else if (i == 1) currentPos = pos + new Vector2(1, 0);
                else if (i == 2) currentPos = pos + new Vector2(-1, 0);
                else if (i == 3) currentPos = pos + new Vector2(0, 1);
                else if (i == 4) currentPos = pos + new Vector2(0, -1);
                else if (i == 5) currentPos = pos + new Vector2(1, 1);
                else if (i == 6) currentPos = pos + new Vector2(-1, -1);
                else if (i == 7) currentPos = pos + new Vector2(1, -1);
                else if (i == 8) currentPos = pos + new Vector2(-1, 1);

                freshlyLoadedScenes.Add(currentPos);
                if (visibleScenes.Contains(currentPos) == false) {
                    if (SceneExists(currentPos)) {
                        if (i == 0 || loadNearbyScenes) {
                            visibleScenes.Add(currentPos);
                            StartCoroutine(LoadScene(currentPos));
                        }
                    } else {
                        if (logging == Logging.errors)
                            Debug.Log("Failed to load " + currentPos + ", scene doesn't exist. Make sure the scene file exists at "+PathToTiles+" and is added to the Scene In Build list");
                    }
                }
            }
            foreach (Vector2 s in oldScenes) {
                if (freshlyLoadedScenes.Contains(s) == false) {
                    UnloadScene(s);
                    visibleScenes.Remove(s);
                }
            }
        }

        public bool SceneLoaded(Vector2 v) {
            return visibleScenes.Contains(v);
        }

        public bool SceneExists(Vector2 pos) {
            return sceneList.ContainsKey(pos);
        }

        IEnumerator LoadScene(Vector2 pos) {
            string sceneName = sceneList[pos];
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (op.isDone == false) {
                yield return null;
            }

            if (visibleScenes.Contains(pos) == false) {
                UnloadScene(pos);
            }
        }

        public void UnloadScene(Vector2 pos) {
            string sceneName = sceneList[pos];
            SceneManager.UnloadSceneAsync(sceneName);
        }




#if UNITY_EDITOR
        public void LoadTileEditor(int x, int y) {
            string activeSceneName = EditorSceneManager.GetActiveScene().name;
            string path = PathToTiles + x + "_" + y + ".unity";

            try {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (EasyOpenWorldEditor.loadedTiles.Contains(new Vector2(x, y)) == false)
                    EasyOpenWorldEditor.loadedTiles.Add(new Vector2(x, y));
                AddFavorite(new Vector2(x, y));
                if (activeSceneName == "Base") {// set loaded scene active if we're  in base scene
                    Scene scene = SceneManager.GetSceneByName(x + "_" + y);
                    EditorSceneManager.SetActiveScene(scene);
                }
            } catch {
                EasyOpenWorldEditor.AddMessageToLog("Failed to load scene " + x + "_" + y + ". \nMake sure scene exists at " + path + " and is added to File > Build Settings > Scenes In Build\nYou can also go to Map > Fill to add empty scenes to fill the map");
            }
        }
        public void UnloadTile(int x, int y) {
            string sceneName = x + "_" + y;
            EditorSceneManager.CloseScene(SceneManager.GetSceneByName(sceneName), true);
            EasyOpenWorldEditor.loadedTiles.Remove(new Vector2(x, y));
            if (EasyOpenWorldEditor.loadedTiles.Count > 0) {
                Scene scene = SceneManager.GetSceneByName(EasyOpenWorldEditor.loadedTiles[0].x + "_" + EasyOpenWorldEditor.loadedTiles[0].y);//set first in list as active, otherwise basescene is set active by default
                EditorSceneManager.SetActiveScene(scene);
            }
        }
        public void UnloadAll() {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.SetActiveScene(EditorSceneManager.OpenScene(PathToBaseScene + "Base.unity", OpenSceneMode.Single));
            EasyOpenWorldEditor.loadedTiles.Clear();
        }

        public void FocusEditorCameraOnTile(int x, int y) {
            var view = SceneView.lastActiveSceneView;
            if (view != null) {
                var target = new GameObject();
                target.transform.position = new Vector3(x * tileSize + tileSize * 0.5f, cameraHeight, y * tileSize + tileSize * 0.5f);
                target.transform.rotation = Quaternion.Euler(90, 0, 0);
                view.AlignViewToObject(target.transform);
                GameObject.DestroyImmediate(target);
            }
        }
        public void AddFavorite(Vector2 s) {
            if (EasyOpenWorldEditor.favoritedTiles.Contains(s) == false)
                EasyOpenWorldEditor.favoritedTiles.Add(s);
            SaveFavorites();
        }
        public void RemoveFavorite(Vector2 s) {
            if (EasyOpenWorldEditor.favoritedTiles.Contains(s))
                EasyOpenWorldEditor.favoritedTiles.Remove(s);
            SaveFavorites();
        }
        public void ClearFavorites() {
            EasyOpenWorldEditor.favoritedTiles.Clear();
            SaveFavorites();
        }
        void SaveFavorites() {
            string s = "";
            foreach (Vector2 v in EasyOpenWorldEditor.favoritedTiles) s += (int)v.x + "_" + (int)v.y + "_" + ",";
            PlayerPrefs.SetString("Favorites", s);
        }
        public void LoadFavorites() {
            if (PlayerPrefs.HasKey("Favorites")) {
                foreach (string f in PlayerPrefs.GetString("Favorites").Split(',')) {
                    if (f != null && f != "") {
                        int x = System.Convert.ToInt32(f.Split('_')[0]);
                        int y = System.Convert.ToInt32(f.Split('_')[1]);
                        if (EasyOpenWorldEditor.favoritedTiles.Contains(new Vector2(x, y)) == false)
                            EasyOpenWorldEditor.favoritedTiles.Add(new Vector2(x, y));
                    }

                }
            }
        }


        void OnDrawGizmos() { //  draw tiles in scene view
            for (int x = 0; x < worldSize; x++) {
                for (int y = 0; y < worldSize; y++) {
                    Gizmos.DrawWireCube(new Vector3(worldOffset.x+ x * tileSize + tileSize * 0.5f, 0, worldOffset.y+ y * tileSize + tileSize * 0.5f), new Vector3(tileSize, 0.1f, tileSize));
                    Handles.Label(new Vector3(worldOffset.x + x * tileSize + tileSize * 0.5f, 10, worldOffset.y + y * tileSize + tileSize * 0.5f), "" + x + "," + y);

                }
            }
        }
#endif
    }

}
