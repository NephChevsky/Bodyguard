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
			System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
			IConfigurationRoot configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("config.json", false)
				.AddJsonFile("secret.json", false)
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