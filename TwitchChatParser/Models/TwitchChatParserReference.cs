using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChatParser.Models
{
	internal class TwitchChatParserReference
	{
		public string Id { get; set; }
		public Task Task { get; set; }
		public CancellationTokenSource Cts { get; set; }

		public TwitchChatParserReference(string id, Task task, CancellationTokenSource cts)
		{
			Id = id;
			Task = task;
			Cts = cts;
		}

		public void Stop()
		{
			Cts.Cancel();
			Task.Wait();
		}
	}
}
