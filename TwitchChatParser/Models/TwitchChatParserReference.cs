using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChatParser.Models
{
	internal class TwitchChatParserReference
	{
		public string UserId { get; set; }
		public Task Task { get; set; }
		public CancellationTokenSource Cts { get; set; }

		public TwitchChatParserReference(string userId, Task task, CancellationTokenSource cts)
		{
			UserId = userId;
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
