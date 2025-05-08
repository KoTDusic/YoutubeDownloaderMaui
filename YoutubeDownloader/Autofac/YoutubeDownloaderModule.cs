using Autofac;
using YoutubeDLSharp;

namespace YoutubeDownloader;

public class YoutubeDownloaderModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<MainViewModel>().AsSelf().SingleInstance();
        builder.RegisterType<MainPage>().AsSelf().SingleInstance();
        builder.Register(_ => new YoutubeDL
        {
            YoutubeDLPath = @"F:\Downloads\yt-dlp.exe",
            OutputFolder = @"F:\Downloads\youtube-dl-download",
            OutputFileTemplate = "%(title)s.%(ext)s"
        }).AsSelf().SingleInstance();
        builder.Register(context => new MainWindow(context.Resolve<MainPage>())
        {
            BindingContext = context.Resolve<MainViewModel>()
        }).AsSelf().SingleInstance();
    }
}