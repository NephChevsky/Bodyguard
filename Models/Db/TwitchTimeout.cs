using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class TwitchTimeout : ITwitchOwnable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public string TwitchOwner { get; set; }
		public string Channel { get; set; }
		public int TimeoutDuration { get; set; }
		public string? TimeoutReason { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime? LastModificationDateTime { get; set; }

		public TwitchTimeout()
		{
			TwitchOwner = string.Empty;
			Channel = string.Empty;
			TimeoutDuration = 0;
			TimeoutReason = string.Empty;
		}

		public TwitchTimeout(string channel, string userId, int timeoutDuration, string timeoutReason)
		{
			TwitchOwner = channel;
			Channel = userId;
			TimeoutDuration = timeoutDuration;
			TimeoutReason = timeoutReason;
		}
	}
}
