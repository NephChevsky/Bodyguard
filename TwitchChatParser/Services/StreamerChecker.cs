using Db;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchChatParser.Models;

namespace TwitchChatParser.Services
{
	internal class StreamerChecker : BackgroundService
	{
		private Settings _settings;
		private readonly ILogger<StreamerChecker> _logger;
		private TwitchApi.TwitchApi _api;

		private List<TwitchChatParserReference> Instances;

		public StreamerChecker(IConfiguration configuration, ILogger<StreamerChecker> logger, TwitchApi.TwitchApi api)
		{
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_logger = logger;
			_api = api;

			Instances = new List<TwitchChatParserReference>();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("StreamerChecker started successfully");
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					using (BodyguardDbContext db = new())
					{
						await FindNewStreamers();

						List<TwitchStreamer> streamers = db.TwitchStreamers.ToList();

						List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = await GetTwitchStreams(streamers);

						await RemoveDeadInstances(streams);

						await CreateNewInstances(streams);
					}

					await Task.Delay(60 * 1000, stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Service was asked to shut down");
			}

			_logger.LogInformation("TwitchChatParser is stopping");

			foreach (TwitchChatParserReference entry in Instances)
			{
				await entry.Stop();
			}

			_logger.LogInformation("TwitchChatParser stopped successfully");
		}

		public async Task FindNewStreamers()
		{
			List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = await _api.GetStreams(null, new List<string> { "fr" });
			streams = streams.OrderByDescending(x => x.ViewerCount).ToList();
			foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams)
			{
				if (stream.ViewerCount < 1000)
					break;
				await _api.GetOrCreateStreamerByUsername(stream.UserLogin);
			}
		}

		public async Task<List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>> GetTwitchStreams(List<TwitchStreamer> streamers)
		{
			List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = new ();
			while (streamers.Count > 0)
			{
				List<TwitchStreamer> streamersSubset = streamers.Take(100).ToList();
				streamers.RemoveRange(0, streamersSubset.Count);
				List<string> streamerIds = streamersSubset.Select(x => x.TwitchOwner).ToList();
				streams.AddRange(await _api.GetStreams(streamerIds));
			}
			return streams;
		}

		public async Task RemoveDeadInstances(List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams)
		{
			for (int i = 0; i < Instances.Count; i++)
			{
				TwitchChatParserReference entry = Instances[i];
				if (streams.Where(x => x.UserId == entry.UserId).FirstOrDefault() == null || entry.Task.IsCompleted)
				{
					await entry.Stop();
				}
			}

			for (int i = 0; i < Instances.Count; i++)
			{
				TwitchChatParserReference entry = Instances[i];
				if (entry.Task.IsCompleted)
				{
					Instances.Remove(entry);
					i--;
				}
			}
		}

		public async Task CreateNewInstances(List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams)
		{
			foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams)
			{
				if (Instances.Where(x => x.UserId == stream.UserId).FirstOrDefault() == null && (Instances.Count() < _settings.Twitch.MaxBotInstances || _settings.Twitch.MaxBotInstances == -1))
				{
					CancellationTokenSource cts = new CancellationTokenSource();
					Task instance = Task.Run(() =>
					{
						TwitchChatParser chatParser = new(_api, stream.UserId);
						chatParser.ExecuteAsync(cts.Token);
					});
					Instances.Add(new TwitchChatParserReference(stream.UserId, instance, cts));
					await Task.Delay(1000);
				}
			}
		}
	}
}
