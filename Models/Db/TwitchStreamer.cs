using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class TwitchStreamer : TwitchUser
	{
		public TwitchStreamer() : base()
		{
		}

		public TwitchStreamer(string id, string name, string displayName) : base(id, name, displayName)
		{

		}
	}
}
