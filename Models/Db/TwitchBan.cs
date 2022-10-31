using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class TwitchBan : ITwitchOwnable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public string TwitchOwner { get; set; }
		public string Channel { get; set; }
		public string? BanReason { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime? LastModificationDateTime { get; set; }

		public TwitchBan()
		{
			TwitchOwner = string.Empty;
			Channel = string.Empty;
			BanReason = string.Empty;
		}

		public TwitchBan(string channel, string userId, string banReason)
		{
			TwitchOwner = channel;
			Channel = userId;
			BanReason = banReason;
		}
	}
}
