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
		public bool? Sentiment { get; set; }

		public TwitchSample()
		{
			Message = string.Empty;
			Sentiment = null;
		}

		public TwitchSample(string message, bool sentiment)
		{
			Message = message;
			Sentiment = sentiment;
		}
	}
}
