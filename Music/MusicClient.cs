using Mycroft.App;
using Mycroft.App.Message;
using NAudio.Wave;
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
        private WaveOut waveOut;
        private BufferedWaveProvider waveProvider;
        private string audioStatus = "";
        private List<Track> queue;

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
            session.EndOfTrack += session_EndOfTrack;
            //session.StartPlayback += session_StartPlayback;
            //session.StopPlayback += session_StopPlayback;

            waveOut = new WaveOut();
            waveProvider = new BufferedWaveProvider(new WaveFormat());
            waveOut.Init(waveProvider);
            waveOut.Play();

        }

        protected async override void Response(APP_DEPENDENCY type, dynamic message)
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
                    await SendJson("MSG_QUERY", new MessageQuery("stt", "load_grammar", new { grammar = new { name = "music", xml = grammar } }, new string[] { }, 30));
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

        protected override void Response(APP_MANIFEST_OK type, dynamic message)
        {
            InstanceId = message["instanceId"];
            Console.WriteLine("Recieved: " + type);
        }

        protected async override void Response(MSG_BROADCAST type, dynamic message)
        {
            var content = message["content"];
            if (content["grammar"] == "music")
            {
                var tags = content["tags"];
                if (tags.ContainsKey("action"))
                {
                    if (tags["action"] == "play" || audioStatus == "paused")
                    {
                        waveOut.Play();
                        session.PlayerPlay();
                        audioStatus = "playing";
                    }
                    else if (tags["action"] == "pause" || audioStatus == "playing")
                    {
                        session.PlayerPause();
                        waveOut.Pause();
                        audioStatus = "paused";
                    }
                }
                else
                {
                    string query = tags["media"];
                    if (tags["type"] == "song")
                    {
                        Search search = await session.SearchTracks(query, 0, 1);
                        Track track = await search.Tracks[0].GetPlayable();
                        //listener = new TcpListener(port);
                        //listener.Start();
                        await SendJson("MSG_QUERY", new MessageQuery("audioOutput", "stream_spotify", new { port = port, ip = ipAddress }, new string[] { "speakers" }, 30));
                        //client = listener.AcceptTcpClient();
                        session.Play(track);
                        audioStatus = "playing";
                        queue.In
                    }
                }
            }
        }

        private void session_MusicDelivered(Session sender, MusicDeliveryEventArgs e)
        {
            //e.ConsumedFrames = e.Frames;
            //var stream = client.GetStream();
            //stream.Write(e.Samples, 0, e.Samples.Length);
            try
            {
                waveProvider.AddSamples(e.Samples, 0, e.Samples.Length);
                e.ConsumedFrames = e.Frames;
            }
            catch
            {

            }
        }

        private void session_EndOfTrack(Session sender, SessionEventArgs e)
        {
            if (queue.Count() != 0)
            {
                var track = queue[0];
                queue.RemoveAt(0);
                session.PlayerUnload();
                session.PlayerLoad(track);
                session.PlayerPlay();
            }
        }

    }
}
