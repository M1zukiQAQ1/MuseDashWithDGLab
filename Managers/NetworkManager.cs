using System;

using System.Collections.Generic;
using Newtonsoft.Json;
using WebSocketSharp;
using QRCoder;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TestMod.Managers
{
    public class NetworkManager
    {

        private string connectionId = "";
        private string targetWSId = "";
        private readonly WebSocket ws;

        // Random waves

        public NetworkManager(string ip)
        {
            ws = new(ip);
            
            ws.OnOpen += (sender, e) => MelonLogger.Msg("Connection Established");
            ws.OnMessage += (sender, e) =>
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<DGMessage<string>>(e.Data);
                    switch (response.type)
                    {
                        case "bind":
                            if (response.targetId?.Length == 0)
                            {
                                var bindAddress = $"https://www.dungeon-lab.com/app-download.php#DGLAB-SOCKET#{ip}" + response.clientId;
                                connectionId = response.clientId;
                                MelonLogger.Msg("Your connectionId is " + response.clientId);
                                MelonLogger.Msg("Create a QR code using " + bindAddress);
                                return;
                            }
                            else
                            {
                                if (response.clientId != connectionId)
                                {
                                    MelonLogger.Msg("Wrong targeId!" + response.message);
                                    return;
                                }
                                targetWSId = response.targetId;
                                MelonLogger.Msg("Connected, your targetId is " + targetWSId);
                            }
                            break;
                        case "heartbeat":
                            MelonLogger.Msg("Hearbeat received");
                            break;
                        default:
                            MelonLogger.Msg(ConvertToUt8(e.Data));
                            break;
                    }
                }
                catch
                {
                    
                }
            };
        }

        private string ConvertToUt8 (string str) {
            Encoding utf8 = Encoding.GetEncoding("iso-8859-1");
            byte[] btArr = utf8.GetBytes(str);
            return Encoding.UTF8.GetString(btArr);
        }

        public void Initialize()
        {
            ws.Connect();
            Main.FireEvent += Fire;
        }

        public void Fire(FireData fireData)
        {
            var strengthMsg = $"strength-{fireData.channel}+2+" + fireData.strength;
            var message = new DGMessage<int>(_type: 4, _targetId: targetWSId, _clientId: connectionId, _message: strengthMsg);

            string waveMessageStr;
            if (fireData.channel == 1) {
                waveMessageStr = "A: " + fireData.waveData;
            } else {
                waveMessageStr = "B: " + fireData.waveData;
            }
            string channel = fireData.channel == 1 ? "A" : "B";

            var waveMessage = new WaveData(_type: "clientMsg", _message: waveMessageStr, _message2: "", time: 1, _clientId: connectionId, _targetId: targetWSId, _channel: channel);

            string json = JsonConvert.SerializeObject(message);
            string waveJson = JsonConvert.SerializeObject(waveMessage);

            ws.Send(json);
            MelonLogger.Msg("Sent strength " + strengthMsg + " to server!");
            if (fireData.strength == 0) return;
            ws.Send(waveJson);
            MelonLogger.Msg("Sent waves data " + waveJson + " to server!");
        }

        public void ClearAB(int channelIndex) {
            var message = new DGMessage<int>(_type: 4, _clientId: connectionId, _targetId: targetWSId, _message: "clear-" + channelIndex);
            string json = JsonConvert.SerializeObject(message);
            ws.Send(json);
            MelonLogger.Msg("Cleared channel " + channelIndex);
        }
    }

    public class DGMessage<T>(T _type, string _clientId, string _targetId, string _message)
    {
        public string clientId = _clientId;
        public string targetId = _targetId;
        public string message = _message;
        public T type = _type;
    }

    public class WaveData(string _type, string _message, string _message2, float time, string _clientId, string _targetId, string _channel)
    {
        public string clientId = _clientId;
        public string targetId = _targetId;
        public string type = _type;
        public string message = _message;
        public string message2 = _message2;
        public float time = 1f;
        public string channel = _channel;
    }
}
