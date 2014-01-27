using Mycroft.App;
using System;
using System.Collections.Generic;
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

        public MusicClient() : base()
        {
            stt = new Dictionary<string, string>();
            speakers = new Dictionary<string, string>();
            status = "down";
        }

        protected async override void Response(Mycroft.App.Message.APP_DEPENDENCY type, dynamic message)
        {
            if (message.ContainsKey("stt"))
            {
                var new_stt = message["stt"];
                foreach (var kv in new_stt)
                {
                    stt[kv.Key] = kv.Value;
                }
            }
            if (message.ContainsKey("audioOutput"))
            {
                var new_speakers = message["audioOutput"];
                foreach (var kv in new_speakers)
                {
                    speakers[kv.Key] = kv.Value;
                }
            }
            if (status == "down" && stt.ContainsKey("stt1") && stt["stt1"] == "up" && speakers.ContainsKey("speakers") && speakers["speakers"] == "up")
            {
                await SendData("APP_UP", "");
            }
            else if (status == "up" && (stt["stt1"] == "down" || speakers["speakers"] == "down"))
            {
                await SendData("APP_DOWN", "");
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
