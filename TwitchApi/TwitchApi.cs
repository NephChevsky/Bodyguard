using Db;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using NLog.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace TwitchApi
{
	public class TwitchApi
	{
		private Settings _settings;
		private static ILogger<TwitchApi> _logger;
		private TwitchAPI api;

        public TwitchApi(IConfiguration configuration, ILogger<TwitchApi> logger)
        {
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _logger = logger;

            api = new TwitchAPI();
            api.Settings.ClientId = _settings.Twitch.ClientId;
            api.Settings.Secret = _settings.Twitch.ClientSecret;

            CheckAndUpdateTokenStatus("TwitchApi").GetAwaiter().GetResult();
        }

		public TwitchApi(string tokenPrefix)
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<TwitchApi>();

			api = new TwitchAPI();
			api.Settings.ClientId = _settings.Twitch.ClientId;
			api.Settings.Secret = _settings.Twitch.ClientSecret;

			CheckAndUpdateTokenStatus(tokenPrefix).GetAwaiter().GetResult();
		}

		public async Task CheckAndUpdateTokenStatus(string tokenPrefix)
		{
			TwitchAPI api = new();
			api.Settings.ClientId = _settings.Twitch.ClientId;
			api.Settings.Secret = _settings.Twitch.ClientSecret;
			using (BodyguardDbContext db = new())
			{
				bool shouldRefreshToken = false;
				Token? accessToken = db.Tokens.Where(x => x.Name == tokenPrefix + "AccessToken").SingleOrDefault();
				if (accessToken != null)
				{
					ValidateAccessTokenResponse response = await api.Auth.ValidateAccessTokenAsync(accessToken.Value);
					if (response == null)
					{
						shouldRefreshToken = true;
					}
				}
				else
				{
					accessToken = new Token(tokenPrefix + "AccessToken");
					db.Tokens.Add(accessToken);
					shouldRefreshToken = true;
				}

				if (shouldRefreshToken)
				{
					Token? refreshToken = db.Tokens.Where(x => x.Name == tokenPrefix + "RefreshToken").SingleOrDefault();
					if (refreshToken != null)
					{
						RefreshResponse newToken = await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _settings.Twitch.ClientSecret);
						accessToken.Value = newToken.AccessToken;
						refreshToken.Value = newToken.RefreshToken;
					}
					else
					{
						refreshToken = new Token(tokenPrefix + "RefreshToken");
						db.Tokens.Add(refreshToken);

						var server = new WebServer(_settings.Twitch.RedirectUri);

						List<string> scopes = new List<string>();
						if (tokenPrefix == "TwitchChat")
						{
							scopes.Add("chat:read");
						}

						string uri = $"https://id.twitch.tv/oauth2/authorize?client_id={_settings.Twitch.ClientId}&redirect_uri={System.Web.HttpUtility.UrlEncode(_settings.Twitch.RedirectUri)}&response_type=code&scope={String.Join('+', scopes)}";
						_logger.LogWarning($"Please authorize here: {uri}");

						string auth = await server.Listen();

						AuthCodeResponse resp = await api.Auth.GetAccessTokenFromCodeAsync(auth, _settings.Twitch.ClientSecret, _settings.Twitch.RedirectUri);
						accessToken.Value = resp.AccessToken;
						refreshToken.Value = resp.RefreshToken;
					}
					db.SaveChanges();
				}
			}
		}

		public async Task RefreshTokenAsync()
		{
			using (BodyguardDbContext db = new())
			{
				Token accessToken = db.Tokens.Where(x => x.Name == "StreamerAccessToken").Single();
				Token refreshToken = db.Tokens.Where(x => x.Name == "StreamerRefreshToken").Single();
				RefreshResponse newToken = await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _settings.Twitch.ClientSecret);
				accessToken.Value = newToken.AccessToken;
				refreshToken.Value = newToken.RefreshToken;
				db.SaveChanges();
				api.Settings.AccessToken = newToken.AccessToken;
			}
		}

		public async Task<TwitchViewer?> GetOrCreateViewerById(string id)
		{
			using (BodyguardDbContext db = new())
			{
				TwitchViewer? viewer = db.TwitchViewers.Where(x => x.TwitchOwner == id).FirstOrDefault();
				if (viewer == null)
				{
					GetUsersResponse response = await api.Helix.Users.GetUsersAsync(new List<string>() { id });
					if (response != null && response.Users.Count() != 0)
					{
						viewer = new(response.Users[0].Id, response.Users[0].Login, response.Users[0].DisplayName);
						db.TwitchViewers.Add(viewer);
						db.SaveChanges();
					}
					else
					{
						return null;
					}
				}
				return viewer;
			}
		}
	}
}