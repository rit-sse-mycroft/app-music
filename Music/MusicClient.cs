using Mycroft.App;
using Mycroft.App.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public MusicClient() : base()
        {
            stt = new Dictionary<string, string>();
            speakers = new Dictionary<string, string>();
            status = "down";
            sentGrammar = false;
            var reader = new StreamReader("grammar.xml")
            grammar = reader.ReadToEnd();
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

        protected override void Response(Mycroft.App.Message.MSG_BROADCAST type, dynamic message)
        {
            throw new NotImplementedException();
        }
    }
}
