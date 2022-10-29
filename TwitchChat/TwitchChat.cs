using Db;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchChat
{
	public class TwitchChat
	{
		private Settings _settings;
		private readonly ILogger<TwitchChat> _logger;
		public TwitchClient Client;

		public TwitchChat(IConfiguration configuration, ILogger<TwitchChat> logger)
		{
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_logger = logger;
			CheckAndUpdateTokenStatus().GetAwaiter().GetResult();

			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			WebSocketClient customClient = new WebSocketClient(clientOptions);
			Client = new TwitchClient(customClient);

			using (BodyguardDbContext db = new())
			{
				Token token = db.Tokens.Where(x => x.Name == "TwitchBotAccessToken").Single();
				ConnectionCredentials credentials = new(_settings.Twitch.BotName, token.Value);
				Client.Initialize(credentials, "nephchevsky");
			}

			Client.Connect();

			while (!Client.IsConnected)
			{
				Task.Delay(20).Wait();
			}
		}

		public async Task CheckAndUpdateTokenStatus()
		{
			TwitchAPI api = new();
			api.Settings.ClientId = _settings.Twitch.ClientId;
			api.Settings.Secret = _settings.Twitch.ClientSecret;
			using (BodyguardDbContext db = new())
			{
				bool shouldRefreshToken = false;
				Token? accessToken = db.Tokens.Where(x => x.Name == "TwitchBotAccessToken").SingleOrDefault();
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
					accessToken = new Token("TwitchBotAccessToken");
					db.Tokens.Add(accessToken);
					shouldRefreshToken = true;
				}

				if (shouldRefreshToken)
				{
					Token? refreshToken = db.Tokens.Where(x => x.Name == "TwitchBotRefreshToken").SingleOrDefault();
					if (refreshToken != null)
					{
						RefreshResponse newToken = await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _settings.Twitch.ClientSecret);
						accessToken.Value = newToken.AccessToken;
						refreshToken.Value = newToken.RefreshToken;
					}
					else
					{
						refreshToken = new Token("TwitchBotRefreshToken");
						db.Tokens.Add(refreshToken);

						var server = new WebServer(_settings.Twitch.RedirectUri);
						List<string> scopes = new List<string> { "chat:read" };

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
	}
}