using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class TwitchStreamer : ISoftDeleteable
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public bool Deleted { get; set; }

		public TwitchStreamer()
		{
			Id = Guid.NewGuid(); 
			Name = string.Empty;
			Deleted = false;
		}
	}
}
