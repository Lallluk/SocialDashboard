using Microsoft.EntityFrameworkCore;
using SocialDashboard.Models;
using System;
using System.IO;

namespace SocialDashboard.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<VideoAsset> Videos => Set<VideoAsset>();

    public DbSet<YouTubeAccount> YouTubeAccounts => Set<YouTubeAccount>();

    public DbSet<YouTubeUpload> YouTubeUploads => Set<YouTubeUpload>();

    public DbSet<YouTubeStatistics> YouTubeStatistics => Set<YouTubeStatistics>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SocialDashboard");

        Directory.CreateDirectory(appDataFolder);

        string databasePath = Path.Combine(
            appDataFolder,
            "socialdashboard.db");

        optionsBuilder.UseSqlite($"Data Source={databasePath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VideoAsset>()
            .Property(video => video.LocalFilePath)
            .IsRequired();

        modelBuilder.Entity<VideoAsset>()
            .Property(video => video.FileName)
            .IsRequired();

        modelBuilder.Entity<YouTubeAccount>()
            .Property(account => account.ChannelId)
            .IsRequired();

        modelBuilder.Entity<YouTubeUpload>()
            .Property(upload => upload.Title)
            .IsRequired();
    }
}