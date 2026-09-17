using SocialDashboard.Data;
using SocialDashboard.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SocialDashboard.Services;

public sealed class PlatformVideoDownloader
{
    private readonly string _ytDlpPath;

    private readonly string _ffmpegDirectory;

    public PlatformVideoDownloader()
    {
        string toolsFolder =
            Path.Combine(
                AppContext.BaseDirectory,
                "Tools");

        _ytDlpPath =
            Path.Combine(
                toolsFolder,
                "yt-dlp.exe");

        _ffmpegDirectory =
            Path.Combine(
                toolsFolder,
                "ffmpeg",
                "bin");
    }

    public async Task<VideoAsset> DownloadAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Please enter a valid HTTP or HTTPS URL.");
        }

        if (!File.Exists(_ytDlpPath))
        {
            throw new FileNotFoundException(
                $"yt-dlp.exe was not found at {_ytDlpPath}");
        }

        VideoMetadata metadata =
            await ReadMetadataAsync(
                url,
                cancellationToken);

        string videoFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SocialDashboard",
                "Videos");

        Directory.CreateDirectory(videoFolder);

        string safeTitle =
            MakeSafeFileName(
                string.IsNullOrWhiteSpace(
                    metadata.Title)
                    ? "downloaded-video"
                    : metadata.Title);

        string filePrefix =
            $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";

        string outputTemplate =
            Path.Combine(
                videoFolder,
                $"{filePrefix}-{safeTitle}.%(ext)s");

        ProcessStartInfo startInfo =
            CreateProcessStartInfo();

        AddDownloadArguments(
            startInfo,
            outputTemplate,
            url);

        using Process process =
            new()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

        StringBuilder errors =
            new();

        process.ErrorDataReceived +=
            (_, eventArgs) =>
            {
                if (!string.IsNullOrWhiteSpace(
                        eventArgs.Data))
                {
                    errors.AppendLine(
                        eventArgs.Data);
                }
            };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Could not start yt-dlp.");
        }

        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(
                    entireProcessTree: true);
            }

            DeleteIncompleteFiles(
                videoFolder,
                filePrefix);

            throw;
        }

        if (process.ExitCode != 0)
        {
            DeleteIncompleteFiles(
                videoFolder,
                filePrefix);

            throw new InvalidOperationException(
                $"Download failed:{Environment.NewLine}{errors}");
        }

        string? downloadedFile =
            Directory
                .GetFiles(videoFolder)
                .Where(
                    file =>
                        Path.GetFileName(file)
                            .StartsWith(
                                filePrefix,
                                StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(
                    File.GetLastWriteTimeUtc)
                .FirstOrDefault();

        if (downloadedFile is null)
        {
            throw new FileNotFoundException(
                "yt-dlp completed, but no downloaded video was found.");
        }

        FileInfo fileInfo =
            new(downloadedFile);

        VideoAsset video =
            new()
            {
                OriginalUrl = url,
                SourcePlatform =
                    DetectPlatform(uri),
                SourceTitle =
                    metadata.Title,
                SourceUploader =
                    metadata.Uploader,
                SourceVideoId =
                    metadata.VideoId,
                LocalFilePath =
                    downloadedFile,
                FileName =
                    Path.GetFileName(downloadedFile),
                FileSizeBytes =
                    fileInfo.Length,
                DurationSeconds =
                    metadata.DurationSeconds,
                Width =
                    metadata.Width,
                Height =
                    metadata.Height,
                ImportStatus =
                    "Downloaded from platform URL",
                CreatedAtUtc =
                    DateTime.UtcNow
            };

        using AppDbContext database =
            new();

        database.Videos.Add(video);

        await database.SaveChangesAsync();

        return video;
    }

    private async Task<VideoMetadata> ReadMetadataAsync(
        string url,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo =
            CreateProcessStartInfo();

        startInfo.ArgumentList.Add(
            "--dump-single-json");

        startInfo.ArgumentList.Add(
            "--no-playlist");

        startInfo.ArgumentList.Add(
            "--skip-download");

        startInfo.ArgumentList.Add(url);

        using Process process =
            new()
            {
                StartInfo = startInfo
            };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Could not start yt-dlp metadata reader.");
        }

        string json =
            await process.StandardOutput
                .ReadToEndAsync(cancellationToken);

        string errors =
            await process.StandardError
                .ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(
            cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Could not read video metadata:{Environment.NewLine}{errors}");
        }

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        return new VideoMetadata
        {
            VideoId =
                GetString(
                    root,
                    "id"),

            Title =
                GetString(
                    root,
                    "title"),

            Uploader =
                GetString(
                    root,
                    "uploader"),

            DurationSeconds =
                GetDouble(
                    root,
                    "duration"),

            Width =
                GetInt(
                    root,
                    "width"),

            Height =
                GetInt(
                    root,
                    "height")
        };
    }

    private ProcessStartInfo CreateProcessStartInfo()
    {
        ProcessStartInfo startInfo =
            new()
            {
                FileName = _ytDlpPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

        if (Directory.Exists(
                _ffmpegDirectory))
        {
            startInfo.ArgumentList.Add(
                "--ffmpeg-location");

            startInfo.ArgumentList.Add(
                _ffmpegDirectory);
        }

        return startInfo;
    }

    private static void AddDownloadArguments(
        ProcessStartInfo startInfo,
        string outputTemplate,
        string url)
    {
        startInfo.ArgumentList.Add(
            "--no-playlist");

        startInfo.ArgumentList.Add(
            "--merge-output-format");

        startInfo.ArgumentList.Add(
            "mp4");

        startInfo.ArgumentList.Add(
            "--newline");

        startInfo.ArgumentList.Add(
            "--no-warnings");

        startInfo.ArgumentList.Add(
            "-o");

        startInfo.ArgumentList.Add(
            outputTemplate);

        startInfo.ArgumentList.Add(
            url);
    }

    private static string DetectPlatform(
        Uri uri)
    {
        string host =
            uri.Host.ToLowerInvariant();

        if (host.Contains("youtube") ||
            host.Contains("youtu.be"))
        {
            return "YouTube";
        }

        if (host.Contains("tiktok"))
        {
            return "TikTok";
        }

        if (host.Contains("instagram"))
        {
            return "Instagram";
        }

        return "Other";
    }

    private static string MakeSafeFileName(
        string fileName)
    {
        foreach (char invalidCharacter
            in Path.GetInvalidFileNameChars())
        {
            fileName =
                fileName.Replace(
                    invalidCharacter,
                    '_');
        }

        return fileName.Length > 80
            ? fileName[..80]
            : fileName;
    }

    private static string? GetString(
        JsonElement root,
        string propertyName)
    {
        return root.TryGetProperty(
                propertyName,
                out JsonElement value) &&
            value.ValueKind ==
                JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static double GetDouble(
        JsonElement root,
        string propertyName)
    {
        return root.TryGetProperty(
                propertyName,
                out JsonElement value) &&
            value.TryGetDouble(
                out double result)
            ? result
            : 0;
    }

    private static int GetInt(
        JsonElement root,
        string propertyName)
    {
        return root.TryGetProperty(
                propertyName,
                out JsonElement value) &&
            value.TryGetInt32(
                out int result)
            ? result
            : 0;
    }

    private static void DeleteIncompleteFiles(
        string folder,
        string filePrefix)
    {
        string[] files =
            Directory
                .GetFiles(folder)
                .Where(
                    file =>
                        Path.GetFileName(file)
                            .StartsWith(
                                filePrefix,
                                StringComparison.OrdinalIgnoreCase))
                .ToArray();

        foreach (string file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }
    }

    private sealed class VideoMetadata
    {
        public string? VideoId { get; set; }

        public string? Title { get; set; }

        public string? Uploader { get; set; }

        public double DurationSeconds { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}