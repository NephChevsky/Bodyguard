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
			try {
				while (!stoppingToken.IsCancellationRequested)
				{
					using (BodyguardDbContext db = new())
					{
						List<TwitchStreamer> streamers = db.TwitchStreamers.ToList();
						List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = new List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>();
						while (streamers.Count > 0)
						{
							List<TwitchStreamer> streamersSubset = streamers.Take(100).ToList();
							streamers.RemoveRange(0, streamersSubset.Count);
							List<string> streamerIds = streamersSubset.Select(x => x.TwitchOwner).ToList();
							streams.AddRange(await _api.GetStreams(streamerIds));
						}
						for (int i = 0; i < Instances.Count; i++)
						{
							TwitchChatParserReference entry = Instances[i];
							if (streams.Where(x => x.UserId == entry.UserId).FirstOrDefault() == null)
							{
								entry.Stop();
								Instances.Remove(entry);
								i--;
							}
						}
						foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams)
						{
							if (Instances.Where(x => x.UserId == stream.UserId).FirstOrDefault() == null)
							{
								CancellationTokenSource cts = new CancellationTokenSource();
								Task instance = Task.Run(() =>
								{
									TwitchChatParser chatParser = new(stream.UserId);
									chatParser.ExecuteAsync(cts.Token);
								});
								Instances.Add(new TwitchChatParserReference(stream.UserId, instance, cts));
							}
						}
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
				entry.Stop();
			}

			_logger.LogInformation("TwitchChatParser stopped successfully");
		}
	}
}
