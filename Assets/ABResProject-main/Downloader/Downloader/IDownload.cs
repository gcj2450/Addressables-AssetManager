using System.ComponentModel;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Downloader
{
    public interface IDownload : IDisposable
    {
         string Url { get; }
         string Folder { get; }
         string Filename { get; }
         long DownloadedFileSize { get; }
         long TotalFileSize { get; }
         DownloadPackage Package { get; }
         DownloadStatus Status { get; }

         event EventHandler<DownloadProgressChangedEventArgs> ChunkDownloadProgressChanged;
         event EventHandler<AsyncCompletedEventArgs> DownloadFileCompleted;
         event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;
         event EventHandler<DownloadStartedEventArgs> DownloadStarted;

         Task<Stream> StartAsync(CancellationToken cancellationToken = default);
         void Stop();
         void Pause();
         void Resume();
    }
}
