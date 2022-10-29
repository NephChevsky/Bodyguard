using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class TwitchMessage : IDateTimeTrackable, ITwitchOwnable
	{
		public Guid Id { get; set; }
		public string TwitchOwner { get; set; }
		public string Channel { get; set; }
		public string Message { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime? LastModificationDateTime { get; set; }

		public TwitchMessage()
		{
			Channel = string.Empty;
			TwitchOwner = string.Empty;
			Message = string.Empty;
			CreationDateTime = DateTime.Now;
		}

		public TwitchMessage(string channel, string userId, string userMessage)
		{
			Channel = channel;
			TwitchOwner = userId;
			Message = userMessage;
			CreationDateTime = DateTime.Now;
		}
	}
}
