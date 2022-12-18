using Db;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using TwitchLib.Api.Helix.Models.Charity.GetCharityCampaign;

namespace TwitchBotManager
{
	internal class StreamerChecker : BackgroundService
	{
		private Settings _settings;
		private readonly ILogger<StreamerChecker> _logger;
		private TwitchApi.TwitchApi _api;
		private DockerClient _dockerClient;

		public StreamerChecker(IConfiguration configuration, ILogger<StreamerChecker> logger, TwitchApi.TwitchApi api)
		{
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_logger = logger;
			_api = api;
			_dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Bot manager started successfully");
			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					using (BodyguardDbContext db = new())
					{
						await FindNewStreamers(new List<string> { "fr" }, new List<string> { "30921" });

						List<TwitchStreamer> streamers = db.TwitchStreamers.ToList();

						List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = await GetTwitchStreams(streamers);

						streams = streams.Where(x => x.GameId == "30921").ToList();
						await StartAndStopInstances(streams);
					}

					await Task.Delay(60 * 1000, stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Service was asked to shut down");
			}

			_logger.LogInformation("Bot manager is stopping");

			await StartAndStopInstances(new List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>());

			_logger.LogInformation("Bot manager stopped successfully");
		}

		public async Task FindNewStreamers(List<string> languages, List<string> games)
		{
			List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = await _api.GetStreams(null, languages, games);
			streams = streams.Where(x => x.ViewerCount >= 20).ToList();
			foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams)
			{
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

		public async Task StartAndStopInstances(List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams)
		{
			IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters()
			{
				All = true
			});
			foreach(ContainerListResponse container in containers)
			{
				TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream = streams.Where(x => x.UserId == container.Names[0].Replace("/twitch-chat-parser-", "")).FirstOrDefault();
				if ( stream == null && container.State == "running")
				{
					await StopContainer(container.Names[0].Replace("/twitch-chat-parser-", ""));
				}
			}

			foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams)
			{
				ContainerListResponse container = containers.Where(x => x.Names.Contains($"/twitch-chat-parser-{stream.UserId}")).FirstOrDefault();
				if (container == null || container.State != "running")
				{
					if (container == null)
					{
						await CreateContainer(stream.UserId);
					}
					await StartContainer(stream.UserId);
					await Task.Delay(2000);
				}
			}
		}

		public async Task CreateContainer(string userId)
		{
			await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
			{
				Name = $"twitch-chat-parser-{userId}",
				Image = "twitch-chat-parser",
				Env = new List<string> { "TZ=Europe/Paris" },
				Cmd = new List<string> { userId },
				HostConfig = new HostConfig
				{
					Binds = new [] { @"c:/logs:/app/logs"}
				}
			});
		}

		public async Task StartContainer(string userId)
		{
			await _dockerClient.Containers.StartContainerAsync($"twitch-chat-parser-{userId}", new ContainerStartParameters());
		}

		public async Task StopContainer(string userId)
		{
			await _dockerClient.Containers.StopContainerAsync($"twitch-chat-parser-{userId}", new ContainerStopParameters());
		}
	}
}
