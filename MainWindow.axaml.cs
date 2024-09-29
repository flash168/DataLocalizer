using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;

namespace DataLocalizer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            sourceService = new SourceService();
            sourceService.downloadManager.OnProgressChanged += DownloadManager_OnProgressChanged;
        }
        SourceService sourceService;
        private async void Button_Go(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var url = txt_url.Text?.Trim();
            if (string.IsNullOrEmpty(url) || !url.StartsWith("http", System.StringComparison.OrdinalIgnoreCase)) return;
            mang.IsVisible = true;
            string json = await sourceService.GetSource(url);
            txt_Source.Text = json;
            mang.IsVisible = false;
        }

        private async void Button_Down(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // �ӵ�ǰ�ؼ���ȡ TopLevel�����ߣ���Ҳ����ʹ�� Window ���á�
            var topLevel = TopLevel.GetTopLevel(this);
            // �����첽�����Դ򿪶Ի���
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "��ѡ�񱣴�·��",
                AllowMultiple = false
            });
            if (folder.Count <= 0) return;

            mang.IsVisible = true;
            progress.Value = 0;

            progress.Maximum = await sourceService.SaveLocal(Uri.UnescapeDataString(folder[0].Path.AbsolutePath));
            sourceService.downloadManager.OnAllDownloadsCompleted -= DownloadManager_OnAllDownloadsCompleted;
            sourceService.downloadManager.OnAllDownloadsCompleted += DownloadManager_OnAllDownloadsCompleted;
        }

        private void DownloadManager_OnAllDownloadsCompleted()
        {
            mang.IsVisible = false;
        }

        private void DownloadManager_OnProgressChanged(DownloadTask obj)
        {
            if (obj.Status == DownloadStatus.Completed)
                progress.Value += 1;
        }
    }
}