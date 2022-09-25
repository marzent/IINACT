using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using System;

namespace RainbowMage.OverlayPlugin.EventSources {
    internal partial class MiniParseEventSource : EventSourceBase {
        // Part of ACTWebSocket
        // Copyright (c) 2016 ZCube; Licensed under MIT license.
        public enum MessageType {
            LogLine = 0,
            ChangeZone = 1,
            ChangePrimaryPlayer = 2,
            AddCombatant = 3,
            RemoveCombatant = 4,
            AddBuff = 5,
            RemoveBuff = 6,
            FlyingText = 7,
            OutgoingAbility = 8,
            IncomingAbility = 10,
            PartyList = 11,
            PlayerStats = 12,
            CombatantHP = 13,
            NetworkStartsCasting = 20,
            NetworkAbility = 21,
            NetworkAOEAbility = 22,
            NetworkCancelAbility = 23,
            NetworkDoT = 24,
            NetworkDeath = 25,
            NetworkBuff = 26,
            NetworkTargetIcon = 27,
            NetworkRaidMarker = 28,
            NetworkTargetMarker = 29,
            NetworkBuffRemove = 30,
            Debug = 251,
            PacketDump = 252,
            Version = 253,
            Error = 254,
            Timer = 255
        }

        private void LogLineReader(bool isImported, LogLineEventArgs e) {
            Log(LogLevel.Info, e.logLine);

            var d = e.logLine.Split('|');

            if (d == null || d.Length < 2) // DataErr0r: null or 1-section
            {
                return;
            }

            var type = (MessageType)Convert.ToInt32(d[0]);

            switch (type) {
                case MessageType.LogLine:
                    if (d.Length < 5) // Invalid
                    {
                        break;
                    }
                    var logType = Convert.ToInt32(d[2], 16);

                    if (logType == 56) // type:echo
                    {
                        sendEchoEvent(isImported, "echo", d[4]);
                    }
                    break;
            }
        }

        private void sendEchoEvent(bool isImported, string type, string text) {
            var message = new JObject();
            message["isImported"] = isImported;
            message["type"] = type;
            message["message"] = text;

            var e = new JObject();
            e["type"] = "LogLine";
            e["detail"] = message;

            DispatchEvent(e);
        }
    }
}
