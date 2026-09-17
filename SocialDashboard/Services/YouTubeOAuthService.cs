using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SocialDashboard.Services;

public sealed class YouTubeOAuthService
{
    private static readonly string[] Scopes =
    {
        YouTubeService.Scope.YoutubeReadonly,
        YouTubeService.Scope.YoutubeUpload,
        YouTubeService.Scope.YoutubeForceSsl
    };

    public async Task<YouTubeService> ConnectAsync()
    {
        string credentialsPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Credentials",
                "client_secret.json");

        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException(
                $"Google credentials were not found at:{Environment.NewLine}{credentialsPath}");
        }

        using FileStream credentialsStream =
            new(
                credentialsPath,
                FileMode.Open,
                FileAccess.Read);

        GoogleClientSecrets secrets =
            await GoogleClientSecrets
                .FromStreamAsync(credentialsStream);

        string tokenFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SocialDashboard",
                "GoogleToken");

        Directory.CreateDirectory(tokenFolder);

        UserCredential credential =
            await GoogleWebAuthorizationBroker
                .AuthorizeAsync(
                    secrets.Secrets,
                    Scopes,
                    "youtube-user",
                    CancellationToken.None,
                    new FileDataStore(tokenFolder));

        return new YouTubeService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer =
                    credential,

                ApplicationName =
                    "SocialDashboard"
            });
    }
}