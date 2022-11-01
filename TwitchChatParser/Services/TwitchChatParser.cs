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
using TwitchLib.Communication.Events;

namespace TwitchChatParser.Services
{
	internal class TwitchChatParser
	{
		private Settings _settings;
		private readonly ILogger _logger;
		private TwitchChat.TwitchChat _chat;
		private TwitchApi.TwitchApi _api;

		private TwitchStreamer _streamer;
		private List<OnMessageClearedArgs> DeletedMessages;

		public TwitchChatParser(string streamerId)
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<TwitchChatParser>();
			_chat = new TwitchChat.TwitchChat();
			_api = new TwitchApi.TwitchApi("TwitchApi");

			DeletedMessages = new List<OnMessageClearedArgs>();

			using (BodyguardDbContext db = new())
			{
				_streamer = db.TwitchStreamers.Where(x => x.TwitchOwner == streamerId).First();
			}
		}

		private async void Client_OnMessageReceivedAsync(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
		{
			await _api.GetOrCreateViewerById(e.ChatMessage.UserId);
			using (BodyguardDbContext db = new())
			{
				TwitchMessage message = new TwitchMessage(_streamer.TwitchOwner, e.ChatMessage.UserId, Guid.Parse(e.ChatMessage.Id), e.ChatMessage.Message);
				db.Add(message);
				db.SaveChanges();
			}
		}

		private async void Client_OnUserBanned(object? sender, OnUserBannedArgs e)
		{
			await _api.GetOrCreateViewerById(e.UserBan.TargetUserId);
			using (BodyguardDbContext db = new())
			{
				TwitchBan ban = new TwitchBan(_streamer.TwitchOwner, e.UserBan.TargetUserId, e.UserBan.BanReason);
				db.TwitchBans.Add(ban);
				List<TwitchMessage> messages = db.TwitchMessages.Where(x => x.Channel == _streamer.TwitchOwner && x.TwitchOwner == e.UserBan.TargetUserId && x.CreationDateTime > DateTime.Now.AddMinutes(-10)).OrderByDescending(x => x.CreationDateTime).Take(5).ToList();
				foreach (TwitchMessage message in messages)
				{
					message.Sentiment = false;
				}
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
					TwitchTimeout timeout = new TwitchTimeout(_streamer.TwitchOwner, viewer.TwitchOwner, e.UserTimeout.TimeoutDuration, e.UserTimeout.TimeoutReason);
					db.TwitchTimeouts.Add(timeout);
					List<TwitchMessage> messages = db.TwitchMessages.Where(x => x.Channel == _streamer.TwitchOwner && x.TwitchOwner == viewer.TwitchOwner && x.CreationDateTime > DateTime.Now.AddMinutes(-10)).OrderByDescending(x => x.CreationDateTime).Take(5).ToList();
					foreach (TwitchMessage message in messages)
					{
						message.Sentiment = false;
					}
					db.SaveChanges();
				}
			}
		}

		private void Client_OnMessageCleared(object? sender, OnMessageClearedArgs e)
		{
			DeletedMessages.Add(e);
		}

		private void Client_OnConnectionError(object? sender, OnConnectionErrorArgs e)
		{
			_logger.LogError($"Connection error triggered in chat bot for {_streamer.Name}");
		}

		public async void ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Starting chat bot for " + _streamer.Name);
			_chat.Client.OnMessageReceived += Client_OnMessageReceivedAsync;
			_chat.Client.OnUserBanned += Client_OnUserBanned;
			_chat.Client.OnUserTimedout += Client_OnUserTimedout;
			_chat.Client.OnMessageCleared += Client_OnMessageCleared;
			_chat.Client.OnConnectionError += Client_OnConnectionError;
			_chat.Connect(_streamer.Name);
			
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					_logger.LogInformation($"Chat bot for {_streamer.Name} is still running");
					DeletePendingClearedMessages();
					await Task.Delay(60 * 1000, stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation($"Chat bot for {_streamer.Name} was asked to shut down");
			}

			_logger.LogInformation("Stopping chat bot for " + _streamer.Name);
			_chat.Client.OnMessageReceived -= Client_OnMessageReceivedAsync;
			_chat.Client.OnUserBanned -= Client_OnUserBanned;
			_chat.Client.OnUserTimedout -= Client_OnUserTimedout;
			_chat.Client.OnMessageCleared -= Client_OnMessageCleared;
			_chat.Disconnect();
		}

		private void DeletePendingClearedMessages()
		{
			for (int i = 0; i < DeletedMessages.Count; i++)
			{
				OnMessageClearedArgs entry = DeletedMessages[i];
				using (BodyguardDbContext db = new())
				{
					TwitchMessage? message = db.TwitchMessages.Where(x => x.TwitchMessageId == Guid.Parse(entry.TargetMessageId.ToUpper())).FirstOrDefault();
					if (message != null)
					{
						message.Sentiment = false;
						db.SaveChanges();
					}
					else
					{
						DateTime limit = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(entry.TmiSentTs)).DateTime, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
						if (limit < DateTime.Now.AddMinutes(5))
						{
							DeletedMessages.Remove(entry);
							_logger.LogError($"Couldn't find message \"{entry.Message}\" ({entry.TargetMessageId}) in channel {entry.Channel}");
						}
					}
				}
			}
		}
	}
}
