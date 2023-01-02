using Db;
using Models;
using Models.Db;
using TwitchLib.Client.Events;

namespace TwitchChatParser
{
	public class ChatParser : IHostedService
	{
		private Settings _settings;
		private readonly ILogger<ChatParser> _logger;
		private TwitchChat.TwitchChat _chat;
		private TwitchApi.TwitchApi _api;

		private TwitchStreamer _streamer;
		private List<OnMessageClearedArgs> DeletedMessages;

		private Timer DeletedMessagesTimer;

		public ChatParser(IConfiguration configuration, ILogger<ChatParser> logger, TwitchApi.TwitchApi api, CommandLineArgs args)
		{
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_logger = logger;
			_api = api;

			DeletedMessages = new List<OnMessageClearedArgs>();

			using (BodyguardDbContext db = new())
			{
				_streamer = db.TwitchStreamers.Where(x => x.TwitchOwner == args.StreamerId).First();
			}
			_chat = new TwitchChat.TwitchChat(_streamer.Name);

			DeletedMessagesTimer = new(DeletePendingClearedMessages, false, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Starting chat bot for " + _streamer.Name);
			_chat.Client.OnMessageReceived += Client_OnMessageReceivedAsync;
			_chat.Client.OnUserBanned += Client_OnUserBanned;
			_chat.Client.OnUserTimedout += Client_OnUserTimedout;
			_chat.Client.OnMessageCleared += Client_OnMessageCleared;
			_chat.Client.OnConnectionError += Client_OnConnectionError;
			_chat.Connect();
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Stopping chat bot for " + _streamer.Name);
			_chat.Client.OnMessageReceived -= Client_OnMessageReceivedAsync;
			_chat.Client.OnUserBanned -= Client_OnUserBanned;
			_chat.Client.OnUserTimedout -= Client_OnUserTimedout;
			_chat.Client.OnMessageCleared -= Client_OnMessageCleared;
			_chat.Client.OnConnectionError -= Client_OnConnectionError;
			_chat.Disconnect();

			DeletePendingClearedMessages(true);
			return Task.CompletedTask;
		}

		private async void Client_OnMessageReceivedAsync(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
		{
			await _api.GetOrCreateViewerById(e.ChatMessage.UserId);
			using (BodyguardDbContext db = new())
			{
				TwitchMessage message = new TwitchMessage(_streamer.TwitchOwner, e.ChatMessage.UserId, Guid.Parse(e.ChatMessage.Id), e.ChatMessage.Message);
				db.Add(message);
				db.SaveChanges();
			}
		}

		private async void Client_OnUserBanned(object sender, OnUserBannedArgs e)
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

		private async void Client_OnUserTimedout(object sender, OnUserTimedoutArgs e)
		{
			TwitchViewer viewer = await _api.GetOrCreateViewerByUsername(e.UserTimeout.Username);
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

		private void Client_OnMessageCleared(object sender, OnMessageClearedArgs e)
		{
			DeletedMessages.Add(e);
		}

		private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
		{
			_logger.LogError($"Connection error triggered in chat bot for {_streamer.Name}: {e.Error.Message}");
		}

		private void DeletePendingClearedMessages(object state)
		{
			bool force = false;
			if (state != null && (bool) state == true)
			{
				force = true;
			}
			for (int i = 0; i < DeletedMessages.Count; i++)
			{
				OnMessageClearedArgs entry = DeletedMessages[i];
				bool removeEntry = false;
				using (BodyguardDbContext db = new())
				{
					TwitchMessage message = db.TwitchMessages.Where(x => x.TwitchMessageId == Guid.Parse(entry.TargetMessageId.ToUpper())).FirstOrDefault();
					if (message != null)
					{
						message.Sentiment = false;
						db.SaveChanges();
						removeEntry = true;
					}
					else
					{
						DateTime limit = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(entry.TmiSentTs)).DateTime, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));
						if (limit < DateTime.Now.AddMinutes(5) || force)
						{
							removeEntry = true;
							_logger.LogError($"Couldn't find message \"{entry.Message}\" ({entry.TargetMessageId}) in channel {entry.Channel}");
						}
					}
				}
				if (removeEntry)
				{
					DeletedMessages.Remove(entry);
					i--;
				}
			}
		}
	}
}
