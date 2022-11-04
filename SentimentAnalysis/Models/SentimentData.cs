using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentimentAnalysis.Models
{
	internal class SentimentData
	{
		[LoadColumn(0)]
		public string SentimentText { get; set; }

		[LoadColumn(1), ColumnName("Label")]
		public bool Sentiment { get; set; }
	}
}
