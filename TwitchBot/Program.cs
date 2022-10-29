using TwitchBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System.Reflection;
using Db;
using Models.Db;

namespace TwitchBot
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string projectName = Assembly.GetCallingAssembly().GetName().Name ?? "App";

			IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((hostingContext, configBuilder) =>
			{
				string pathSecret = "secret.json";
				if (!File.Exists(pathSecret))
				{
					pathSecret = Path.Combine(@"D:\dev\Bodyguard", pathSecret);
				}
				string pathConfig = "config.json";
				if (!File.Exists(pathConfig))
				{
					pathConfig = Path.Combine(@"D:\dev\Bodyguard", pathConfig);
				}
				IConfigurationRoot config = configBuilder.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile(pathSecret, false)
					.AddJsonFile(pathConfig, false)
					.Build();
			})
			.ConfigureServices(services =>
			{
				services.Configure<HostOptions>(hostOptions =>
				{
					hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
				});

				services.AddSingleton<TwitchApi.TwitchApi>();

				using (BodyguardDbContext db = new())
				{
					List<TwitchStreamer> streamers = db.TwitchStreamers.ToList();
					foreach (TwitchStreamer streamer in streamers)
					{
						services.AddSingleton<IHostedService>(x => ActivatorUtilities.CreateInstance<Services.TwitchBot>(x, streamer.Name, streamer.TwitchOwner));
					}
				}

				services.AddLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddNLog("nlog.config");
					GlobalDiagnosticsContext.Set("appName", projectName);
				});
			})
			.Build();

			host.Run();
		}
	}
}