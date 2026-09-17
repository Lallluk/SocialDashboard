using System;

namespace SocialDashboard.Models;

public sealed class YouTubeAccount
{
    public int Id { get; set; }

    public required string ChannelId { get; set; }

    public string? ChannelTitle { get; set; }

    public string? ChannelHandle { get; set; }

    public string? Email { get; set; }

    public DateTime? TokenExpiresAtUtc { get; set; }

    public DateTime ConnectedAtUtc { get; set; }

    public DateTime? LastCheckedAtUtc { get; set; }

    public string ConnectionStatus { get; set; } = "Connected";
}