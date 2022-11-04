using Db;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;
using Models.Db;
using SentimentAnalysis.Models;
using SentimentAnalysis.Models.Db;
using System.Text.RegularExpressions;
using static Microsoft.ML.DataOperationsCatalog;

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
				case "learn":
					Learn();
					break;
				default:
					Console.WriteLine("No arguments");
					break;
			}
		}

		public static void PullProduction()
		{
			Console.WriteLine("Starting to pull production");
			List<TwitchSample> samples = new List<TwitchSample>();

			using (MLDbContext mlDb = new())
			{
				DateTime creationDateTime = DateTime.MinValue;
				TwitchSample? latestRecord = mlDb.TwitchSamples.OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
				if (latestRecord != null)
				{
					creationDateTime = latestRecord.CreationDateTime;
				}

				using (BodyguardDbContext prodDb = new())
				{
					List<TwitchStreamer> streamers = prodDb.TwitchStreamers.ToList();
					
					int offset = 0;
					int bulkSize = 1000;
					int count = 0;
					do
					{
						List<TwitchMessage> messages = prodDb.TwitchMessages.Where(x => x.CreationDateTime > creationDateTime).OrderBy(x => x.CreationDateTime).Skip(offset).Take(bulkSize).ToList();
						count = messages.Count();
						offset += bulkSize;
						foreach (TwitchMessage message in messages)
						{
							TwitchStreamer streamer = streamers.Where(x => x.TwitchOwner == message.Channel).First();
							string newMessage = Regex.Replace(message.Message, "@?" + streamer.Name + "(?=\\s|$)", "{StreamerName}", RegexOptions.IgnoreCase);
							TwitchSample sample = new TwitchSample(newMessage, message.Sentiment ?? true, message.CreationDateTime);
							mlDb.TwitchSamples.Add(sample);
						}
						mlDb.SaveChanges();
					} while (count == bulkSize);
				}
			}
			
			Console.WriteLine("Production is imported into machine learning");
		}

		public static void Learn()
		{
			MLContext mlContext = new MLContext();
			DatabaseLoader loader = mlContext.Data.CreateDatabaseLoader<SentimentData>();

			IConfigurationRoot configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("secret.json", false)
				.Build();

			var connectionString = configuration.GetConnectionString("MLDbKey");
			string sqlCommand = "SELECT Message,Sentiment FROM TwitchSamples";

			DatabaseSource dbSource = new DatabaseSource(SqlClientFactory.Instance, connectionString, sqlCommand);

			IDataView data = loader.Load(dbSource);
			TrainTestData splitDataView = mlContext.Data.TrainTestSplit(data, 0.1);

			var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentData.SentimentText))
								.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));
			var model = estimator.Fit(splitDataView.TrainSet);

			IDataView predictions = model.Transform(splitDataView.TestSet);
			CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, "Label");

			Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
			Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
			Console.WriteLine($"F1Score: {metrics.F1Score:P2}");

			mlContext.Model.Save(model, data.Schema, @"D:\Dev\Bodyguard\SentimentAnalysis\model.zip");
		}
	}
}