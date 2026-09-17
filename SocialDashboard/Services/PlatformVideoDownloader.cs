using SocialDashboard.Data;
using SocialDashboard.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        string videoFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SocialDashboard",
                "Videos");

        Directory.CreateDirectory(videoFolder);

        string filePrefix =
            Guid.NewGuid().ToString();

        string outputTemplate =
            Path.Combine(
                videoFolder,
                $"{filePrefix}.%(ext)s");

        ProcessStartInfo startInfo =
            new()
            {
                FileName = _ytDlpPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

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

        if (Directory.Exists(
                _ffmpegDirectory))
        {
            startInfo.ArgumentList.Add(
                "--ffmpeg-location");

            startInfo.ArgumentList.Add(
                _ffmpegDirectory);
        }

        startInfo.ArgumentList.Add(url);

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
                LocalFilePath = downloadedFile,
                FileName =
                    Path.GetFileName(downloadedFile),
                FileSizeBytes = fileInfo.Length,
                DurationSeconds = 0,
                Width = 0,
                Height = 0,
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

    private static void DeleteIncompleteFiles(
        string folder,
        string filePrefix)
    {
        string[] files =
            Directory.GetFiles(folder)
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
}