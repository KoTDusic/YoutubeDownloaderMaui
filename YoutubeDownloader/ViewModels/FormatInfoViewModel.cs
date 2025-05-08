using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeDLSharp.Metadata;

namespace YoutubeDownloader;

public partial class FormatInfoViewModel : ObservableObject
{
    private readonly Action<FormatInfoViewModel> _selectButtonClick;

    public FormatInfoViewModel(FormatData format, Action<FormatInfoViewModel> selectButtonClick)
    {
        _selectButtonClick = selectButtonClick;
        FormatId = format.FormatId;
        Format = format.FormatNote;
        Url = format.Url;
        Resolution = format.Resolution;
        if (uint.TryParse(format.Resolution.Split("x").FirstOrDefault(), out var resolutionFirstPart))
        {
            ResolutionFirstPart = resolutionFirstPart;
        }

        Extension = format.Extension;
        AudioCodec = format.AudioCodec;
        VideoCodec = format.VideoCodec;
        IsVideo = format.VideoCodec != "none";
        IsAudio = format.AudioCodec != "none";
        FileSize = format.ApproximateFileSize;
    }

    [ObservableProperty] public partial string FormatId { get; set; }
    [ObservableProperty] public partial string Format { get; set; }
    [ObservableProperty] public partial string Url { get; set; }
    [ObservableProperty] public partial string Extension { get; set; }
    [ObservableProperty] public partial string Resolution { get; set; }
    [ObservableProperty] public partial uint ResolutionFirstPart { get; set; }
    [ObservableProperty] public partial string VideoCodec { get; set; }
    [ObservableProperty] public partial string AudioCodec { get; set; }
    [ObservableProperty] public partial bool IsVideo { get; set; }
    [ObservableProperty] public partial bool IsAudio { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial long? FileSize { get; set; }

    [RelayCommand]
    private void SelectButton()
    {
        _selectButtonClick.Invoke(this);
    }

    public override string ToString() => $"{Format} ({VideoCodec}+{AudioCodec}) {Extension}";
}