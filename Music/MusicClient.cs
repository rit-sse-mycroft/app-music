using Mycroft.App;
using Mycroft.App.Message;
using SpotiFire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    class MusicClient : Client
    {
        private Dictionary<string, string> stt;
        private Dictionary<string, string> speakers;
        private string status;
        private bool sentGrammar;
        private string grammar;
        private Session session;
        private TcpClient client;
        private int port;
        private string ipAddress;
        private TcpListener listener;

        public MusicClient(Session session) : base()
        {
            stt = new Dictionary<string, string>();
            speakers = new Dictionary<string, string>();
            status = "down";
            sentGrammar = false;
            var reader = new StreamReader("grammar.xml");
            grammar = reader.ReadToEnd();
            this.session = session;
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                ipAddress = stream.ReadToEnd();
            }

            //Search for the ip in the html
            int first = ipAddress.IndexOf("Address: ") + 9;
            int last = ipAddress.LastIndexOf("</body>");
            ipAddress = ipAddress.Substring(first, last - first);
            port = 6666;
            session.MusicDelivered += session_MusicDelivered;
            session.StartPlayback += session_StartPlayback;
            session.StopPlayback += session_StopPlayback;
        }

        protected async override void Response(Mycroft.App.Message.APP_DEPENDENCY type, dynamic message)
        {
            if (message.ContainsKey("stt"))
            {
                var newStt = message["stt"];
                foreach (var kv in newStt)
                {
                    stt[kv.Key] = kv.Value;
                }
            }
            if (message.ContainsKey("audioOutput"))
            {
                var newSpeakers = message["audioOutput"];
                foreach (var kv in newSpeakers)
                {
                    speakers[kv.Key] = kv.Value;
                }
            }
            if (status == "down" && stt.ContainsKey("stt1") && stt["stt1"] == "up" && speakers.ContainsKey("speakers") && speakers["speakers"] == "up")
            {
                await SendData("APP_UP", "");
                if (!sentGrammar)
                {
                    await SendJson("MSG_QUERY", new MessageQuery("stt", "load_grammar", new { name = "music", xml = grammar }, new string[] { }, 30));
                    sentGrammar = true;
                }
            }
            else if (status == "up" && (stt["stt1"] == "down" || speakers["speakers"] == "down"))
            {
                await SendData("APP_DOWN", "");
                await SendJson("MSG_QUERY", new MessageQuery("stt", "unload_gramamr", new { grammar = "music" }, new string[] { }, 30));
                sentGrammar = false;
            }
        }

        protected override void Response(Mycroft.App.Message.APP_MANIFEST_OK type, dynamic message)
        {
            InstanceId = message["instanceId"];
            Console.WriteLine("Recieved: " + type);
        }

        protected async override void Response(Mycroft.App.Message.MSG_BROADCAST type, dynamic message)
        {
            if (message["grammar"]["name"] == "music")
            {
                var tags = message["grammar"]["tags"];
                string query = tags["media"];
                Search search = await session.SearchTracks(query, 0, 1);
            }
        }

        private void session_MusicDelivered(Session sender, MusicDeliveryEventArgs e)
        {
            var stream = client.GetStream();
            stream.Write(e.Samples, 0, e.Samples.Length);
        }

        private void session_StopPlayback(Session sender, SessionEventArgs e)
        {
            client.Close();
            listener.Stop();
        }

        private async void session_StartPlayback(Session sender, SessionEventArgs e)
        {
            listener = new TcpListener(port);
            listener.Start();
            await SendJson("MSG_QUERY", new MessageQuery("audioOutput", "stream_tts", new { port = port, ip = ipAddress }, new string[] { "speakers" }, 30));
            client = listener.AcceptTcpClient();
        }

    }
}
