using NLog;
using NLog.Extensions.Logging;
using System.Reflection;

namespace TwitchChatParser
{
	public class Program
	{
		public static void Main(string[] args)
		{
			string projectName = Assembly.GetCallingAssembly().GetName().Name ?? "App";

			var builder = Host.CreateDefaultBuilder(args);

			builder.ConfigureAppConfiguration((hostingContext, configBuilder) =>
			{
				System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
				IConfigurationRoot config = configBuilder.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("config.json", false, false)
					.AddJsonFile("secret.json", false, false)
					.Build();
			});

			builder.ConfigureServices(services =>
			{
				services.AddAuthorization();
				services.AddSingleton(new CommandLineArgs(args));
				services.AddSingleton<TwitchApi.TwitchApi>();
				services.AddHostedService<ChatParser>();
				services.AddLogging(logging =>
				{
					logging.ClearProviders();
					logging.AddNLog("nlog.config");
					GlobalDiagnosticsContext.Set("appName", $"{projectName}-{args[0]}");
				});
				services.AddControllers();
			});

			var app = builder.Build();

			app.Run();
		}
	}
}