using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System.Reflection;
using TwitchAnalyzer.Services;

namespace TwitchAnalyzer
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
					.AddJsonFile("config.json", false)
					.AddJsonFile("secret.json", false)
					.Build();
			})
			.ConfigureServices(services =>
			{
				services.Configure<HostOptions>(hostOptions =>
				{
					hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
				});

				services.AddHostedService<MessageAnalyzer>();

				services.AddLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddNLog("nlog.config");
					GlobalDiagnosticsContext.Set("appName", projectName);
				});
			})
			.UseWindowsService()
			.Build();

			host.Run();
		}
	}
}