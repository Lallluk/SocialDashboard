using System;

namespace SocialDashboard.Models;

public sealed class YouTubeStatistics
{
    public int Id { get; set; }

    public int YouTubeUploadId { get; set; }

    public long ViewCount { get; set; }

    public long LikeCount { get; set; }

    public long CommentCount { get; set; }

    public DateTime RetrievedAtUtc { get; set; }
}