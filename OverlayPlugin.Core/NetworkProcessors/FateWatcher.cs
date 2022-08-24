using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace RainbowMage.OverlayPlugin.NetworkProcessors {
    public class FateWatcher {
        private ILogger logger;
        private string region_;

        // Fate start
        // param1: fateID
        // param2: unknown
        //
        // Fate end
        // param1: fateID
        //
        // Fate update
        // param1: fateID
        // param2: progress (0-100)
        private struct ACSelfOPCodes {
            public ACSelfOPCodes(int add_, int remove_, int update_) { this.add = add_; this.remove = remove_; this.update = update_; }
            public int add;
            public int remove;
            public int update;
        };
        private static readonly ACSelfOPCodes acself_v5_2 = new ACSelfOPCodes(
          0x935,
          0x936,
          0x93E
        );

        private struct CEDirectorOPCodes {
            public CEDirectorOPCodes(int size_, int opcode_) { this.size = size_; this.opcode = opcode_; }
            public int size;
            public int opcode;
        }

        private static readonly CEDirectorOPCodes cedirector_ko = new CEDirectorOPCodes(
      0x30,
      0x223
    );

        private static readonly CEDirectorOPCodes cedirector_cn = new CEDirectorOPCodes(
          0x30,
          0x1AD
        );

        private static readonly CEDirectorOPCodes cedirector_intl = new CEDirectorOPCodes(
          0x30,
          0x108
        );

        private struct ActorControlSelf {
            public ActorControlSelf(Type messagetype_, Assembly assembly_, NetworkParser netHelper) {
                packetType = assembly_.GetType("Machina.FFXIV.Headers.Server_ActorControlSelf");
                if (packetType != null) {
                    // FFXIV_ACT_Plugin version >= 2.6.2.0
                    size = Marshal.SizeOf(packetType);
                    categoryOffset = netHelper.GetOffset(packetType, "category");
                    param1Offset = netHelper.GetOffset(packetType, "param1");
                    param2Offset = netHelper.GetOffset(packetType, "param2");
                    opCode = netHelper.GetOpcode("ActorControlSelf");
                } else {
                    // FFXIV_ACT_Plugin version < 2.6.2.0
                    packetType = assembly_.GetType("Machina.FFXIV.Headers.Server_ActorControl143");
                    size = Marshal.SizeOf(packetType);
                    categoryOffset = netHelper.GetOffset(packetType, "category");
                    param1Offset = netHelper.GetOffset(packetType, "param1");
                    param2Offset = netHelper.GetOffset(packetType, "param2");
                    opCode = netHelper.GetOpcode("ActorControl143");
                }
            }
            public Type packetType;
            public int size;
            public int categoryOffset;
            public int param1Offset;
            public int param2Offset;
            public int opCode;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct CEDirectorData {

            [FieldOffset(0x20)]
            public uint popTime;
            [FieldOffset(0x24)]
            public ushort timeRemaining;
            [FieldOffset(0x28)]
            public byte ceKey;
            [FieldOffset(0x29)]
            public byte numPlayers;
            [FieldOffset(0x2A)]
            public byte status;
            [FieldOffset(0x2C)]
            public byte progress;
        };

        private static SemaphoreSlim fateSemaphore;
        private static SemaphoreSlim ceSemaphore;
        private Dictionary<string, ACSelfOPCodes> acselfopcodes = null;
        private Dictionary<string, CEDirectorOPCodes> cedirectoropcodes = null;

        private Type MessageType = null;
        private Type messageHeader = null;
        public int headerOffset = 0;
        public int messageTypeOffset = 0;
        private ActorControlSelf actorControlself;

        // fates<fateID, progress>
        private static Dictionary<int, int> fates;
        private static Dictionary<int, CEDirectorData> ces;

        public event EventHandler<FateChangedArgs> OnFateChanged;

        public FateWatcher(TinyIoCContainer container) {
            logger = container.Resolve<ILogger>();
            var ffxiv = container.Resolve<FFXIVRepository>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            var language = ffxiv.GetLocaleString();

            if (language == "ko")
                region_ = "ko";
            else if (language == "cn")
                region_ = "cn";
            else
                region_ = "intl";

            fateSemaphore = new SemaphoreSlim(0, 1);
            ceSemaphore = new SemaphoreSlim(0, 1);
            acselfopcodes = new Dictionary<string, ACSelfOPCodes>();
            acselfopcodes.Add("ko", acself_v5_2);
            acselfopcodes.Add("cn", acself_v5_2);
            acselfopcodes.Add("intl", acself_v5_2);

            cedirectoropcodes = new Dictionary<string, CEDirectorOPCodes>();
            cedirectoropcodes.Add("ko", cedirector_ko);
            cedirectoropcodes.Add("cn", cedirector_cn);
            cedirectoropcodes.Add("intl", cedirector_intl);

            fates = new Dictionary<int, int>();
            ces = new Dictionary<int, CEDirectorData>();

            var netHelper = container.Resolve<NetworkParser>();
            var mach = Assembly.Load("Machina.FFXIV");
            MessageType = mach.GetType("Machina.FFXIV.Headers.Server_MessageType");

            actorControlself = new ActorControlSelf(MessageType, mach, netHelper);
            headerOffset = netHelper.GetOffset(actorControlself.packetType, "MessageHeader");
            messageHeader = actorControlself.packetType.GetField("MessageHeader").FieldType;
            messageTypeOffset = headerOffset + netHelper.GetOffset(messageHeader, "MessageType");
            ffxiv.RegisterNetworkParser(MessageReceived);
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message) {
            if (message.Length < actorControlself.size && message.Length < cedirectoropcodes[region_].size)
                return;

            fixed (byte* buffer = message) {
                if (*(ushort*)&buffer[messageTypeOffset] == actorControlself.opCode) {
                    ProcessActorControl143(buffer, message);
                    return;
                }
                if (cedirectoropcodes.ContainsKey(region_)) {
                    if (*(ushort*)&buffer[messageTypeOffset] == cedirectoropcodes[region_].opcode) {
                        ProcessCEDirector(buffer, message);
                        return;
                    }
                }
            }
        }

        public unsafe void ProcessActorControl143(byte* buffer, byte[] message) {
            int a = *(ushort*)&buffer[actorControlself.categoryOffset];

            fateSemaphore.WaitAsync();
            try {
                if (a == acselfopcodes[region_].add) {
                    AddFate(*(int*)&buffer[actorControlself.param1Offset]);
                } else if (a == acselfopcodes[region_].remove) {
                    RemoveFate(*(int*)&buffer[actorControlself.param1Offset]);
                } else if (a == acselfopcodes[region_].update) {
                    var param1 = *(int*)&buffer[actorControlself.param1Offset];
                    var param2 = *(int*)&buffer[actorControlself.param2Offset];
                    if (!fates.ContainsKey(param1)) {
                        AddFate(param1);
                    }
                    try {
                        if (fates[param1] != param2)
                            UpdateFate(param1, param2);
                    }
                    catch (KeyNotFoundException) {
                        AddFate(param1);
                    }
                }
            }
            finally {
                fateSemaphore.Release();
            }
}

public unsafe void ProcessCEDirector(byte* buffer, byte[] message) {
    var data = *(CEDirectorData*)&buffer[0];

    ceSemaphore.WaitAsync();
    try {
        if (data.status != 0 && !ces.ContainsKey(data.ceKey)) {
            AddCE(data);
            return;
        } else {

            // Don't update if key is about to be removed
            if (!ces[data.ceKey].Equals(data) &&
              data.status != 0) {
                UpdateCE(data.ceKey, data);
                return;
            }

            // Needs removing
            if (data.status == 0) {
                RemoveCE(data);
                return;
            }
        }
    }
    finally {
        ceSemaphore.Release();
    }
}

private void AddCE(CEDirectorData data) {
    ces.Add(data.ceKey, data);
    // TODO
    // client_.DoCEEvent(new JSEvents.CEEvent("add", JObject.FromObject(data)));
}

private void RemoveCE(CEDirectorData data) {
    if (ces.ContainsKey(data.ceKey)) {
        // TODO
        // client_.DoCEEvent(new JSEvents.CEEvent("remove", JObject.FromObject(data)));
        ces.Remove(data.ceKey);
    }
}
private void UpdateCE(byte ceKey, CEDirectorData data) {
    ces[data.ceKey] = data;
    // TODO
    // client_.DoCEEvent(new JSEvents.CEEvent("update", JObject.FromObject(data)));
}

public void RemoveAndClearCEs() {
    foreach (var ceKey in ces.Keys) {
        // TODO
        // client_.DoCEEvent(new JSEvents.CEEvent("remove", JObject.FromObject(ces[ceKey])));
    }
    ces.Clear();
}

private void AddFate(int fateID) {
    if (!fates.ContainsKey(fateID)) {
        fates[fateID] = 0;
        OnFateChanged(null, new FateChangedArgs("add", fateID, 0));
    }
}

private void RemoveFate(int fateID) {
    if (fates.ContainsKey(fateID)) {
        OnFateChanged(null, new FateChangedArgs("remove", fateID, fates[fateID]));
        fates.Remove(fateID);
    }
}

private void UpdateFate(int fateID, int progress) {
    fates[fateID] = progress;
    OnFateChanged(null, new FateChangedArgs("update", fateID, progress));
}

public void RemoveAndClearFates() {
    foreach (var fateID in fates.Keys) {
        OnFateChanged(null, new FateChangedArgs("remove", fateID, fates[fateID]));
    }
    fates.Clear();
}

public class FateChangedArgs : EventArgs {
    public string eventType { get; private set; }
    public int fateID { get; private set; }
    public int progress { get; private set; }

    public FateChangedArgs(string eventType, int fateID, int progress) : base() {
        this.eventType = eventType;
        this.fateID = fateID;
        this.progress = progress;
    }
}
    }
}
