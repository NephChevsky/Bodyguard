using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class TwitchUser : IDateTimeTrackable, ITwitchOwnable
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string TwitchOwner { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime? LastModificationDateTime { get; set; }

		public TwitchUser()
		{
			Id = Guid.NewGuid();
			Name = string.Empty;
			DisplayName = string.Empty;
			TwitchOwner = string.Empty;
			CreationDateTime = DateTime.Now;
		}

		public TwitchUser(string id, string name, string displayName)
		{
			Id = Guid.NewGuid();
			Name = name;
			DisplayName = displayName;
			TwitchOwner = id;
			CreationDateTime = DateTime.Now;
		}
	}
}
