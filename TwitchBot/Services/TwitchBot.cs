using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
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
		private readonly ILogger _logger;
		private string _channel;

		public TwitchBot(string channel)
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<TwitchBot>();
			_chat = new TwitchChat.TwitchChat();
			_channel = channel;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_chat.Client.OnMessageReceived += Client_OnMessageReceived;
			_chat.Connect(_channel);
			return Task.CompletedTask;
		}

		private void Client_OnMessageReceived(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
		{
			_logger.LogInformation($"{e.ChatMessage.DisplayName}: {e.ChatMessage.Message}");
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_chat.Client.OnMessageReceived -= Client_OnMessageReceived;
			return Task.CompletedTask;
		}
	}
}
