using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using SocialDashboard.Data;
using SocialDashboard.Models;
using SocialDashboard.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SocialDashboard;

public sealed partial class MainWindow : Window
{
    private readonly VideoLibraryService _videoLibraryService = new();

    private readonly ObservableCollection<VideoAsset> _videos = new();

    public MainWindow()
    {
        InitializeComponent();

        VideoListView.ItemsSource = _videos;

        _ = LoadVideosAsync();
    }

    private async Task LoadVideosAsync()
    {
        try
        {
            using AppDbContext database = new();

            List<VideoAsset> videos =
                await database.Videos
                    .OrderByDescending(video => video.CreatedAtUtc)
                    .ToListAsync();

            _videos.Clear();

            foreach (VideoAsset video in videos)
            {
                _videos.Add(video);
            }

            StatusText.Text =
                $"{_videos.Count} video(s) in your library.";
        }
        catch (Exception exception)
        {
            StatusText.Text =
                $"Could not load library: {exception.Message}";
        }
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

            _videos.Insert(0, video);

            StatusText.Text =
                $"Imported: {video.FileName}";
        }
        catch (Exception exception)
        {
            StatusText.Text =
                $"Import failed: {exception.Message}";
        }
    }

    private async void RefreshLibraryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await LoadVideosAsync();
    }
}