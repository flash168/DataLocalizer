using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace DataLocalizer
{
    public class DownloadManager
    {
        private ConcurrentDictionary<string, DownloadTask> _tasks;
        private SemaphoreSlim _semaphore;
        private HttpClient _client;

        public event Action<DownloadTask> OnProgressChanged;
        public event Action OnAllDownloadsCompleted;
        public event Action<DownloadTask> OnTaskDeleted; // 添加任务删除的事件

        public DownloadManager(int maxConcurrentDownloads)
        {
            _tasks = new ConcurrentDictionary<string, DownloadTask>();
            _semaphore = new SemaphoreSlim(maxConcurrentDownloads);
            _client = new HttpClient(); // 共享的 HttpClient 实例
        }

        public DownloadTask AddTask(string url, string filePath)
        {
            var task = new DownloadTask(url, filePath, _client);

            if (_tasks.TryAdd(task.Id, task))
            {
                task.OnStatusChanged += Task_OnStatusChanged;
                StartTaskAsync(task);
            }
            return task;
        }
        private async void StartTaskAsync(DownloadTask task)
        {
            await _semaphore.WaitAsync();
            if (task.Status != DownloadStatus.Canceled)
                await task.StartAsync();
            _semaphore.Release();
        }

        private void Task_OnStatusChanged(DownloadTask task)
        {
            var totalBytes = _tasks.Values.Sum(t => t.TotalBytes);
            var downloadedBytes = _tasks.Values.Sum(t => t.DownloadedBytes);
            var totalProgress = totalBytes == 0 ? 0 : (double)downloadedBytes / totalBytes;
            OnProgressChanged?.Invoke(task);

            if (_tasks.Values.All(t => t.Status == DownloadStatus.Completed || t.Status == DownloadStatus.Failed || t.Status == DownloadStatus.Canceled))
            {
                OnAllDownloadsCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="taskId">任务id,如果为空则全部取消</param>
        public void CancelTask(string? taskId = null)
        {
            if (taskId != null)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                    task.Cancel();
            }
            else
            {
                foreach (var task in _tasks.Values)
                {
                    task.Cancel();
                }
            }
        }


        /// <summary>
        /// 重试任务
        /// </summary>
        /// <param name="taskId">任务id,如果为空则全部重试</param>
        public void RetryTask(string? taskId = null)
        {
            if (taskId != null)
            {
                if (_tasks.TryGetValue(taskId, out var task))
                {
                    task.Status = DownloadStatus.Pending;
                    StartTaskAsync(task);
                }
            }
            else
            {
                foreach (var task in _tasks.Values)
                {
                    if (task.Status == DownloadStatus.Failed || task.Status == DownloadStatus.Canceled)
                    {
                        task.Status = DownloadStatus.Pending;
                        StartTaskAsync(task);
                    }
                }
            }
        }

        public void DeleteTask(string? taskId = null)
        {
            if (!string.IsNullOrEmpty(taskId))
            {
                if (_tasks.TryRemove(taskId, out var task))
                {
                    task.OnStatusChanged -= Task_OnStatusChanged;
                    task.Cancel();
                    OnTaskDeleted?.Invoke(task); // 触发任务删除事件
                }
            }
            else
            {
                foreach (var taskid in _tasks.Keys.ToList())
                {
                    if (_tasks.TryRemove(taskid, out var task))
                    {
                        task.OnStatusChanged -= Task_OnStatusChanged;
                        task.Cancel();
                        OnTaskDeleted?.Invoke(task); // 触发任务删除事件
                    }
                }
            }
        }

        public IEnumerable<DownloadTask> GetAllTasks() => _tasks.Values;
    }
}
