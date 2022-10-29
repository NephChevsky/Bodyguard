namespace Models
{
	public class Settings
	{
		public Twitch Twitch { get; set; } = new Twitch();

		public Settings()
		{
			Twitch = new Twitch();
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