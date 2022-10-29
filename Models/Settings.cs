using Microsoft.Extensions.Configuration;

namespace Models
{
	public class Settings
	{
		public static Settings? Current { get; set; }

		public Twitch Twitch { get; set; }

		public Settings()
		{
			Twitch = new Twitch();
		}

		public Settings LoadSettings()
		{
			Twitch = new Twitch();
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
			IConfigurationRoot configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(pathSecret, false)
				.AddJsonFile(pathConfig, false)
				.Build();

			Current = configuration.GetSection("Settings").Get<Settings>();
			return Current;
		}
	}

	public class Twitch
	{
		public string BotName { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string RedirectUri { get; set; }

		public Twitch()
		{
			BotName = string.Empty;
			ClientId = string.Empty;
			ClientSecret = string.Empty;
			RedirectUri = string.Empty;
		}
	}
}