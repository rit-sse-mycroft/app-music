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
    enum AudioStatus { Playing, Paused, Stopped };
    class MusicClient : Client
    {
        private Dictionary<string, string> stt;
        private Dictionary<string, string> speakers;
        private string status;
        private bool sentGrammar;
        private string grammar;
        private Session session;
        private TcpClient client = null;
        private int port;
        private string ipAddress;
        private TcpListener listener = null;
        private AudioStatus audioStatus;
        private TrackManager queue;

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
                if (speakers["speakers"] == "down" && listener != null)
                {
                    listener.Stop();
                    listener = null;
                    client = null;
                }
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
                if (!tags.ContainsKey("type"))
                {
                    HandleCommands(tags);
                }
                else
                {
                    string query = tags["media"];
                    if (tags["type"] == "song")
                        HandleSongs(query, tags);
                    else if (tags["type"] == "album")
                        HandleAlbums(query,tags);
                }
            }
        }

        private void session_MusicDelivered(Session sender, MusicDeliveryEventArgs e)
        {
            e.ConsumedFrames = e.Frames;
            var stream = client.GetStream();
            stream.Write(e.Samples, 0, e.Samples.Length);
        }

        private void session_EndOfTrack(Session sender, SessionEventArgs e)
        {
            audioStatus = AudioStatus.Stopped;
            if (!queue.IsEmpty())
            {
                NextSong();
            }
        }

        private void NextSong()
        {
            Track track = queue.Dequeue();
            session.PlayerUnload();
            session.PlayerLoad(track);
            session.PlayerPlay();
            audioStatus = AudioStatus.Playing;
        }
        private void HandleCommands(dynamic tags)
        {
            if (tags["action"] == "play" && audioStatus == AudioStatus.Paused)
            {
                session.PlayerPlay();
                audioStatus = AudioStatus.Playing;
            }
            else if (tags["action"] == "pause" && audioStatus == AudioStatus.Playing)
            {
                session.PlayerPause();
                audioStatus = AudioStatus.Paused;
            }
            else if (tags["action"] == "clear queue")
            {
                queue.Clear();
            }
            else if (tags["action"] == "next")
            {
                if (!queue.IsEmpty())
                {
                    NextSong();
                }
            }
        }

        private async void HandleSongs(string query, dynamic tags)
        {
            Search search = await session.SearchTracks(query, 0, 1);
            if (search.TotalTracks != 0)
            {
                Track track = await search.Tracks[0];
                if (client == null)
                {
                    listener = new TcpListener(port);
                    listener.Start();
                    await SendJson("MSG_QUERY", new MessageQuery("audioOutput", "stream_spotify", new { port = port, ip = ipAddress }, new string[] { "speakers" }, 30));
                    client = listener.AcceptTcpClient();
                }
                if (tags["action"] == "play")
                {
                    session.Play(queue.PlayTrack(track));
                    audioStatus = AudioStatus.Playing;
                }
                else if (tags["action"] == "add")
                {
                    if (audioStatus == AudioStatus.Stopped)
                    {
                        session.Play(queue.PlayTrack(track));
                        audioStatus = AudioStatus.Playing;
                    }
                    else
                    {
                        queue.AddTrack(track);
                    }
                }
            }
        }

        private async void HandleAlbums(string query, dynamic tags)
        {
            Search search = await session.SearchAlbums(query, 0, 1);
            if (search.TotalAlbums != 0)
            {
                Album album = await search.Albums[0];
                if (client == null)
                {
                    listener = new TcpListener(port);
                    listener.Start();
                    await SendJson("MSG_QUERY", new MessageQuery("audioOutput", "stream_spotify", new { port = port, ip = ipAddress }, new string[] { "speakers" }, 30));
                    client = listener.AcceptTcpClient();
                }
                if (tags["action"] == "play")
                {
                    session.Play(await queue.PlayAlbum(album));
                    audioStatus = AudioStatus.Playing;
                }
                else if (tags["action"] == "add")
                {
                    if (audioStatus == AudioStatus.Stopped)
                    {
                        session.Play(await queue.PlayAlbum(album));
                        audioStatus = AudioStatus.Playing;
                    }
                    else
                    {
                        queue.AddAlbum(album);
                    }
                }
            }
        }
    }
}
