using Db;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using Models.Db;
using System.Runtime.InteropServices;
using System;
using System.ComponentModel;
using TwitchLib.Api.Helix;

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
			Uri uri = new Uri("npipe://./pipe/docker_engine");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				uri = new Uri("unix:///var/run/docker.sock");
			}
			_dockerClient = new DockerClientConfiguration(uri).CreateClient();
			
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
						List<string> gamesId = new List<string> { "30921" };
						List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = await FindNewStreamers(new List<string> { "fr" }, gamesId);
						streams.AddRange(await FindNewStreamers(new List<string> { "fr" }));
						List<string> streamerIds = streams.Where(x => x.ViewerCount >= 10).Select(x => x.UserId).Distinct().ToList();
						await StartAndStopInstances(streamerIds);
					}

					await Task.Delay(60 * 1000, stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Service was asked to shut down");
			}

			_logger.LogInformation("Bot manager is stopping");
			await StartAndStopInstances(new List<string>());
			_logger.LogInformation("Bot manager stopped successfully");
		}

		public async Task<List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream>> FindNewStreamers(List<string> languages, List<string> games = null)
		{
			List<TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream> streams = await _api.GetStreams(null, languages, games);
			foreach (TwitchLib.Api.Helix.Models.Streams.GetStreams.Stream stream in streams)
			{
				await _api.GetOrCreateStreamerByUsername(stream.UserLogin);
			}

			return streams;
		}

		public async Task StartAndStopInstances(List<string> streamerIds)
		{
			IList<ContainerListResponse> containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters()
			{
				All = true,
				Filters = new Dictionary<string, IDictionary<string, bool>>
				{
					{
						"name",
						new Dictionary<string, bool>
						{
							{ "twitch-chat-parser-*", true}
						}
					}
				}
			});

			int startedInstances = containers.Where(x => x.State == "running").ToList().Count;

			await Task.WhenAll(containers.Select(async container => {
				string streamerId = streamerIds.Where(x => x == container.Names[0].Replace("/twitch-chat-parser-", "")).FirstOrDefault();
				if (streamerId == null && container.State == "running")
				{
					await StopContainer(container.Names[0].Replace("/twitch-chat-parser-", ""));
					startedInstances--;
				}
			}));
			
			foreach (string streamerId in streamerIds)
			{
				ContainerListResponse container = containers.Where(x => x.Names[0] == $"/twitch-chat-parser-{streamerId}").FirstOrDefault();
				if (startedInstances < _settings.Twitch.MaxBotInstances && (container == null || container.State != "running"))
				{
					if (container == null)
					{
						await CreateContainer(streamerId);
					}
					await StartContainer(streamerId);
					await Task.Delay(600);
					startedInstances++;
				}
			}
		}

		public async Task CreateContainer(string userId)
		{
			await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
			{
				Name = $"twitch-chat-parser-{userId}",
				Image = "twitch-chat-parser",
				Cmd = new List<string> { userId },
				HostConfig = new HostConfig
				{
					Binds = new [] { @"c:/logs:/app/logs"},
					Memory = 150000000
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
