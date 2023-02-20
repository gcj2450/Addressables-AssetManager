using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
//using Unity.Mathematics;
using UnityEngine;

namespace BaiduFtpUploadFiles
{
    public class FtpUploaderHelper
    {
        private string ftpServerIP;
        private string ftpRemotePath;
        private string ftpUserID;
        private string ftpPassword;
        public string ftpURI;
        private string ftpServerPort;
        private bool ftpMode;
        /// <summary>
        /// 连接FTP，FtpRemotePath格式：StandaloneWindows64/aaa/
        /// </summary>
        /// <param name="FtpServerIP">FTP连接地址</param>
        /// <param name="FtpRemotePath">指定FTP连接成功后的当前目录, 如果不指定即默认为根目录，指定的话需要确保存在，否则报错</param>
        /// <param name="FtpUserID">用户名</param>
        /// <param name="FtpPassword">密码</param>
        /// <param name="FtpMode">是否使用被动模式</param>
        public FtpUploaderHelper(string FtpServerIP, string FtpServerPort, string FtpRemotePath, string FtpUserID, string FtpPassword, bool FtpMode)
        {
            ftpServerIP = FtpServerIP;
            ftpRemotePath = FtpRemotePath;
            ftpUserID = FtpUserID;
            ftpPassword = FtpPassword;
            ftpServerPort = FtpServerPort;
            ftpMode = FtpMode;
            ftpURI = "ftp://" + ftpServerIP + ":" + ftpServerPort + "/";
            if (!string.IsNullOrEmpty(ftpRemotePath))
            {
                ftpURI = ftpURI + ftpRemotePath + "/";
            }
        }

