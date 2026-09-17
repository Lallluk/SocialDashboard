using System;

namespace SocialDashboard.Models;

public sealed class VideoAsset
{
    public int Id { get; set; }

    public string? OriginalUrl { get; set; }

    public required string LocalFilePath { get; set; }

    public required string FileName { get; set; }

    public long FileSizeBytes { get; set; }

    public double DurationSeconds { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string? ImportStatus { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}