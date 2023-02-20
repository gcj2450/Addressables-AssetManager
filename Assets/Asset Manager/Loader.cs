//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Xml;
//using UnityEditor;
////using UnityEditor.SceneManagement;
//using UnityEngine;

//public class Loader : MonoBehaviour
//{
//    public static Loader Instance;

//    // Start is called before the first frame update
//    void Start()
//    {
//        Instance = this;
//    }

//    // Update is called once per frame
//    void Update()
//    {

//    }

//    public void StartLoader()
//    {
//        Debug.Log("Starting loader...");
//        //add loading animation effects here
//    }

//    public void StopLoader()
//    {
//        Debug.Log("Stop loader...");
//        //stop loading animation effects here
//    }

//    //public static void Execute(UnityEditor.BuildTarget target)
//    //{
//    //    string assetPath = SceneAssetProcesser.GetPlatformPath(target);

//    //    string exportPath = assetPath + "Scene/";
//    //    if (Directory.Exists(exportPath) == false)
//    //        Directory.CreateDirectory(exportPath);

//    //    string currentScene = EditorApplication.currentScene;
//    //    string currentSceneName = currentScene.Substring(currentScene.LastIndexOf('/') + , currentScene.LastIndexOf('.') - currentScene.LastIndexOf('/') - );
//    //    string fileName = exportPath + currentSceneName + ".unity3d";
//    //    BuildPipeline.BuildStreamedSceneAssetBundle(new string[] { EditorApplication.currentScene }, fileName, target);
//    //    // 另外一种方式
//    //    // BuildPipeline.BuildPlayer(new string[1] { EditorApplication.currentScene }, fileName, target, BuildOptions.BuildAdditionalStreamedScenes);
//    //}

//    //private IEnumerator LoadLevelCoroutine()
//    //{
//    //    string url = "ftp://127.0.0.1/TestScene.unity3d";
//    //    int verNum = ;

//    //    WWW wwwForScene = WWW.LoadFromCacheOrDownload(url, verNum);
//    //    while (wwwForScene.isDone == false)
//    //        yield return null;

//    //    AssetBundle bundle = wwwForScene.assetBundle;
//    //    yield return Application.LoadLevelAsync("TestScene");
//    //    wwwForScene.assetBundle.Unload(false);
//    //}

//    public static void ExportXML(string savePath)
//    {
//        // 所有的动态加载的物体都挂在ActiveObjectRoot下面
//        GameObject parent = GameObject.Find("ActiveObjectRoot");
//        if (parent == null)
//        {
//            Debug.LogError("No ActiveObjectRoot Node!");
//            return;
//        }

//        XmlDocument XmlDoc = new XmlDocument();
//        XmlElement XmlRoot = XmlDoc.CreateElement("Root");
//        XmlRoot.SetAttribute("level", EditorSceneManager.GetActiveScene().path);
//        XmlDoc.AppendChild(XmlRoot);

//        foreach (Transform tranGroup in parent.transform)
//        {
//            XmlElement xmlGroupNode = XmlDoc.CreateElement("Group");
//            XmlRoot.AppendChild(xmlGroupNode);

//            CreateTransformNode(XmlDoc, xmlGroupNode, tranGroup);

//            foreach (Transform tranNode in tranGroup.transform)
//            {
//                XmlElement xmlNode = XmlDoc.CreateElement("Node");
//                xmlGroupNode.AppendChild(xmlNode);

//                CreateTransformNode(XmlDoc, xmlNode, tranNode);
//                CreateMeshNode(XmlDoc, xmlNode, tranNode);
//            }
//        }

//        string path = savePath + "Scene/";
//        if (Directory.Exists(path) == false)
//            Directory.CreateDirectory(path);
//        string levelPath = EditorSceneManager.GetActiveScene().path;//过时代码： EditorApplication.currentScene;

//        //string levelName = levelPath.Substring(levelPath.LastIndexOf('/') + , levelPath.LastIndexOf('.') - levelPath.LastIndexOf('/') - );
//        string levelName = levelPath.Substring(levelPath.LastIndexOf('/') + levelPath.LastIndexOf('.') - levelPath.LastIndexOf('/'));
//        XmlDoc.Save(path + "Xml" + levelName + ".xml");
//        XmlDoc = null;
//    }

//    private static void CreateTransformNode(XmlDocument XmlDoc, XmlElement xmlNode, Transform tran)
//    {
//        if (XmlDoc == null || xmlNode == null || tran == null)
//            return;

//        XmlElement xmlProp = XmlDoc.CreateElement("Transform");
//        xmlNode.AppendChild(xmlProp);

