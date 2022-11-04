using Db;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using NLog.Extensions.Logging;
using TwitchApi;
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

		private string _channel;

		public TwitchClient Client;

		public TwitchChat(string channel)
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

			_channel = channel;
		}

		public void Connect()
		{
			using (BodyguardDbContext db = new())
			{
				Token token = db.Tokens.Where(x => x.Name == "TwitchApiAccessToken").Single();
				ConnectionCredentials credentials = new(_settings.Twitch.BotName, token.Value);
				Client.Initialize(credentials, _channel);
			}

			bool ret = Client.Connect();

			while (!Client.IsConnected)
			{
				Task.Delay(20).Wait();
			}

			_logger.LogInformation($"Successfully connected to {_channel}");
		}

		public void Disconnect()
		{
			Client.Disconnect();
			_logger.LogInformation($"Successfully disconnected from {_channel}");
		}
	}
}