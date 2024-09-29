using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DataLocalizer
{
    public enum DownloadStatus
    {
        [Description("等待下载")]//添加后没开始下载
        Pending,
        [Description("下载中")]
        Running,
        [Description("已完成")]
        Completed,
        [Description("异常")]
        Failed,
        [Description("已取消")]
        Canceled
    }

    public class DownloadTask : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Id { get; private set; }
        public string Url { get; private set; }
        public string FilePath { get; private set; }
        private long downloadedBytes;
        public long DownloadedBytes
        {
            get { return downloadedBytes; }
            private set { downloadedBytes = value; OnPropertyChanged(); Progress = TotalBytes == 0 ? 0 : (double)DownloadedBytes / TotalBytes; }
        }
        private long totalBytes;
        public long TotalBytes
        {
            get { return totalBytes; }
            private set { totalBytes = value; OnPropertyChanged(); Progress = TotalBytes == 0 ? 0 : (double)DownloadedBytes / TotalBytes; }
        }
        private double progress;
        public double Progress
        {
            get { return progress; }
            private set { progress = value; OnPropertyChanged(); }
        }

        private DownloadStatus status;
        public DownloadStatus Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged(); }
        }


        private HttpClient _client;
        private CancellationTokenSource _cancellationTokenSource;

        public event Action<DownloadTask> OnStatusChanged;

        public DownloadTask(string url, string filePath, HttpClient client)
        {
            Id = Guid.NewGuid().ToString();
            Url = url;
            FilePath = filePath;
            Status = DownloadStatus.Pending;
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        private void InitDirectory()
        {
            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        }


        /// <summary>
        /// 开始
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                InitDirectory();
                DownloadedBytes = 0;
                _cancellationTokenSource = new CancellationTokenSource();
                Status = DownloadStatus.Running;
                OnStatusChanged?.Invoke(this);

                var response = await _client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, _cancellationTokenSource.Token);
                response.EnsureSuccessStatusCode();

                TotalBytes = response.Content.Headers.ContentLength ?? 0;

                using (var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        DownloadedBytes += bytesRead;
                        OnStatusChanged?.Invoke(this);
                    }
                }

                if (DownloadedBytes == TotalBytes || TotalBytes == 0)
                {
                    Status = DownloadStatus.Completed;
                }
                else
                {
                    Status = DownloadStatus.Failed;
                }
            }
            catch (OperationCanceledException)
            {
                Status = DownloadStatus.Canceled;
            }
            catch (Exception)
            {
                Status = DownloadStatus.Failed;
            }
            finally
            {
                OnStatusChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        public void Cancel()
        {
            if (Status == DownloadStatus.Running)
                _cancellationTokenSource.Cancel();
            else if (Status == DownloadStatus.Pending)
            {
                Status = DownloadStatus.Canceled;
                _cancellationTokenSource?.Cancel();
                OnStatusChanged?.Invoke(this);
            }
        }


    }
}
