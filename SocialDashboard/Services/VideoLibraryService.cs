using Microsoft.UI.Xaml;
using SocialDashboard.Data;
using SocialDashboard.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SocialDashboard.Services;

public sealed class VideoLibraryService
{
    private static readonly string[] SupportedExtensions =
    {
        ".mp4",
        ".mov",
        ".mkv",
        ".avi",
        ".webm"
    };

    public async Task<VideoAsset?> ImportVideoAsync(
        Window ownerWindow)
    {
        FileOpenPicker picker = new();

        picker.FileTypeFilter.Add(".mp4");
        picker.FileTypeFilter.Add(".mov");
        picker.FileTypeFilter.Add(".mkv");
        picker.FileTypeFilter.Add(".avi");
        picker.FileTypeFilter.Add(".webm");

        WinRT.Interop.InitializeWithWindow.Initialize(
            picker,
            (nint)ownerWindow.AppWindow.Id.Value);

        StorageFile? selectedFile =
            await picker.PickSingleFileAsync();

        if (selectedFile is null)
        {
            return null;
        }

        string extension =
            Path.GetExtension(selectedFile.Name)
                .ToLowerInvariant();

        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "This video format is not supported.");
        }

        string videoFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SocialDashboard",
            "Videos");

        Directory.CreateDirectory(videoFolder);

        string destinationPath = Path.Combine(
            videoFolder,
            $"{Guid.NewGuid()}{extension}");

        using Stream sourceStream =
            await selectedFile.OpenStreamForReadAsync();

        using FileStream destinationStream =
            File.Create(destinationPath);

        await sourceStream.CopyToAsync(destinationStream);

        FileInfo fileInfo = new(destinationPath);

        VideoAsset video = new()
        {
            OriginalUrl = null,
            LocalFilePath = destinationPath,
            FileName = selectedFile.Name,
            FileSizeBytes = fileInfo.Length,
            DurationSeconds = 0,
            Width = 0,
            Height = 0,
            ImportStatus = "Imported",
            CreatedAtUtc = DateTime.UtcNow
        };

        using AppDbContext database = new();

        database.Videos.Add(video);
        await database.SaveChangesAsync();

        return video;
    }
}