using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Interfaces
{
	public interface ISoftDeleteable
	{
		public bool Deleted { get; set; }
	}
}
