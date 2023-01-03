using Azure;
using Db;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using NLog.Extensions.Logging;
using Polly;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace TwitchApi
{
	public class TwitchApi
	{
		private Settings _settings;
		private static ILogger<TwitchApi> _logger;
		private TwitchAPI api;

		private Timer RefreshTokenTimer;

		private readonly IAsyncPolicy<dynamic> _retryPolicy = Policy.WrapAsync(
			Policy<dynamic>.Handle<Exception>().FallbackAsync(fallbackValue: null, onFallbackAsync: (exception, context) =>
			{
				_logger.LogError($"Couldn't reach Twitch API: {exception.Exception.Message}");
				_logger.LogError($"Stack:\r\n{exception.Exception.StackTrace}");
				_logger.LogError($"Returning null");
				return Task.CompletedTask;
			}),
			Policy<dynamic>.Handle<Exception>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timespan) =>
			{
				_logger.LogWarning($"Twitch API call failed: {exception.Exception.Message}");
				_logger.LogWarning($"Retrying in {timespan.Seconds} seconds");
			}));

		public TwitchApi(IConfiguration configuration, ILogger<TwitchApi> logger)
        {
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _logger = logger;

			api = new TwitchAPI();
			api.Settings.ClientId = _settings.Twitch.ClientId;
			api.Settings.Secret = _settings.Twitch.ClientSecret;

			CheckAndUpdateTokenStatus().GetAwaiter().GetResult();
		}

		private async Task CheckAndUpdateTokenStatus()
		{
			using (BodyguardDbContext db = new())
			{
				TimeSpan firstRefresh = TimeSpan.FromSeconds(4 * 60 * 60);
				Token accessToken = db.Tokens.Where(x => x.Name == "TwitchApiAccessToken").SingleOrDefault();
				if (accessToken != null)
				{
					ValidateAccessTokenResponse response = await _retryPolicy.ExecuteAsync(async () =>
					{
						return await api.Auth.ValidateAccessTokenAsync(accessToken.Value);
					}); 
					if (response == null || (response != null && response.ExpiresIn <= 30 * 60))
					{
						Token refreshToken = db.Tokens.Where(x => x.Name == "TwitchApiRefreshToken").Single();
						RefreshResponse newToken = await _retryPolicy.ExecuteAsync(async () =>
						{
							return await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _settings.Twitch.ClientSecret);
						});
						if (newToken != null)
						{
							accessToken.Value = newToken.AccessToken;
							refreshToken.Value = newToken.RefreshToken;
							firstRefresh = TimeSpan.FromSeconds(newToken.ExpiresIn - 20 * 60);
							db.SaveChanges();
						}
						else
						{
							throw new Exception("Couldn't refresh Twitch API Token");
						}
					}
					else
					{
						firstRefresh = TimeSpan.FromSeconds(response.ExpiresIn - 20 * 60);
					}
				}
				else
				{
					var server = new WebServer(_settings.Twitch.RedirectUri);

					List<string> scopes = new List<string>();
					scopes.Add("chat:read");
					string uri = $"https://id.twitch.tv/oauth2/authorize?client_id={_settings.Twitch.ClientId}&redirect_uri={System.Web.HttpUtility.UrlEncode(_settings.Twitch.RedirectUri)}&response_type=code&scope={String.Join('+', scopes)}";
					_logger.LogWarning($"Please authorize here: {uri}");

					string auth = await server.Listen();

					AuthCodeResponse resp = await api.Auth.GetAccessTokenFromCodeAsync(auth, _settings.Twitch.ClientSecret, _settings.Twitch.RedirectUri);
					Token refreshToken = new Token("TwitchApiRefreshToken");
					accessToken.Value = resp.AccessToken;
					refreshToken.Value = resp.RefreshToken;
					db.Tokens.Add(accessToken);
					db.Tokens.Add(refreshToken);
					db.SaveChanges();
				}
				api.Settings.AccessToken = accessToken.Value;
				RefreshTokenTimer = new Timer(RefreshTokenAsync, null, firstRefresh, TimeSpan.FromSeconds(4 * 60 * 60));
			}
		}

		private async void RefreshTokenAsync(object state = null)
		{
			await CheckAndUpdateTokenStatus();
		}

		public async Task<TwitchViewer> GetOrCreateViewerById(string id)
		{
			using (BodyguardDbContext db = new())
			{
				TwitchViewer viewer = db.TwitchViewers.Where(x => x.TwitchOwner == id).FirstOrDefault();
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

		public async Task<TwitchStreamer> GetOrCreateStreamerByUsername(string username)
		{
			using (BodyguardDbContext db = new())
			{
				TwitchStreamer streamer = db.TwitchStreamers.Where(x => x.Name == username).FirstOrDefault();
				if (streamer == null)
				{
					GetUsersResponse response = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username });
					if (response != null && response.Users.Count() != 0)
					{
						streamer = db.TwitchStreamers.Where(x => x.TwitchOwner == response.Users[0].Id).FirstOrDefault();
						if (streamer != null && (streamer.Name != response.Users[0].Login || streamer.DisplayName != response.Users[0].DisplayName))
						{
							TwitchNameChange nameChange = new TwitchNameChange(streamer.TwitchOwner, streamer.Name);
							db.TwitchNameChanges.Add(nameChange);
							streamer.Name = response.Users[0].Login;
							streamer.DisplayName = response.Users[0].DisplayName;
						}
						else
						{
							streamer = new(response.Users[0].Id, response.Users[0].Login, response.Users[0].DisplayName);
							db.TwitchStreamers.Add(streamer);
						}
						db.SaveChanges();
					}
					else
					{
						return null;
					}
				}
				return streamer;
			}
		}

		public async Task<TwitchViewer> GetOrCreateViewerByUsername(string username)
		{
			using (BodyguardDbContext db = new())
			{
				TwitchViewer viewer = db.TwitchViewers.Where(x => x.Name == username).FirstOrDefault();
				if (viewer == null)
				{
					GetUsersResponse response = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username });
					if (response != null && response.Users.Count() != 0)
					{
						viewer = db.TwitchViewers.Where(x => x.TwitchOwner == response.Users[0].Id).FirstOrDefault();
						if (viewer != null && (viewer.Name != response.Users[0].Login || viewer.DisplayName != response.Users[0].DisplayName))
						{
							TwitchNameChange nameChange = new TwitchNameChange(viewer.TwitchOwner, viewer.Name);
							db.TwitchNameChanges.Add(nameChange);
							viewer.Name = response.Users[0].Login;
							viewer.DisplayName = response.Users[0].DisplayName;
						}
						else
						{
							viewer = new(response.Users[0].Id, response.Users[0].Login, response.Users[0].DisplayName);
							db.TwitchViewers.Add(viewer);
						}
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

		public async Task<List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>> GetStreams(List<string> streamerIds = null, List<string> languages = null, List<string> gameIds = null)
		{
			GetStreamsResponse response = await api.Helix.Streams.GetStreamsAsync(null, 100, gameIds, languages, streamerIds);
			if (response != null)
			{
				return response.Streams.ToList();
			}
			return new List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>();
		}
	}
}