using System;

namespace SocialDashboard.Models;

public sealed class YouTubeUpload
{
    public int Id { get; set; }

    public int VideoAssetId { get; set; }

    public int YouTubeAccountId { get; set; }

    public string? YouTubeVideoId { get; set; }

    public string? YouTubeUrl { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? TagsJson { get; set; }

    public string PrivacyStatus { get; set; } = "private";

    public string UploadStatus { get; set; } = "Draft";

    public string? ErrorMessage { get; set; }

    public DateTime? UploadedAtUtc { get; set; }
}