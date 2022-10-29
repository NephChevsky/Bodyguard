using Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot.Services
{
	internal class TwitchBot : IHostedService
	{
		private Settings _settings;
		private TwitchChat.TwitchChat _chat;
		private TwitchApi.TwitchApi _api;
		private readonly ILogger _logger;
		private string _streamerName;
		private string _streamerId;

		public TwitchBot(string streamerName, string streamerId)
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<TwitchBot>();
			_streamerName = streamerName;
			_streamerId = streamerId;
			_chat = new TwitchChat.TwitchChat();
			_api = new TwitchApi.TwitchApi("TwitchApi");
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_chat.Client.OnMessageReceived += Client_OnMessageReceivedAsync;
			_chat.Connect(_streamerName);
			return Task.CompletedTask;
		}

		private async void Client_OnMessageReceivedAsync(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
		{
			await _api.GetOrCreateViewerById(e.ChatMessage.UserId);
			using (BodyguardDbContext db = new())
			{
				TwitchMessage message = new TwitchMessage(_streamerId, e.ChatMessage.UserId, e.ChatMessage.Message);
				db.Add(message);
				db.SaveChanges();
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_chat.Client.OnMessageReceived -= Client_OnMessageReceivedAsync;
			return Task.CompletedTask;
		}
	}
}
