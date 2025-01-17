﻿//using UnityEngine;
//using UnityEditor;
//using System.IO;
//using System.Xml;
//using System.Collections;
//using System.Collections.Generic;
//using System.Security.Cryptography;

///// <summary>
///// 2 为每个assetbund生成MD5码，用于检查资源是否有修改 
///// </summary>
//public class CreateMD5List
//{
//    public static void Execute(UnityEditor.BuildTarget target)
//    {
//        string platform = AssetBundleController.GetPlatformName(target);
//        Execute(platform);
//        AssetDatabase.Refresh();
//    }

//    public static void Execute(string platform)
//    {
//        Dictionary<string, string> DicFileMD5 = new Dictionary<string, string>();
//        MD5CryptoServiceProvider md5Generator = new MD5CryptoServiceProvider();

//        string dir = System.IO.Path.Combine(Application.dataPath, "AssetBundle/" + platform);
//        foreach (string filePath in Directory.GetFiles(dir))
//        {
//            if (filePath.Contains(".meta") || filePath.Contains("VersionMD5") || filePath.Contains(".xml"))
//                continue;

//            FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
//            byte[] hash = md5Generator.ComputeHash(file);
//            string strMD5 = System.BitConverter.ToString(hash);
//            file.Close();

//            string key = filePath.Substring(dir.Length + 1, filePath.Length - dir.Length - 1);

//            if (DicFileMD5.ContainsKey(key) == false)
//                DicFileMD5.Add(key, strMD5);
//            else
//                Debug.LogWarning("<Two File has the same name> name = " + filePath);
//        }

//        string savePath = System.IO.Path.Combine(Application.dataPath, "AssetBundle/") + platform + "/VersionNum";
//        if (Directory.Exists(savePath) == false)
//            Directory.CreateDirectory(savePath);

//        // 删除前一版的old数据
//        if (File.Exists(savePath + "/VersionMD5-old.xml"))
//        {
//            System.IO.File.Delete(savePath + "/VersionMD5-old.xml");
//        }

//        // 如果之前的版本存在，则将其名字改为VersionMD5-old.xml
//        if (File.Exists(savePath + "/VersionMD5.xml"))
//        {
//            System.IO.File.Move(savePath + "/VersionMD5.xml", savePath + "/VersionMD5-old.xml");
//        }

//        XmlDocument XmlDoc = new XmlDocument();
//        XmlElement XmlRoot = XmlDoc.CreateElement("Files");
//        XmlDoc.AppendChild(XmlRoot);
//        foreach (KeyValuePair<string, string> pair in DicFileMD5)
//        {
//            XmlElement xmlElem = XmlDoc.CreateElement("File");
//            XmlRoot.AppendChild(xmlElem);

//            xmlElem.SetAttribute("FileName", pair.Key);
//            xmlElem.SetAttribute("MD5", pair.Value);
//        }

//        // 读取旧版本的MD5
//        Dictionary<string, string> dicOldMD5 = ReadMD5File(savePath + "/VersionMD5-old.xml");
//        // VersionMD5-old中有，而VersionMD5中没有的信息，手动添加到VersionMD5
//        foreach (KeyValuePair<string, string> pair in dicOldMD5)
//        {
//            if (DicFileMD5.ContainsKey(pair.Key) == false)
//                DicFileMD5.Add(pair.Key, pair.Value);
//        }

//        XmlDoc.Save(savePath + "/VersionMD5.xml");
//        XmlDoc = null;
//    }

//    static Dictionary<string, string> ReadMD5File(string fileName)
//    {
//        Dictionary<string, string> DicMD5 = new Dictionary<string, string>();

//        // 如果文件不存在，则直接返回
//        if (System.IO.File.Exists(fileName) == false)
//            return DicMD5;

//        XmlDocument XmlDoc = new XmlDocument();
//        XmlDoc.Load(fileName);
//        XmlElement XmlRoot = XmlDoc.DocumentElement;

//        foreach (XmlNode node in XmlRoot.ChildNodes)
//        {
//            if ((node is XmlElement) == false)
//                continue;

//            string file = (node as XmlElement).GetAttribute("FileName");
//            string md5 = (node as XmlElement).GetAttribute("MD5");

//            if (DicMD5.ContainsKey(file) == false)
//            {
//                DicMD5.Add(file, md5);
//            }
//        }

//        XmlRoot = null;
//        XmlDoc = null;

//        return DicMD5;
//    }
//}