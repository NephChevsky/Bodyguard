namespace TwitchChatParser
{
	public class CommandLineArgs
	{
		public string StreamerId { get; set; }

		public CommandLineArgs(string[] args)
		{
			if (args.Length < 1)
			{
				throw new ArgumentException("Missing arguments to start the app");
			}
			StreamerId = args[0];
		}
	}
}
