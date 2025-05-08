namespace YoutubeDownloader;

public partial class App
{
    public App() => InitializeComponent();

    protected override Window CreateWindow(IActivationState activationState) =>
        activationState?.Context.Services.GetService<MainWindow>() ?? new Window();
}