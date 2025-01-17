﻿using System.IO;

namespace ZionGame
{
    public class DownloadInfo
    {
        public string bundle;
        public bool encryption;
        public string hash;
        public string savePath;
        public long size;
        public string url;

        public long downloadedSize
        {
            get
            {
                var info = new FileInfo(savePath);
                if (info.Exists)
                {
                    return info.Length;
                }
                return 0;
            }
        }

        public long downloadSize => size - downloadedSize;
    }
}