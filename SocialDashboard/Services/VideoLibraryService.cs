using Microsoft.UI.Xaml;
using SocialDashboard.Data;
using SocialDashboard.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SocialDashboard.Services;

public sealed class VideoLibraryService
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(30)
    };
    private static readonly string[] SupportedExtensions =
    {
        ".mp4",
        ".mov",
        ".mkv",
        ".avi",
        ".webm"
    };
    public async Task<VideoAsset?> ImportVideoFromUrlAsync(
    string url)
    {
        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri? videoUri) ||
            (videoUri.Scheme != Uri.UriSchemeHttp &&
             videoUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Please enter a valid HTTP or HTTPS URL.");
        }

        using HttpRequestMessage request =
            new(HttpMethod.Get, videoUri);

        request.Headers.UserAgent.ParseAdd(
            "SocialDashboard/1.0");

        using HttpResponseMessage response =
            await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        string? contentType =
            response.Content.Headers.ContentType?.MediaType;

        if (contentType is not null &&
            !contentType.StartsWith(
                "video/",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The URL does not appear to point directly to a video file.");
        }

        string extension =
            Path.GetExtension(videoUri.AbsolutePath)
                .ToLowerInvariant();

        if (!SupportedExtensions.Contains(extension))
        {
            extension = ".mp4";
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

        await using Stream sourceStream =
            await response.Content.ReadAsStreamAsync();

        await using FileStream destinationStream =
            File.Create(destinationPath);

        await sourceStream.CopyToAsync(destinationStream);

        FileInfo fileInfo = new(destinationPath);

        VideoAsset video = new()
        {
            OriginalUrl = url,
            LocalFilePath = destinationPath,
            FileName = Path.GetFileName(destinationPath),
            FileSizeBytes = fileInfo.Length,
            DurationSeconds = 0,
            Width = 0,
            Height = 0,
            ImportStatus = "Downloaded from URL",
            CreatedAtUtc = DateTime.UtcNow
        };

        using AppDbContext database = new();

        database.Videos.Add(video);
        await database.SaveChangesAsync();

        return video;
    }
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