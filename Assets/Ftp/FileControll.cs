using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public static class FileControll
{

    /// <summary>
    /// 创建文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool CreateFile(string filePath)
    {
        if (!FileExist(filePath))
        {
            File.Create(filePath);
            return FileExist(filePath);
        }
        return false;
    }

    /// <summary>
    /// 创建目录
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public static bool CreateFolder(string folderPath)
    {
        if (!FolderExist(folderPath))
        {
            DirectoryInfo info = Directory.CreateDirectory(folderPath);
            return info.Exists;
        }
        return false;
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="filePath"></param>
    public static void DeleteFile(string filePath)
    {
        if (FileExist(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// 删除目录
    /// </summary>
    /// <param name="folderPath"></param>
    public static void DeleteFolder(string folderPath)
    {
        DeleteFolder(folderPath, true);
    }

    /// <summary>
    /// 删除目录
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="recursive">是否递归删除</param>
    public static void DeleteFolder(string folderPath, bool recursive)
    {
        if (FolderExist(folderPath))
        {
            Directory.Delete(folderPath, recursive);
        }
    }

    /// <summary>
    /// 目录是否存在
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public static bool FolderExist(string folderPath)
    {
        return Directory.Exists(folderPath);
    }

    /// <summary>
    /// 文件是否存在
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool FileExist(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// 获取文件的目录
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetFileFolder(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Directory.ToString();
    }

    /// <summary>
    /// 获取子目录
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    public static List<string> GetSubFolders(string folderPath)
    {
        List<string> result = new List<string>();
        GetSubFolders(folderPath, ref result);
        return result;
    }

    /// <summary>
    /// 获取子目录
    /// </summary>
    /// <param name="path"></param>
    /// <param name="result"></param>
    static void GetSubFolders(string path, ref List<string> result)
    {
        result.Add(path);
        if (Directory.Exists(path))
        {
            foreach (string sub in Directory.GetDirectories(path))
            {
                GetSubFolders(sub + "/", ref result);
            }
        }
    }

    /// <summary>
    /// 获取目录下的所有文件
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="recursive">是否递归获取</param>
    /// <param name="endWith"></param>
    /// <returns></returns>
    public static List<string> GetFolderFiles(string folderPath, bool recursive, string endWith = "")
    {
        List<string> fileList;
        if (recursive)
        {
            fileList = new List<string>();
            List<string> subFolderList = GetSubFolders(folderPath);
            foreach (var subFolder in subFolderList)
            {
                List<string> subFileList = GetFolderFiles(subFolder, endWith);
                fileList.AddRange(subFileList);
            }
        }
        else
        {
            fileList = GetFolderFiles(folderPath, endWith);
        }
        return fileList;
    }

    /// <summary>
    /// 获取目录下的所有文件
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="endWith"></param>
    /// <returns></returns>
    public static List<string> GetFolderFiles(string folderPath, string endWith = "")
    {
        List<string> result = new List<string>();
        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                if (!string.IsNullOrEmpty(endWith) && !file.ToLower().EndsWith(endWith.ToLower())) continue;
                result.Add(file);
            }
        }
        return result;
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="bytes"></param>
    public static void WriteFile(string path, byte[] bytes)
    {
        WriteFile(path, bytes, FileMode.Create);
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="bytes"></param>
    /// <param name="fileMode"></param>
    public static void WriteFile(string path, byte[] bytes, FileMode fileMode)
    {
#if UNITY_EDITOR || (!UNITY_WINRT)
        try
        {
            FileStream fs = new FileStream(path, fileMode);
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError("文件写入失败" + path + ":" + ex.Message);
        }
#endif
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="append"></param>
    /// <param name="infos"></param>
    public static void WriteFile(string path, bool append, List<string> infos)
    {
        try
        {
            StreamWriter sw = new StreamWriter(path, append);
            if (infos != null)
            {
                foreach (string info in infos)
                {
                    sw.WriteLine(info);
                }
            }
            sw.Close();
            sw.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogError("文件写入失败" + path + ":" + ex.Message);
        }
    }

    /// <summary>
    /// 写入Txt文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    /// <param name="encoding"></param>
    public static void WriteTxtFile(string path, string content, Encoding encoding)
    {
        File.WriteAllText(path, content, encoding);
    }

    /// <summary>
    /// 写入Txt文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="content"></param>
    public static void WriteTxtFile(string path, string content)
    {
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// 复制文件到
    /// </summary>
    /// <param name="path"></param>
    /// <param name="toPath"></param>
    /// <param name="overwrite">是否覆盖</param>
    public static void CopyFile(string path, string toPath, bool overwrite)
    {
        try
        {
            File.Copy(path, toPath, overwrite);
        }
        catch (Exception ex)
        {
            Debug.LogError("拷贝文件失败：" + ex.Message);
        }
    }

    /// <summary>
    /// 复制目录到
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public static void CopyFolder(string from, string to)
    {
        if (!Directory.Exists(to))
            Directory.CreateDirectory(to);

        // 子文件夹
        foreach (string sub in Directory.GetDirectories(from))
            CopyFolder(sub + "/", to + Path.GetFileName(sub) + "/");

        // 文件
        foreach (string file in Directory.GetFiles(from))
        {
            try
            {
                File.Copy(file, to + Path.GetFileName(file), true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("拷贝失败:" + ex.Message);
            }
        }
    }

    /// <summary>
    /// 读取文件
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static byte[] ReadFile(string path)
    {
#if UNITY_EDITOR || (!UNITY_WINRT)
        if (!File.Exists(path))
            return null;
        FileStream fs = new FileStream(path, FileMode.Open);
        long size = fs.Length;
        byte[] array = new byte[size];
        //将文件读到byte数组中
        fs.Read(array, 0, array.Length);
        fs.Close();
        return array;
#else
		return null;
#endif
    }

    /// <summary>
    /// 读取Txt数据
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string ReadTxtFile(string path)
    {
        if (!File.Exists(path))
            return null;
        return File.ReadAllText(path);
    }

    /// <summary>
    /// 读取Txt文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static string ReadTxtFile(string path, System.Text.Encoding encoding)
    {
        if (!File.Exists(path))
            return null;
        return File.ReadAllText(path, encoding);
    }

    /// <summary>
    /// 读取Txt行
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static List<string> ReadTxtFileLine(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            using (StreamReader sr = new StreamReader(path))
            {
                List<string> dataList = new List<string>();
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    dataList.Add(line);
                }
                return dataList;
            }
        }
        catch (Exception e)
        {
            Debug.Log("文件未能读取" + e.Message);
            return null;
        }
    }

    /// <summary>
    /// 检测并矫正CSV格式
    /// </summary>
    /// <param name="path">csv文件路径</param>
    /// <param name="fileEncoding"></param>
    /// <returns>是否矫正</returns>
    public static bool CheckAndCollectCSVFormat(string path, Encoding fileEncoding = null)
    {
        //if (fileEncoding == null)
        //{
        //    TextUtil.EncodingType encodingType = TextUtil.GuessFileEncoding(path);
        //    if (encodingType == TextUtil.EncodingType.Unknown)
        //    {
        //        fileEncoding = TextUtil.ANSI_CHINESE; //默认用ANSI编码
        //    }
        //    else fileEncoding = TextUtil.GetEncoding(encodingType);
        //}
        //if (!fileEncoding.CodePage.Equals(Encoding.UTF8.CodePage))
        //{
        //    var content = File.ReadAllText(path, fileEncoding);
        //    File.WriteAllText(path, content, Encoding.UTF8);
        //    return true;
        //}

        return false;
    }

    /// <summary>
    /// 合并路径
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static string CombineFilePath(string folderPath, string fileName)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return "";
        }
        DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
        if (!directoryInfo.Exists)
        {
            Debug.LogError("目录不存在：" + folderPath);
            return "";
        }

        return directoryInfo.FullName + "\\" + fileName;
    }

    /// <summary>
    /// 使路径规范化(都变成这样的格式：Assets/Game/Source)
    /// 如Assets\Game\Source变成Assets/Game/Source
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string MakePathPerfect(string path)
    {
        return path.Replace("\\", "/");
    }

    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    public static void MoveFile(string @from, string to)
    {
        @from = from.Replace('\\', '/');
        @to = to.Replace('\\', '/');
        if (Directory.Exists(to))
        {
            FileControll.CopyFolder(@from, to);
            FileControll.DeleteFolder(@from);
            Debug.Log($"覆盖文件夹:{@from} ===> {to}");
        }
        else if (File.Exists(to))
        {
            FileControll.CopyFile(@from, to, true);
            FileControll.DeleteFile(@from);
            Debug.Log($"覆盖文件:{@from} ===> {to}");
        }
        else
        {

            string fileLastPath = to;
            if (to.Contains("."))
            {
                //fileLastPath = to.GetFileLastPath();
            }
            if (!Directory.Exists(fileLastPath))
            {
                Directory.CreateDirectory(fileLastPath);
            }

            File.Move(@from, to);
            Debug.Log($"移动文件:{@from} ===> {to}");
        }
    }
}