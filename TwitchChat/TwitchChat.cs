using Db;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using NLog.Extensions.Logging;
using TwitchApi;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchChat
{
	public class TwitchChat
	{
		private Settings _settings;
		private ILogger<TwitchChat> _logger;
		public TwitchClient Client;

		public TwitchChat()
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<TwitchChat>();

			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			WebSocketClient customClient = new WebSocketClient(clientOptions);
			Client = new TwitchClient(customClient);
		}

		public bool Connect(string channel)
		{
			TwitchApi.TwitchApi api = new();

			using (BodyguardDbContext db = new())
			{
				Token token = db.Tokens.Where(x => x.Name == "TwitchChatAccessToken").Single();
				ConnectionCredentials credentials = new(_settings.Twitch.BotName, token.Value);
				Client.Initialize(credentials, channel);
			}

			bool ret = Client.Connect();

			while (!Client.IsConnected)
			{
				Task.Delay(20).Wait();
			}

			_logger.LogInformation("Successfully connected to " + channel);
			return ret;
		}

		public void Disconnect()
		{
			Client.Disconnect();
			_logger.LogInformation("Successfully disconnected from chat");
		}
	}
}