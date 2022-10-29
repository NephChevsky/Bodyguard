using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TwitchChat
{
	internal class WebServer
	{
        private HttpListener listener;

        public WebServer(string uri)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(uri);
        }

        public async Task<string> Listen()
        {
            listener.Start();
            return await onRequest();
        }

        private async Task<string> onRequest()
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                using (var writer = new StreamWriter(resp.OutputStream))
                {
                    if (req.QueryString != null)
                    {
                        string[]? values = req.QueryString.GetValues("code");
                        if (values != null && values.Length != 0)
                        {
                            writer.WriteLine("Authorization started! Check your application!");
                            writer.Flush();
                            return values[0];
                        }
                    }
                    writer.WriteLine("No code found in query string!");
                    writer.Flush();
                }
            }
            return string.Empty;
        }
    }
}
