using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot.Services
{
	internal class TwitchBot : IHostedService
	{
		private TwitchChat.TwitchChat _chat;
		private readonly ILogger<TwitchBot> _logger;

		public TwitchBot(ILogger<TwitchBot> logger, TwitchChat.TwitchChat chat)
		{
			_logger = logger;
			_chat = chat;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_chat.Client.OnMessageReceived += Client_OnMessageReceived;
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
