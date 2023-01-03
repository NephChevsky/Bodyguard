using Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Db
{
	public class Token : IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
		public bool Locked { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime? LastModificationDateTime { get; set; }

		public Token()
		{
			Id = Guid.NewGuid();
			Name = string.Empty;
			Value = String.Empty;
			Locked = false;
			CreationDateTime = new DateTime();
			LastModificationDateTime = null;
		}

		public Token(string name)
		{
			Id = Guid.NewGuid();
			Name = name;
			Value = String.Empty;
			Locked = false;
			CreationDateTime = new DateTime();
			LastModificationDateTime = null;
		}
	}
}
