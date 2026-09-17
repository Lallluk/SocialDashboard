using System;

namespace SocialDashboard.Models;

public sealed class VideoAsset
{
    public int Id { get; set; }

    public string? OriginalUrl { get; set; }

    public string? SourcePlatform { get; set; }

    public string? SourceTitle { get; set; }

    public string? SourceUploader { get; set; }

    public string? SourceVideoId { get; set; }

    public required string LocalFilePath { get; set; }

    public required string FileName { get; set; }

    public long FileSizeBytes { get; set; }

    public double DurationSeconds { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string? ImportStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}