        /// <summary>  
        /// 上传到当前连接的文件夹下，
        /// 如果文件存在则覆盖,使用UploadFileAsync/UploadFilePath更好
        /// </summary>   
        void UploadFile(string filename)
        {
            FileInfo fileInf = new FileInfo(filename);
            FtpWebRequest reqFTP;
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + fileInf.Name));
            reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            reqFTP.KeepAlive = false;
            reqFTP.UsePassive = ftpMode;
            reqFTP.UseBinary = true;
            reqFTP.ContentLength = fileInf.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            FileStream fs = fileInf.OpenRead();
            try
            {
                Stream strm = reqFTP.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                strm.Close();
                fs.Close();
                Debug.Log("Upload success");
            }
            catch (Exception ex)
            {
                throw new Exception("上传  Error --> " + ex.Message);
            }
        }

        //上传示例，将D:/GitHub/Addressables-AssetManager/ServerData/
        //文件夹内的文件和文件夹传到服务器的ServerData文件夹下
        //UploadFolderAsync("D:/GitHub/Addressables-AssetManager/ServerData/", "ServerData/");
        /// <summary>
        /// 上传_localFolderName文件夹内的文件及文件夹到_ftpFolderName文件夹内，
        /// 会忽略空文件夹
        /// </summary>
        /// <param name="_localFolderName">本地文件夹</param>
        /// <param name="_ftpFolderName">ftp上的文件夹，不传值或空就上传到根目录</param>
        public void UploadFolderAsync(string _localFolderName, string _ftpFolderName)
        {
            List<string> files = new List<string>();
            RecursiveFile(_localFolderName, ref files);
            Debug.Log(files.Count);
            for (int i = 0, cnt = files.Count; i < cnt; i++)
            {
                string dirName = new FileInfo(files[i]).DirectoryName;
                //Debug.Log(dirName);
                dirName = dirName.Replace("\\", "/") + "/";
                //Debug.Log(_localFolderName);
                string remoteFolderName = dirName.Replace(_localFolderName, "/");
                //Debug.Log(remoteFolderName);

                if (!_ftpFolderName.EndsWith("/"))
                    _ftpFolderName = _ftpFolderName + "/";
                Debug.Log(_ftpFolderName + remoteFolderName);
                UploadFileAsync(files[i], "", _ftpFolderName + remoteFolderName);
            }
        }

        /// <summary>
        /// 遍历指定文件夹中的文件包括子文件夹的文件
        /// </summary>
        /// <param name="result">遍历之后的结果</param>
        /// <returns></returns>
        void RecursiveFile(string filePathByForeach, ref List<string> results)
        {
            DirectoryInfo theFolder = new DirectoryInfo(filePathByForeach);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();//获取所在目录的文件夹
            FileInfo[] file = theFolder.GetFiles();//获取所在目录的文件

            foreach (FileInfo fileItem in file) //遍历文件
            {
                //Debug.Log(fileItem.FullName);
                results.Add(fileItem.FullName);
            }
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                RecursiveFile(NextFolder.FullName, ref results);
            }
        }

        public void UploadFileAsync(string _path, string _name = "", string _ftpPath = "/")
        {
            IList<object> objList = new List<object> { _path, _name, _ftpPath };
            Thread threadUpload =
                new Thread(new ParameterizedThreadStart(ThreadUploadFile));
            threadUpload.Start(objList);  //开始采用线程方式下载
        }

        /// <summary>
        /// 线程接收上传
        /// </summary>
        /// <param name="obj"></param>
        private void ThreadUploadFile(object obj)
        {
            string tmpPath;
            string tmpName;
            string tmpFtpPath;
            IList<object> objList = obj as IList<object>;
            if (objList != null && objList.Count == 3)
            {
                tmpPath = objList[0] as string;
                tmpName = objList[1] as string;
                tmpFtpPath = objList[2] as string;
                this.UploadFilePath(tmpPath, tmpName, tmpFtpPath);
            }
        }

        /// <summary>
        /// 上传到指定文件夹,指定的文件夹不存在会自动创建，文件存在则覆盖
        /// </summary>
        /// <param name="path">本地文件路径</param>
        /// <param name="name">ftp上的文件名，不传就使用原来文件名</param>
        /// <param name="FtpPath">Ftp上的文件夹，默认传到根目录</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        bool UploadFilePath(string path, string name = "", string ftpPath = "/")
        {
            FileInfo f = new FileInfo(path);
            _ = path.Replace("\\", "/");
            //不是根目录就检查并创建文件夹
            if (ftpPath != "/")
                FtpCheckDirectoryExist(ftpPath);

            //如果不传文件名，使用原来的文件名
            if (string.IsNullOrEmpty(name))
            {
                name = f.Name;
            }

            path = ftpURI + ftpPath + name; //这个路径是我要传到ftp目录下的这个目录下
            FtpWebRequest reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(path));
            reqFtp.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
            reqFtp.KeepAlive = false;
            reqFtp.UsePassive = ftpMode;
            reqFtp.UseBinary = true;
            reqFtp.Method = WebRequestMethods.Ftp.UploadFile;
            reqFtp.ContentLength = f.Length;
            int buffLength = 2048;
            byte[] buff = new byte[buffLength];
            int contentLen;
            FileStream fs = f.OpenRead();
            try
            {
                int allbye = (int)f.Length;
                int startbye = 0;
                Stream strm = reqFtp.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                    startbye += contentLen;
                    Debug.Log("上传进度：" + Math.Round((startbye / (float)allbye), 2));
                    Debug.Log("已上传:" + (int)(startbye / 1024.0f) + "KB/" +
                        "总长度:" + (int)(allbye / 1024.0f) + "KB" + " " + " 文件名:" + f.Name);
                }
                strm.Close();
                fs.Close();
                Debug.Log("上传成功!");
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("上传  Error --> " + ex.Message);
            }
        }

        //Ftp_Helper.DownLoadFile("ftp://10.27.209.219:8021/ServerData/StandaloneWindows64/1234.bundle",
        //"D:/GitHub/Addressables-AssetManager/ServerData/", "tmbh", "anquan@123");
        /// <summary>
        /// 从ftp下载文件到本地文件夹
        /// </summary>
        /// <param name="fullSourcePath">ftp文件完整地址</param>
        /// <param name="targetDirectoryPath">目标文件夹路径</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        public static void DownLoadFile(string fullSourcePath, string targetDirectoryPath, string username, string password)
        {
            Uri uri = new Uri(@fullSourcePath);
            string directory = Path.GetFullPath(targetDirectoryPath);
            string filename = Path.GetFileName(uri.LocalPath);
            //创建一个文件流
            string FileName = Path.GetFullPath(directory) + Path.DirectorySeparatorChar.ToString() + Path.GetFileName(filename);
            FileStream fs = null;
            Stream responseStream = null;
            try
            {
                //创建一个与FTP服务器联系的FtpWebRequest对象                
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
                //设置请求的方法是FTP文件下载                
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.UsePassive = false;
                //连接登录FTP服务器                
                request.Credentials = new NetworkCredential(username, password);
                //获取一个请求响应对象                
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                //获取请求的响应流                
                responseStream = response.GetResponseStream();
                //判断本地文件是否存在，如果存在，删除             
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                fs = File.Create(FileName);
                if (fs != null)
                {
                    int buffer_count = 65536;
                    byte[] buffer = new byte[buffer_count];
                    int size = 0;
                    while ((size = responseStream.Read(buffer, 0, buffer_count)) > 0)
                    {
                        fs.Write(buffer, 0, size);
                    }
                    fs.Flush();
                    fs.Close();
                    responseStream.Close();
                }
            }
            finally
            {
                if (fs != null)
                    fs.Close();
                if (responseStream != null)
                    responseStream.Close();
            }
        }

        /// <summary>
        /// 删除文件，不存在也不报错,地址传相对于当前文件夹下的路径，
        /// 例如删除folder1/test.txt当前在根目录,传folder1/test.txt，
        /// 如果在folder1文件夹内，传test.txt
        /// </summary>
        /// <param name="fileName"></param>
        public string DeleteFile(string fileName)
        {
            Debug.Log("DeleteFile: " + fileName);
            try
            {
                string uri = ftpURI + fileName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;

                string result = string.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new StreamReader(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
                Debug.Log("Delete OK");
            }
            catch (Exception ex)
            {
                return "删除失败 --> " + ex.Message + "  文件名:" + fileName;
            }

            return string.Empty;
        }

        /// <summary>
        /// 删除文件夹,文件夹不为空报错,文件夹不存在也报错
        /// </summary>
        /// <param name="folderName"></param>
        public void RemoveDirectory(string folderName)
        {
            try
            {
                string uri = ftpURI + folderName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;

                string result = String.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new StreamReader(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
                Debug.Log("Delete success: " + folderName);
            }
            catch (Exception ex)
            {
                throw new Exception("FtpHelper  Error --> " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前目录下明细(包含文件和文件夹)
        /// </summary>
        /// <returns>名称数组</returns>
        public string[] GetFilesDetailList()
        {
            try
            {
                StringBuilder result = new StringBuilder();
                FtpWebRequest ftp;
                ftp = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI));
                ftp.UseBinary = true;
                ftp.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                ftp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                ftp.KeepAlive = false;
                ftp.UsePassive = ftpMode;//表示连接类型为主动模式
                WebResponse response = ftp.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                //while (reader.Read() > 0)
                //{
                //}
                string line = reader.ReadLine();
                //line = reader.ReadLine();
                //line = reader.ReadLine();
                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                result.Remove(result.ToString().LastIndexOf("\n"), 1);
                reader.Close();
                response.Close();
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                throw new Exception("文件列表获取  Error --> " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前目录下文件列表(仅文件)，有问题
        /// </summary>
        /// <returns></returns>
        string[] GetFileList(string mask)
        {
            StringBuilder result = new StringBuilder();
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line.ToLower().IndexOf(mask) > 0)
                    {
                        result.Append(line);
                        result.Append("\n");
                    }
                    //if (mask.Trim() != string.Empty && mask.Trim() != "*.*")
                    //{
                    //    string mask_ = mask.Substring(0, mask.IndexOf("*"));
                    //    if (line.Substring(0, mask_.Length) == mask_)
                    //    {
                    //        result.Append(line);
                    //        result.Append("\n");
                    //    }
                    //}
                    //else
                    //{
                    //    result.Append(line);
                    //    result.Append("\n");
                    //}
                    line = reader.ReadLine();
                }
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                reader.Close();
                response.Close();
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                throw new Exception("FtpHelper  Error --> " + ex.Message);
            }
        }

        /// <summary>
        /// 获取当前目录下所有的文件夹列表(仅文件夹)
        /// </summary>
        /// <returns></returns>
        public string[] GetDirectoryList()
        {
            string[] drectory = GetFilesDetailList();
            string m = string.Empty;
            foreach (string str in drectory)
            {
                int dirPos = str.IndexOf("<DIR>");
                if (dirPos > 0)
                {
                    /*判断 Windows 风格*/
                    m += str.Substring(dirPos + 5).Trim() + "\n";
                }
                else if (str.Trim().Substring(0, 1).ToUpper() == "D")
                {
                    /*判断 Unix 风格*/
                    string dir = str.Substring(54).Trim();
                    if (dir != "." && dir != "..")
                    {
                        m += dir + "\n";
                    }
                }
            }
            char[] n = new char[] { '\n' };
            return m.Split(n);
        }

        /// <summary>
        /// 判断文件的目录是否存,不存则创建，路径不以/开头，需要以/结尾
        /// </summary>
        /// <param name="destFilePath">路径需要以/结尾，否则最后一个名称不创建文件夹</param>
        public void FtpCheckDirectoryExist(string destFilePath)
        {
            if (!destFilePath.EndsWith("/"))
                destFilePath = destFilePath + "/";
            string fullDir = FtpParseDirectory(destFilePath);   //目录解析
            string[] dirs = fullDir.Split('/');
            string curDir = "/";
            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i];
                try
                {
                    curDir += dir + "/";    //一层一层创建
                    MakeDir(curDir);        //创建文件夹
                    Debug.Log("MakeDir success");
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.Message);
                }
            }

        }

        //目录解析
        string FtpParseDirectory(string destFilePath)
        {
            return destFilePath.Substring(0, destFilePath.LastIndexOf("/"));
        }

        /// <summary>
        /// 判断当前目录下指定的子目录是否存在
        /// 不能越级判断，只能判断当前文件夹下的目录是否存在
        /// </summary>
        /// <param name="RemoteDirectoryName">指定的目录名</param>
        public bool DirectoryExist(string RemoteDirectoryName)
        {
            string[] dirList = GetDirectoryList();
            foreach (string str in dirList)
            {
                if (str.Trim() == RemoteDirectoryName.Trim())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断当前目录下指定的文件是否存在，有问题
        /// </summary>
        /// <param name="RemoteFileName">远程文件名</param>
        bool FileExist(string RemoteFileName)
        {
            string[] fileList = GetFileList("*.*");
            foreach (string str in fileList)
            {
                if (str.Trim() == RemoteFileName.Trim())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 在当前文件夹下创建单级文件夹，
        /// 文件夹存在就报错，dirName带多级也报错
        /// 文件名格式如"folderName"或"folderName/"
        /// </summary>
        /// <param name="dirName">要创建的文件夹名</param>
        public void MakeDir(string dirName)
        {
            FtpWebRequest reqFTP;
            try
            {
                // dirName = name of the directory to create.
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + dirName));
                reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("FtpHelper MakeDir Error --> " + ex.Message);
            }
        }

        /// <summary>
        /// 获取指定文件字节大小，如果在当前文件夹下，传文件名，如果在根目录下，传相对根目录的完整路径
        /// </summary>
        /// <param name="filename">文件路径</param>
        /// <returns></returns>
        public long GetFileSize(string filename)
        {
            FtpWebRequest reqFTP;
            long fileSize = 0;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + filename));
                reqFTP.Method = WebRequestMethods.Ftp.GetFileSize;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = ftpMode;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                fileSize = response.ContentLength;

                ftpStream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("FtpHelper GetFileSize Error --> " + ex.Message);
            }
            return fileSize;
        }

        /// <summary>
        /// 改名,只能改当前目录下的文件名，不能传带文件夹结构的文件名，
        /// 如folder1/folder2/text.txt，如果newFilename存在，会被覆盖
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="newFilename"></param>
        public void ReName(string currentFilename, string newFilename)
        {
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpURI + currentFilename));
                reqFTP.Method = WebRequestMethods.Ftp.Rename;
                reqFTP.RenameTo = newFilename;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = ftpMode;
                reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();
                ftpStream.Close();
                response.Close();
                Debug.Log("Rename success");
            }
            catch (Exception ex)
            {
                throw new Exception("FtpHelper ReName Error --> " + ex.Message);
            }
        }

        /// <summary>
        /// 移动文件,有问题
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="newFilename"></param>
        void MovieFile(string currentFilename, string newDirectory)
        {
            ReName(currentFilename, newDirectory);
        }

        /*示例代码
         * 构建实例
         * Ftp_Helper ftp_Helper = 
         * new Ftp_Helper("10.27.209.219", "8021", "/", "tmbh", "anquan@123", false);
         *转到ftp根目录下的StandaloneWindows64/aaa/文件夹内，需要确保文件夹存在
         *ftp_Helper.GotoDirectory("StandaloneWindows64/aaa/", true);
         *string[] directoies = ftp_Helper.GetDirectoryList();
         *进入上面aaa文件夹的子文件夹bbb
         *ftp_Helper.GotoDirectory("bbb/", false);
         *string[]  subdirectoies = ftp_Helper.GetDirectoryList();
        */
        /// <summary>
        /// 转到目录，isFromRoot=true，需要从根目录传路径，
        /// 比如访问ftp://127.0.0.1:21/folderA/folderB/，需要传folderA/folderB/
        /// </summary>
        /// <param name="DirectoryName"></param>
        /// <param name="isFromRoot">是否从根目录开始</param>
        public void GotoDirectory(string DirectoryName, bool isFromRoot)
        {
            if (isFromRoot)
            {
                ftpRemotePath = DirectoryName;
            }
            else
            {
                ftpRemotePath += DirectoryName + "/";
            }
            //ftpURI = "ftp://" + ftpServerIP + "/" + ftpRemotePath + "/";
            ftpURI = "ftp://" + ftpServerIP + ":" + ftpServerPort + "/";
            if (!string.IsNullOrEmpty(ftpRemotePath))
            {
                ftpURI += ftpRemotePath;
            }
        }
    }
}