//        xmlNode.SetAttribute("name", tran.name);
//        xmlProp.SetAttribute("posX", tran.position.x.ToString());
//        xmlProp.SetAttribute("posY", tran.position.y.ToString());
//        xmlProp.SetAttribute("posZ", tran.position.z.ToString());
//        xmlProp.SetAttribute("rotX", tran.eulerAngles.x.ToString());
//        xmlProp.SetAttribute("rotY", tran.eulerAngles.y.ToString());
//        xmlProp.SetAttribute("rotZ", tran.eulerAngles.z.ToString());
//        xmlProp.SetAttribute("scaleX", tran.localScale.x.ToString());
//        xmlProp.SetAttribute("scaleY", tran.localScale.y.ToString());
//        xmlProp.SetAttribute("scaleZ", tran.localScale.z.ToString());
//    }

//    private static void CreateMeshNode(XmlDocument XmlDoc, XmlElement xmlNode, Transform tran)
//    {
//        if (XmlDoc == null || xmlNode == null || tran == null)
//            return;

//        XmlElement xmlProp = XmlDoc.CreateElement("MeshRenderer");
//        xmlNode.AppendChild(xmlProp);

//        foreach (MeshRenderer mr in tran.gameObject.GetComponentsInChildren<MeshRenderer>(true))
//        {
//            if (mr.material != null)
//            {
//                XmlElement xmlMesh = XmlDoc.CreateElement("Mesh");
//                xmlProp.AppendChild(xmlMesh);

//                // 记录Mesh名字和Shader
//                xmlMesh.SetAttribute("Mesh", mr.name);
//                xmlMesh.SetAttribute("Shader", mr.material.shader.name);

//                // 记录主颜色
//                XmlElement xmlColor = XmlDoc.CreateElement("Color");
//                xmlMesh.AppendChild(xmlColor);
//                bool hasColor = mr.material.HasProperty("_Color");
//                xmlColor.SetAttribute("hasColor", hasColor.ToString());
//                if (hasColor)
//                {
//                    xmlColor.SetAttribute("r", mr.material.color.r.ToString());
//                    xmlColor.SetAttribute("g", mr.material.color.g.ToString());
//                    xmlColor.SetAttribute("b", mr.material.color.b.ToString());
//                    xmlColor.SetAttribute("a", mr.material.color.a.ToString());
//                }

//                // 光照贴图信息
//                XmlElement xmlLightmap = XmlDoc.CreateElement("Lightmap");
//                xmlMesh.AppendChild(xmlLightmap);
//                // 是否为static，static的对象才有lightmap信息
//                xmlLightmap.SetAttribute("IsStatic", mr.gameObject.isStatic.ToString());
//                xmlLightmap.SetAttribute("LightmapIndex", mr.lightmapIndex.ToString());
//                xmlLightmap.SetAttribute("OffsetX", mr.lightmapScaleOffset.x.ToString());
//                xmlLightmap.SetAttribute("OffsetY", mr.lightmapScaleOffset.y.ToString());
//                xmlLightmap.SetAttribute("OffsetZ", mr.lightmapScaleOffset.z.ToString());
//                xmlLightmap.SetAttribute("OffsetW", mr.lightmapScaleOffset.w.ToString());
//            }
//        }
//    }

//    //private void ParseChildNode(XmlElement xmlGroup, XmlElement xmlChild)
//    //{
//    //    XmlSceneGameobjectProp newChild = new XmlSceneGameobjectProp();
//    //    newChild.group = xmlGroup.GetAttribute("name");
//    //    newChild.name = xmlChild.GetAttribute("name");
//    //    // 注册资源名字
//    //    if (lstRes.Contains(newChild.name) == false)
//    //    {
//    //        lstRes.Add(newChild.name);
//    //    }

//    //    // Tranform节点
//    //    XmlNode xmlTransform = xmlChild.SelectSingleNode("Transform");
//    //    // MeshRenderer节点
//    //    XmlNode xmlMeshRenderer = xmlChild.SelectSingleNode("MeshRenderer");

//    //    if (xmlTransform != null && xmlTransform is XmlElement)
//    //    {
//    //        CXmlRead goReader = new CXmlRead(xmlTransform as XmlElement);
//    //        newChild.posX = goReader.Float("posX", 0f);
//    //        newChild.posY = goReader.Float("posY", 0f);
//    //        newChild.posZ = goReader.Float("posZ", 0f);
//    //        newChild.rotX = goReader.Float("rotX", 0f);
//    //        newChild.rotY = goReader.Float("rotY", 0f);
//    //        newChild.rotZ = goReader.Float("rotZ", 0f);
//    //        newChild.scaleX = goReader.Float("scaleX", 1f);
//    //        newChild.scaleY = goReader.Float("scaleY", 1f);
//    //        newChild.scaleZ = goReader.Float("scaleZ", 1f);
//    //    }

//    //    if (xmlMeshRenderer != null && xmlMeshRenderer is XmlElement)
//    //    {
//    //        foreach (XmlNode node in xmlMeshRenderer.ChildNodes)
//    //        {
//    //            if ((node is XmlElement) == false)
//    //                continue;

