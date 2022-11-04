using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentimentAnalysis.Models.Db
{
	public class TwitchSample
	{
		public int Id { get; set; }
		public string Message { get; set; }
		public bool Sentiment { get; set; }
		public DateTime CreationDateTime { get; set; }

		public TwitchSample(string message, bool sentiment, DateTime creationDateTime)
		{
			Message = message;
			Sentiment = sentiment;
			CreationDateTime = creationDateTime;
		}
	}
}
