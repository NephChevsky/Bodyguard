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
using TwitchLib.Client.Events;

namespace TwitchChatParser.Services
{
	internal class TwitchChatParser : IHostedService
	{
		private Settings _settings;
		private readonly ILogger _logger;
		private TwitchChat.TwitchChat _chat;
		private TwitchApi.TwitchApi _api;
		private string _streamerName;
		private string _streamerId;

		public TwitchChatParser(string streamerName, string streamerId)
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<TwitchChatParser>();
			_streamerName = streamerName;
			_streamerId = streamerId;
			_chat = new TwitchChat.TwitchChat();
			_api = new TwitchApi.TwitchApi("TwitchApi");
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Starting chat bot for " + _streamerName);
			_chat.Client.OnMessageReceived += Client_OnMessageReceivedAsync;
			_chat.Client.OnUserBanned += Client_OnUserBanned;
			_chat.Client.OnUserTimedout += Client_OnUserTimedout;
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

		private async void Client_OnUserBanned(object? sender, OnUserBannedArgs e)
		{
			await _api.GetOrCreateViewerById(e.UserBan.TargetUserId);
			using (BodyguardDbContext db = new())
			{
				TwitchBan ban = new TwitchBan(_streamerId, e.UserBan.TargetUserId, e.UserBan.BanReason);
				db.TwitchBans.Add(ban);
				db.SaveChanges();
			}
		}

		private async void Client_OnUserTimedout(object? sender, OnUserTimedoutArgs e)
		{
			TwitchViewer? viewer = await _api.GetOrCreateViewerByUsername(e.UserTimeout.Username);
			if (viewer != null)
			{
				using (BodyguardDbContext db = new())
				{
					TwitchTimeout timeout = new TwitchTimeout(_streamerId, viewer.TwitchOwner, e.UserTimeout.TimeoutDuration, e.UserTimeout.TimeoutReason);
					db.TwitchTimeouts.Add(timeout);
					db.SaveChanges();
				}
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Stopping chat bot for " + _streamerName);
			_chat.Client.OnMessageReceived -= Client_OnMessageReceivedAsync;
			_chat.Client.OnUserBanned -= Client_OnUserBanned;
			_chat.Client.OnUserTimedout -= Client_OnUserTimedout;
			return Task.CompletedTask;
		}
	}
}