//    //            XmlSceneGameobjectProp.MeshInfo mesh = new XmlSceneGameobjectProp.MeshInfo();
//    //            mesh.name = (node as XmlElement).GetAttribute("Mesh");
//    //            mesh.shader = (node as XmlElement).GetAttribute("Shader");
//    //            XmlNode xmlLightmap = node.SelectSingleNode("Lightmap");
//    //            if (xmlLightmap != null && xmlLightmap is XmlElement)
//    //            {
//    //                CXmlRead reader = new CXmlRead(xmlLightmap as XmlElement);
//    //                mesh.isStatic = reader.Bool("IsStatic", true);
//    //                mesh.lightmapIndex = reader.Int("LightmapIndex", -);
//    //                mesh.lightmapTilingOffset = new Vector4(reader.Float("OffsetX", 0f), reader.Float("OffsetY", 0f), reader.Float("OffsetZ", 0f), reader.Float("OffsetW", 0f));
//    //            }
//    //            XmlNode xmlColor = node.SelectSingleNode("Color");
//    //            if (xmlColor != null && xmlColor is XmlElement)
//    //            {
//    //                CXmlRead reader = new CXmlRead(xmlColor as XmlElement);
//    //                mesh.hasColor = reader.Bool("hasColor", false);
//    //                mesh.color = new Vector4(reader.Float("r", 0f), reader.Float("g", 0f), reader.Float("b", 0f), reader.Float("a", 0f));
//    //            }
//    //            newChild.LstMesh.Add(mesh);
//    //        }
//    //    }

//    //    lstGameObjectProp.Add(newChild);
//    //}

//    //public void InstantiateObject()
//    //{
//    //    // 实例化
//    //    GameObject goIns = GameObject.Instantiate(asset) as GameObject;
//    //    goIns.name = goProp.name;

//    //    // 设置父节点
//    //    GameObject goGroup = null;
//    //    dicGroupGameobject.TryGetValue(goProp.group, out goGroup);
//    //    if (goGroup != null)
//    //        goIns.transform.parent = goGroup.transform;
//    //    else
//    //        goIns.transform.parent = goRoot.transform;

//    //    // 设置Transform
//    //    goIns.transform.position = new Vector3(goProp.posX, goProp.posY, goProp.posZ);
//    //    goIns.transform.eulerAngles = new Vector3(goProp.rotX, goProp.rotY, goProp.rotZ);
//    //    goIns.transform.localScale = new Vector3(goProp.scaleX, goProp.scaleY, goProp.scaleZ);

//    //    // 设置Shader、Lightmap
//    //    int index = ;
//    //    int meshCount = goProp.LstMesh.Count;
//    //    foreach (MeshRenderer mr in goIns.gameObject.GetComponentsInChildren<MeshRenderer>(true))
//    //    {
//    //        if (mr.sharedMaterial != null)
//    //        {
//    //            if (index < meshCount)
//    //            {
//    //                XmlSceneGameobjectProp.MeshInfo meshProp = goProp.LstMesh[index];
//    //                mr.sharedMaterial.shader = Shader.Find(meshProp.shader);
//    //                if (meshProp.hasColor)
//    //                    mr.sharedMaterial.color = meshProp.color;
//    //                bool isStatic = meshProp.isStatic;
//    //                mr.gameObject.isStatic = isStatic;
//    //                if (isStatic)
//    //                {
//    //                    mr.lightmapIndex = meshProp.lightmapIndex;
//    //                    mr.lightmapTilingOffset = meshProp.lightmapTilingOffset;
//    //                }
//    //            }
//    //            index++;
//    //        }
//    //    }
//    //}
//}

//// 和资源有关的管理器都将继承自此类
//public class IAssetManager
//{
//    // 管理器所管理的资源列表，实际上是引用列表
//    protected List<string> lstRefAsset = new List<string>();

//    // 增加引用的资源
//    public virtual void RefAsset(string name)
//    { }

//    // 以一定的策略卸载资源
//    public virtual bool UnloadAsset()
//    { return true; }

//}

//public class XmlSceneGameobjectProp
//{
//    // Mesh信息
//    public class MeshInfo
//    {
//        public string name;
//        public string shader;

//        public bool hasColor = false;
//        public Vector4 color;

//        public bool isStatic = true;
//        public int lightmapIndex;
//        public Vector4 lightmapTilingOffset;
//    }

//    public string name;
//    public string group;
//    // Transform信息
//    public float posX, posY, posZ;
//    public float rotX, rotY, rotZ;
//    public float scaleX, scaleY, scaleZ;
//    // Mesh列表，一个模型可以包含多个MeshRenderer
//    public List<MeshInfo> LstMesh = new List<MeshInfo>();
//}
