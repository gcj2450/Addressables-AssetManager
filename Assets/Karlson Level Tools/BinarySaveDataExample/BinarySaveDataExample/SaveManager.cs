using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using Random = UnityEngine.Random;

public class SaveManager : MonoBehaviour
{
    static string _saveFolderPath;

    public const int CurrentSaveVersion = 1;

    void OnEnable()
    {
        _saveFolderPath = $"{Application.persistentDataPath}/Saves";

        if (!Directory.Exists(_saveFolderPath))
            Directory.CreateDirectory(_saveFolderPath);
    }

    /// <summary>
    /// 保存二进制文件
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="saveData"></param>
    public static void SaveBinary(string fileName, SaveData saveData)
    {
        var fileFullPath = $"{_saveFolderPath}/{fileName}";
        using (BinaryWriter writer = new BinaryWriter(File.Open(fileFullPath, FileMode.OpenOrCreate)))
        {
            writer.Write(saveData.Version);
            writer.Write(saveData.EntityUuids.Length);
            for (int i = 0; i < saveData.EntityUuids.Length; ++i)
            {
                writer.Write(saveData.EntityUuids[i]);
                var position = saveData.EntityPositions[i];
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);
            }
            writer.Write(JsonUtility.ToJson(saveData.RandomState));
        }

        Debug.Log($"Saved Binary to {fileFullPath}");
    }

    /// <summary>
    /// 读取二进制文件
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="saveData"></param>
    public static void LoadBinary(string fileName, SaveData saveData)
    {
        var fileFullPath = $"{_saveFolderPath}/{fileName}";
        using (BinaryReader reader = new BinaryReader(File.Open(fileFullPath, FileMode.Open)))
        {
            saveData.Version = reader.ReadInt32();
            var entityCount = reader.ReadInt32();
            saveData.EntityUuids = new string[entityCount];
            saveData.EntityPositions = new Vector3[entityCount];
            for (int i = 0; i < entityCount; ++i)
            {
                saveData.EntityUuids[i] = reader.ReadString();
                saveData.EntityPositions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            saveData.RandomState = JsonUtility.FromJson<Random.State>(reader.ReadString());
        }

        Debug.Log($"Loaded Binary from {fileFullPath}");
    }

    /// <summary>
    /// 保存Json
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="saveData"></param>
    public static void SaveJson(string fileName, SaveData saveData)
    {
        var fileFullPath = $"{_saveFolderPath}/{fileName}";
        var jsonString = JsonUtility.ToJson(saveData);
        File.WriteAllText(fileFullPath, jsonString);

        Debug.Log($"Saved JSON to {fileFullPath}");
    }

    /// <summary>
    /// 加载Json
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="saveData"></param>
    public static void LoadJson(string fileName, SaveData saveData)
    {
        var fileFullPath = $"{_saveFolderPath}/{fileName}";
        var jsonString = File.ReadAllText(fileFullPath);
        JsonUtility.FromJsonOverwrite(jsonString, saveData);

        Debug.Log($"Loaded JSON from {fileFullPath}");
    }

    /// <summary>
    /// 使用GZip压缩
    /// </summary>
    /// <param name="sourceFile"></param>
    /// <param name="compressedFile"></param>
    public static void Compress(string sourceFile, string compressedFile)
    {
        // 使用创建线程
        using (FileStream sourceStream = new FileStream(sourceFile, FileMode.OpenOrCreate))
        {
            using (FileStream targetStream = File.Create(compressedFile))
            {
                using (GZipStream compressionStream = new GZipStream(targetStream, CompressionMode.Compress))
                {
                    sourceStream.CopyTo(compressionStream);
                    Debug.Log($"文件 {sourceFile} 已压缩。原始尺寸: {sourceStream.Length}  压缩尺寸： {targetStream.Length.ToString()}.");
                }
            }
        }
    }

    /// <summary>
    /// 使用GZip解压
    /// </summary>
    /// <param name="sourceFile"></param>
    /// <param name="compressedFile"></param>
    public static void Decompress(string compressedFile, string targetFile)
    {
        using (FileStream sourceStream = new FileStream(compressedFile, FileMode.OpenOrCreate))
        {
            using (FileStream targetStream = File.Create(targetFile))
            {
                using (GZipStream decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(targetStream);
                    Debug.Log($"解压文件: {targetFile}");
                }
            }
        }

    }
}