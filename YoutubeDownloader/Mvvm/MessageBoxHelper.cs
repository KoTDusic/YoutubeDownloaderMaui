namespace YoutubeDownloader;

public static class MessageBoxHelper
{
    public static async Task NotifyAsync(string title, string message, string cancel)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
            Application.Current?.Windows[0].Page?.DisplayAlert(title, message, cancel));
    }
}