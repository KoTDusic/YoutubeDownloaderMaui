using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace YoutubeDownloader;

public partial class MainViewModel : ObservableObject, IProgress<DownloadProgress>
{
    private readonly YoutubeDL _youtubeDl;
    private readonly OptionSet _options;

    public MainViewModel(YoutubeDL youtubeDl)
    {
        _youtubeDl = youtubeDl;
        _options = new OptionSet
        {
            CookiesFromBrowser = "firefox"
        };
    }

    [ObservableProperty]
    public partial ObservableCollection<FormatInfoViewModel> VideoFormats { get; private set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<FormatInfoViewModel> AudioFormats { get; private set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunSearchCommand), nameof(SaveCommand))]
    public partial string Uri { get; set; }

    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial string Thumbnail { get; set; }
    [ObservableProperty] public partial double Progress { get; set; }

    [RelayCommand(CanExecute = nameof(CanRunSearch), AllowConcurrentExecutions = false)]
    private async Task RunSearch()
    {
        var dataFetch = await _youtubeDl.RunVideoDataFetch(Uri, overrideOptions: _options).ConfigureAwait(false);
        if (!dataFetch.Success)
        {
            return;
        }

        var data = dataFetch.Data;
        Title = data.Title.Replace('|', '｜');
        Thumbnail = data.Thumbnail;
        var audioFormats = new List<FormatInfoViewModel>();
        var videoFormats = new List<FormatInfoViewModel>();
        foreach (var format in data.Formats.Select(o => new FormatInfoViewModel(o, SelectButtonClick)))
        {
            if (format.IsAudio && !format.IsVideo)
            {
                audioFormats.Add(format);
                continue;
            }

            if (format.IsVideo && !format.IsAudio)
            {
                videoFormats.Add(format);
            }

            audioFormats.Reverse();
            videoFormats.Reverse();
            AudioFormats = new ObservableCollection<FormatInfoViewModel>(audioFormats);
            VideoFormats =
                new ObservableCollection<FormatInfoViewModel>(
                    videoFormats.OrderByDescending(model => model.ResolutionFirstPart));
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunSearch), AllowConcurrentExecutions = false)]
    private async Task Save()
    {
        var audio = AudioFormats.FirstOrDefault(model => model.IsSelected);
        var video = VideoFormats.FirstOrDefault(model => model.IsSelected);
        if (video == null && audio != null)
        {
            await DownloadAudioAsync(audio).ConfigureAwait(false);
        }

        if (video != null && audio != null)
        {
            await DownloadVideo(video, audio);
        }
    }

    private async Task DownloadVideo(FormatInfoViewModel video, FormatInfoViewModel audio)
    {
        Progress = 0;
        var result = await _youtubeDl.RunVideoDownload(Uri, $"{video.FormatId}+{audio.FormatId}", progress: this)
            .ConfigureAwait(false);
        if (result.Success)
        {
            var path = $"{_youtubeDl.OutputFolder}\\{Title}{Path.GetExtension(result.Data)}";

            ShowInExplorer(path);
        }

        Progress = 0;
    }

    private async Task DownloadAudioAsync(FormatInfoViewModel audio)
    {
        Progress = 0;
        var ext = audio.AudioCodec == "opus" ? "opus" : audio.Extension;
        var fileName = $"{Title}.{ext}";
        var file = await FileSaver.SaveAsync(_youtubeDl.OutputFolder, fileName, new MemoryStream())
            .ConfigureAwait(false);

        if (!file.IsSuccessful || file.FilePath == null)
        {
            await MessageBoxHelper.NotifyAsync("Отмена", "Сохранение отменено.", "OK").ConfigureAwait(false);
            return;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(0, null);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        using var response = await client.GetAsync(audio.Url).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? -1;
        if (contentLength <= 0)
            throw new Exception("Сервер не сообщил размер файла.");
        await using var streamToWriteTo = File.Open(file.FilePath, FileMode.Create);
        await using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await streamToReadFrom.ReadAsync(buffer)) > 0)
        {
            await streamToWriteTo.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            Progress = (double)totalRead / contentLength;
        }

        Progress = 0;
        ShowInExplorer(file.FilePath);
    }

    private static void ShowInExplorer(string filePath)
    {
        var args = $"/select,\"{filePath}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", args));
    }

    private void SelectButtonClick(FormatInfoViewModel button)
    {
        var isSelected = button.IsSelected;
        if (button.IsAudio)
        {
            foreach (var audioFormat in AudioFormats)
            {
                audioFormat.IsSelected = false;
            }

            if (!isSelected)
            {
                button.IsSelected = true;
            }

            return;
        }

        if (button.IsVideo)
        {
            foreach (var videoFormat in VideoFormats)
            {
                videoFormat.IsSelected = false;
            }
        }

        if (!isSelected)
        {
            button.IsSelected = true;
        }
    }

    private bool CanRunSearch() => !string.IsNullOrEmpty(Uri);

    public void Report(DownloadProgress downloadProgress)
    {
        Progress = downloadProgress.Progress;
    }
}