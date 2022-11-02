using Db;
using Models.Db;
using SentimentAnalysis.Models.Db;

namespace SentimentAnalysis
{
	internal class Program
	{
		static void Main(string[] args)
		{
			switch(args[0])
			{
				case "pull-production":
					PullProduction();
					break;
				default:
					Console.WriteLine("No arguments");
					break;
			}
		}

		public static void PullProduction()
		{
			Console.WriteLine("Starting export");
			List<TwitchSample> samples = new List<TwitchSample>();
			using (BodyguardDbContext db = new())
			{
				List<TwitchMessage> messages = db.TwitchMessages.OrderBy(x => x.Channel).ThenBy(x => x.CreationDateTime).ToList();
				foreach (TwitchMessage message in messages)
				{
					samples.Add(new TwitchSample(message.Message, message.Sentiment ?? true));
				}
			}
			Console.WriteLine("Export done");

			Console.WriteLine($"Starting import");
			using (MLDbContext db = new())
			{
				foreach (TwitchSample sample in samples)
				{
					TwitchSample? oldRecord = db.TwitchSamples.Where(x => x.Message == sample.Message).FirstOrDefault();
					if (oldRecord == null)
					{
						db.TwitchSamples.Add(sample);
					}
				}
				db.SaveChanges();
			}
			Console.WriteLine("Import done");
		}
	}
}