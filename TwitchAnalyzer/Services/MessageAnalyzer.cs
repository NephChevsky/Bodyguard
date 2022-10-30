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

namespace TwitchAnalyzer.Services
{
	internal class MessageAnalyzer : BackgroundService
	{
		private Settings _settings;
		private readonly ILogger _logger;

		public MessageAnalyzer()
		{
			_settings = new Settings().LoadSettings();
			var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
				.ClearProviders()
				.AddNLog("nlog.config"));
			_logger = loggerFactory.CreateLogger<MessageAnalyzer>();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("MessageAnalyzer running at: {time}", DateTimeOffset.Now);

				using (BodyguardDbContext db = new())
				{
					List<TwitchMessage> messages = db.TwitchMessages.Where(x => x.Sentiment == false).ToList();
					foreach (TwitchMessage message in messages)
					{
						// TODO: predict sentiment
					}
					db.SaveChanges();
				}

				await Task.Delay(60 * 1000, stoppingToken);
			}
		}
	}
}
