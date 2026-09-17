using Microsoft.UI.Xaml;
using SocialDashboard.Models;
using SocialDashboard.Services;
using System;

namespace SocialDashboard;

public sealed partial class MainWindow : Window
{
    private readonly VideoLibraryService _videoLibraryService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void ImportVideoButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Select a video file...";

            VideoAsset? video =
                await _videoLibraryService.ImportVideoAsync(this);

            if (video is null)
            {
                StatusText.Text = "Import cancelled.";
                return;
            }

            StatusText.Text =
                $"Imported: {video.FileName}\n" +
                $"Size: {video.FileSizeBytes:N0} bytes";
        }
        catch (Exception exception)
        {
            StatusText.Text =
                $"Import failed: {exception.Message}";
        }
    }
}