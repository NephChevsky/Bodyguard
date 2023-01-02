using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TwitchBotManager
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string projectName = Assembly.GetCallingAssembly().GetName().Name ?? "App";

			IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((hostingContext, configBuilder) =>
			{
				System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
				IConfigurationRoot config = configBuilder.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("config.json", false, false)
					.AddJsonFile("secret.json", false, false)
					.Build();
			})
			.ConfigureServices(services =>
			{
				services.Configure<HostOptions>(hostOptions =>
				{
					hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
				});

				services.AddSingleton<TwitchApi.TwitchApi>();
				services.AddHostedService<StreamerChecker>();

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