using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class TwitchNameChange : ITwitchOwnable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public string TwitchOwner { get; set; }
		public string Name { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime? LastModificationDateTime { get; set; }

		public TwitchNameChange()
		{
			TwitchOwner = string.Empty;
			Name = string.Empty;
		}

		public TwitchNameChange(string userId, string name)
		{
			TwitchOwner = string.Empty;
			Name = string.Empty;
		}
	}
}
