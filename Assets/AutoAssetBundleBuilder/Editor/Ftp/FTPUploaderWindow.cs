//using UnityFtp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;
using BaiduFtpUploadFiles;

/// <summary>
/// Ftp上传窗口扩展
/// </summary>
public class FTPUploaderWindow : EditorWindow
{
    public static readonly Version version = new Version(0, 1);
    const string applicationName = "Uploader";

    GUIContent serverLabel = new GUIContent("Server", "ftp url like:ftp://127.0.0.1:21/");
    GUIContent usernameLabel = new GUIContent("User Name", "ftp user name");
    GUIContent passwordLabel = new GUIContent("Password", "ftp password");
    GUIContent uploadLabel = new GUIContent("Upload");
    GUIContent uploadFolderLabel = new GUIContent("Upload Folder", "The directory to upload to on the server.");
    GUIContent remoteFolderLabel = new GUIContent("Remote Folder", "The name of the directory on the server");

    /// <summary>
    /// Initializes the window.
    /// </summary>
    [MenuItem("Window/FTPUploader")]
    static void Init()
    {
        // Show existing open window, or make new one.
        FTPUploaderWindow window = EditorWindow.GetWindow(typeof(FTPUploaderWindow)) as FTPUploaderWindow;
        window.Show();
    }

    string server = "ftp://172.24.76.133:21/";
    string username = "baidu";
    string password = "123";
    string uploadFolder = "";
    string remoteFolder = "";

    /// <summary>
    /// Displays the upload form.
    /// </summary>
    void OnGUI()
    {
        // 获取上次存储的数值
        server = EditorPrefs.GetString(applicationName + " server", "");
        username = EditorPrefs.GetString(applicationName + " username", "");
        password = EditorPrefs.GetString(applicationName + " password", "");
        uploadFolder = EditorPrefs.GetString(applicationName + " uploadFolder", "");
        remoteFolder = EditorPrefs.GetString(applicationName + " remoteFolder", "");

        //王辉的ftp服务端配置
        if (string.IsNullOrEmpty(server))
            server = "ftp://10.27.209.219:8021/";
        if (string.IsNullOrEmpty(username))
            username = "tmbh";
        if (string.IsNullOrEmpty(password))
            password = "anquan@123";

        // 获取输入的数值
        server = EditorGUILayout.TextField(serverLabel, server);
        username = EditorGUILayout.TextField(usernameLabel, username);
        password = EditorGUILayout.PasswordField(passwordLabel, password);
        uploadFolder = EditorGUILayout.TextField(uploadFolderLabel, uploadFolder);
        remoteFolder = EditorGUILayout.TextField(remoteFolderLabel, remoteFolder);

        // 存储输入的数值
        EditorPrefs.SetString(applicationName + " server", server);
        EditorPrefs.SetString(applicationName + " username", username);
        EditorPrefs.SetString(applicationName + " password", password);
        EditorPrefs.SetString(applicationName + " uploadFolder", uploadFolder);
        EditorPrefs.SetString(applicationName + " remoteFolder", remoteFolder);

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button(uploadLabel))
        {
            FtpUploaderHelper ftpUpHelper = new FtpUploaderHelper("10.27.209.219", "8021", "/", "tmbh", "anquan@123", false);

            //ftpUpHelper.UploadFileAsync("LocalFileName", "", "FtpFolderName/");

            ftpUpHelper.UploadFolderAsync(uploadFolder, remoteFolder);

        }

        EditorGUILayout.EndHorizontal();
    }
}