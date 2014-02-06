using Mycroft.App;
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
    /// <summary>
    /// Enum for the current status of audio
    /// </summary>
    enum AudioStatus { Playing, Paused, Stopped };
    /// <summary>
    /// The Music Client Claas
    /// </summary>
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

        /// <summary>
        /// Constructor for a music client
        /// </summary>
        /// <param name="session">The spotify session</param>
        /// <param name="manifest">The path to app manifest</param>
        public MusicClient(Session session, string manifest) : base(manifest)
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
            handler.On("APP_MANIFEST_OK", AppManifestOk);
            handler.On("APP_DEPENDENCY", AppDependency);
            handler.On("MSG_BROADCAST", MsgBroadcast);
        }

        #region Message Handlers
        /// <summary>
        /// Called when APP_DEPENDENCY is received
        /// </summary>
        /// <param name="message">the message received</param>
        protected async void AppDependency(dynamic message)
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
                await Up();
                if (!sentGrammar)
                {
                    await Query("stt", "load_grammar", new { grammar = new { name = "music", xml = grammar } });
                    sentGrammar = true;
                }
            }
            else if (status == "up" && (stt["stt1"] == "down" || speakers["speakers"] == "down"))
            {
                await Down();
                await Query("stt", "unload_gramamr", new { grammar = "music" });
                sentGrammar = false;
                if (speakers["speakers"] == "down" && listener != null)
                {
                    listener.Stop();
                    listener = null;
                    client = null;
                }
            }
        }

        /// <summary>
        /// Called when APP_MANIFEST_OK is received
        /// </summary>
        /// <param name="message">the message received</param>
        protected void AppManifestOk(dynamic message)
        {
            InstanceId = message["instanceId"];
        }

        /// <summary>
        /// Called when MSG_BROADCAST is received
        /// </summary>
        /// <param name="message">The message received</param>
        protected async override void MsgBroadcast(dynamic message)
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
        #endregion
        #region Music Handlers
        /// <summary>
        /// Called when music is delievered
        /// </summary>
        /// <param name="sender"The sender</param>
        /// <param name="e">The music being delievered</param>
        private void session_MusicDelivered(Session sender, MusicDeliveryEventArgs e)
        {
            e.ConsumedFrames = e.Frames;
            var stream = client.GetStream();
            stream.Write(e.Samples, 0, e.Samples.Length);
        }

        /// <summary>
        /// Called when a track is finished. 
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event args</param>
        private void session_EndOfTrack(Session sender, SessionEventArgs e)
        {
            audioStatus = AudioStatus.Stopped;
            if (!queue.IsEmpty())
            {
                NextSong();
            }
        }

        /// <summary>
        /// Plays the next song in the play queue
        /// </summary>
        private void NextSong()
        {
            Track track = queue.Dequeue();
            session.PlayerUnload();
            session.PlayerLoad(track);
            session.PlayerPlay();
            audioStatus = AudioStatus.Playing;
        }
        #endregion
        #region Helpers
        /// <summary>
        /// Helper for handling music player commands
        /// </summary>
        /// <param name="tags">The tags received from STT</param>
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

        /// <summary>
        /// Helper for handling song commands
        /// </summary>
        /// <param name="query">The song requested</param>
        /// <param name="tags">the matched tags from stt</param>
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
                    await Query("audioOutput", "stream_spotify", new { port = port, ip = ipAddress }, new string[] { "speakers" });
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

        /// <summary>
        /// Helper for handling album commands
        /// </summary>
        /// <param name="query">The album to search for</param>
        /// <param name="tags">The tags from STT</param>
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
                    await Query("audioOutput", "stream_spotify", new { port = port, ip = ipAddress }, new string[] { "speakers" });
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
        #endregion
    }
}
