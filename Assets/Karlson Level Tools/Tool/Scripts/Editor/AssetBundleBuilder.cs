using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.IO;
using System.Text;

public class AssetBundleBuilder : Editor
{
    [MenuItem("Assets/Build to Level Asset Bundle")]
    static void BuildLevelAssetBundle()
    {
        if (!Directory.Exists("Assets/Bundles")) Directory.CreateDirectory("Assets/Bundles");

        //Get All CutomLevelObject guids
        string[] guids = AssetDatabase.FindAssets("t: CustomLevelObject");

        for (int i = 0; i < guids.Length; i++)
        {
            //Get CustomLevelObject
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

            AssetDatabase.ImportAsset(assetPath);
            CustomLevelObject level = AssetDatabase.LoadAssetAtPath<CustomLevelObject>(assetPath);

            if (level == null)
            {
                Debug.LogWarning("Level asset not found");
                continue;
            }
            if (level.LevelScene == null)
            {
                Debug.LogWarning("No level assigned to " + assetPath);
                continue;
            }

            //Create AssetBundle settings
            AssetBundleBuild[] build = new AssetBundleBuild[1];
            build[0].assetBundleName = level.LevelName;
            build[0].assetNames = new string[] { AssetDatabase.GetAssetPath(level.LevelScene) };

            //Build assetbundle
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles("Assets/Bundles/", build, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
            string bundle = manifest.GetAllAssetBundles()[0];

            Debug.Log(bundle);

            //Add extra data to AssetBundle file
            AddMetadata("Assets/Bundles/" + bundle, level.LevelName, level.Author, level.Thumbnail);
        }

        Debug.Log("All Asset bundles build succesfully!");
    }

    private static void AddMetadata(string bundle, string name, string author, Texture2D thumbnail)
    {
        if (string.IsNullOrEmpty(author)) author = "unknown";
        if (thumbnail == null) thumbnail = new Texture2D(1, 1);

        #region Thumbnail
        Texture2D scaledThumbnail = Resize(thumbnail, 960, 540);
        LevelMetadata levelData = new LevelMetadata(name, author, scaledThumbnail);
        #endregion

        #region Components
        LevelScripts levelScripts = new LevelScripts();

        //var identifiers = FindObjectsOfType<MonoBehaviour>().OfType<IObjectIdentifier>();
        //foreach (IObjectIdentifier identifier in identifiers)
        //{
        //    levelScripts.AddGameObject(identifier.component.gameObject);
        //}
        #endregion

        using (MemoryStream memStream = new MemoryStream())
        using (BinaryWriter memWriter = new BinaryWriter(memStream))
        {
            //Convert metadata
            memWriter.Write(levelData.LevelName);
            memWriter.Write(levelData.Author);
            memWriter.Write(levelData.Format);
            memWriter.Write(levelData.Thumbnail);

            //Compress metadata
            byte[] MetaData = SevenZip.Compression.LZMA.SevenZipHelper.Compress(memStream.ToArray());
            memWriter.Close();
            memStream.Close();

            //Compress component info
            string SerializedJson = JsonUtility.ToJson(levelScripts);
            byte[] EncodedJson = Encoding.UTF8.GetBytes(SerializedJson);
            byte[] LevelData = SevenZip.Compression.LZMA.SevenZipHelper.Compress(EncodedJson);

            string targetFile = bundle + ".klf";
            if (File.Exists(targetFile)) File.Delete(targetFile);

            using (Stream stream = new FileStream(targetFile, FileMode.Create))
            using (BinaryWriter fileWriter = new BinaryWriter(stream))
            {
                //Write header
                fileWriter.Write((byte)75);
                fileWriter.Write((byte)97);
                fileWriter.Write((byte)76);
                fileWriter.Write((byte)70);

                fileWriter.Write(MetaData.Length);
                fileWriter.Write(MetaData);

                fileWriter.Write(LevelData.Length);
                fileWriter.Write(LevelData);


                using (FileStream assetBundle = new FileStream(bundle, FileMode.Open))
                {
                    assetBundle.CopyTo(stream);
                }
            }
        }
    }

    /// <summary>
    /// Overly complicated way to resize a Texture2D.
    /// Don't want to make users have to check boxes
    /// - Jor02
    /// </summary>
    private static Texture2D Resize(Texture2D orig, int width, int height)
    {
        //Create render texture from original texture
        RenderTexture renderTex = RenderTexture.GetTemporary(orig.width, orig.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(orig, renderTex);

        //Set render texture to be active
        RenderTexture prevRenderTex = RenderTexture.active;
        RenderTexture.active = renderTex;

        //Create readable texture
        Texture2D readableTex = new Texture2D(orig.width, orig.height, TextureFormat.ARGB32, false);
        //Apply render texture pixels to readable texture
        readableTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableTex.Apply();

        //Set previously active render texture back as active render texture
        RenderTexture.active = prevRenderTex;
        RenderTexture.ReleaseTemporary(renderTex);

        //Create texture with correct TextureFormat
        Texture2D scaledThumbnail = new Texture2D(readableTex.width, readableTex.height, TextureFormat.ARGB32, false);
        
        //Set pixels to readable texture
        scaledThumbnail.SetPixels(readableTex.GetPixels());

        //Finally resize our texture
        scaledThumbnail.Reinitialize(width, height, TextureFormat.ARGB32, false);
        scaledThumbnail.Apply();

        //Compress it for good measure
        EditorUtility.CompressTexture(scaledThumbnail, orig.format, UnityEditor.TextureCompressionQuality.Best);

        return readableTex;
    }

    [Serializable]
    struct LevelMetadata
    {
        public string LevelName;
        public string Author;
        public byte[] Thumbnail;
        public byte Format;

        public LevelMetadata(string levelName, string author, Texture2D thumbnail)
        {
            LevelName = levelName;
            Author = author;
            Thumbnail = thumbnail.EncodeToPNG();
            Format = (byte)thumbnail.format;
        }
    }
}
