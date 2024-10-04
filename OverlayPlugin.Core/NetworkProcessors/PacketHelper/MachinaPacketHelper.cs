using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper
{
    internal static class MachinaMap
    {
        public static readonly Type HeaderType_Global;
        public static readonly Type HeaderType_CN;
        public static readonly Type HeaderType_KR;

        public static readonly ReadOnlyDictionary<GameRegion, ReadOnlyDictionary<string, Type>> packetTypeMap;

        static MachinaMap()
        {
            var machina = Assembly.Load("Machina.FFXIV");
            var allMachinaTypes = machina.GetTypes();
            var globalDict = new Dictionary<string, Type>();
            var chineseDict = new Dictionary<string, Type>();
            var koreanDict = new Dictionary<string, Type>();

            foreach (var mType in allMachinaTypes)
            {
                var parentNamespaceName = mType.Namespace;

                // Wrong namespace
                if (!parentNamespaceName.StartsWith("Machina.FFXIV.Headers")) continue;
                // Not a struct or enum or primitive
                if (!mType.IsValueType) continue;
                // Check for enum
                if (mType.IsEnum) continue;
                // Check for primitive
                if (mType.IsPrimitive) continue;

                // Only allow structs that are fixed layout of some sort, this avoids potential exceptions when marshaling
                if (!mType.IsExplicitLayout && !mType.IsLayoutSequential) continue;

                // Don't allow inner/nested types.
                if (mType.IsNested) continue;

                switch (parentNamespaceName)
                {
                    case "Machina.FFXIV.Headers":
                        globalDict.Add(mType.Name, mType);
                        break;
                    case "Machina.FFXIV.Headers.Chinese":
                        chineseDict.Add(mType.Name, mType);
                        break;
                    case "Machina.FFXIV.Headers.Korean":
                        koreanDict.Add(mType.Name, mType);
                        break;
                }

                packetTypeMap = new ReadOnlyDictionary<GameRegion, ReadOnlyDictionary<string, Type>>(new Dictionary<GameRegion, ReadOnlyDictionary<string, Type>>() {
                    { GameRegion.Global, new ReadOnlyDictionary<string, Type>(globalDict) },
                    { GameRegion.Chinese, new ReadOnlyDictionary<string, Type>(chineseDict) },
                    { GameRegion.Korean, new ReadOnlyDictionary<string, Type>(koreanDict) },
                });

                MachinaPacketWrapper.InitTypePropertyMap(mType);
            }

            // Currently Machina uses the same message header for all regions. Allow for that to change in the future.
            HeaderType_Global = machina.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
            MachinaHeaderWrapper.InitTypePropertyMap(HeaderType_Global);
            HeaderType_CN = machina.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
            MachinaHeaderWrapper.InitTypePropertyMap(HeaderType_CN);
            HeaderType_KR = machina.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
            MachinaHeaderWrapper.InitTypePropertyMap(HeaderType_KR);
        }

        public static bool GetPacketType(GameRegion region, string name, out Type packetType)
        {
            packetType = null;
            if (!packetTypeMap.TryGetValue(region, out var map)) return false;
            if (!map.TryGetValue(name, out packetType))
            {
                if (!name.StartsWith("Server_"))
                {
                    if (map.TryGetValue("Server_" + name, out packetType)) return true;
                }
                return false;
            }

            return true;
        }
    }

    class MachinaRegionalizedPacketHelper<PacketType>
        where PacketType : MachinaPacketWrapper, new()
    {
        public readonly MachinaPacketHelper<PacketType> global;
        public readonly MachinaPacketHelper<PacketType> cn;
        public readonly MachinaPacketHelper<PacketType> kr;

        private MachinaRegionalizedPacketHelper(MachinaPacketHelper<PacketType> global, MachinaPacketHelper<PacketType> cn, MachinaPacketHelper<PacketType> kr)
        {
            this.global = global;
            this.cn = cn;
            this.kr = kr;
        }

        public static bool Create(string packetTypeName, out MachinaRegionalizedPacketHelper<PacketType> packetHelper, string packetOpcodeName = null)
        {
            packetOpcodeName = packetOpcodeName ?? packetTypeName;
            packetHelper = null;
            var opcodes = FFXIVRepository.GetMachinaOpcodes();
            if (opcodes == null)
            {
                return false;
            }

            if (!opcodes.TryGetValue(GameRegion.Global, out var globalOpcodes))
            {
                return false;
            }
            if (!opcodes.TryGetValue(GameRegion.Chinese, out var cnOpcodes))
            {
                return false;
            }
            if (!opcodes.TryGetValue(GameRegion.Korean, out var krOpcodes))
            {
                return false;
            }

            if (!MachinaMap.GetPacketType(GameRegion.Global, packetTypeName, out var globalPacketType))
            {
                return false;
            }
            if (!MachinaMap.GetPacketType(GameRegion.Chinese, packetTypeName, out var cnPacketType))
            {
                return false;
            }
            if (!MachinaMap.GetPacketType(GameRegion.Korean, packetTypeName, out var krPacketType))
            {
                return false;
            }

            if (!globalOpcodes.TryGetValue(packetOpcodeName, out var globalOpcode))
            {
                globalOpcode = 0;
            }
            if (!cnOpcodes.TryGetValue(packetOpcodeName, out var cnOpcode))
            {
                cnOpcode = 0;
            }
            if (!krOpcodes.TryGetValue(packetOpcodeName, out var krOpcode))
            {
                krOpcode = 0;
            }

            var global = new MachinaPacketHelper<PacketType>(globalOpcode, MachinaMap.HeaderType_Global, globalPacketType);
            var cn = new MachinaPacketHelper<PacketType>(cnOpcode, MachinaMap.HeaderType_CN, cnPacketType);
            var kr = new MachinaPacketHelper<PacketType>(krOpcode, MachinaMap.HeaderType_KR, krPacketType);

            packetHelper = new MachinaRegionalizedPacketHelper<PacketType>(global, cn, kr);

            return true;
        }

        public IPacketHelper this[GameRegion gameRegion]
        {
            get
            {
                switch (gameRegion)
                {
                    case GameRegion.Global: return global;
                    case GameRegion.Chinese: return cn;
                    case GameRegion.Korean: return kr;

                    default: return global;
                }
            }
        }
    }

    class MachinaPacketHelper<PacketType> : IPacketHelper
        where PacketType : MachinaPacketWrapper, new()
    {
        public readonly ushort Opcode;
        public readonly int headerSize;
        public readonly int packetSize;

        public readonly Type headerType;
        public readonly Type packetType;

        public MachinaPacketHelper(ushort opcode, Type headerType, Type packetType)
        {
            Opcode = opcode;
            headerSize = Marshal.SizeOf(headerType);
            // Machina packets include the header as part of the packet struct
            packetSize = Marshal.SizeOf(packetType) - headerSize;
            this.headerType = headerType;
            this.packetType = packetType;
        }

        /// <summary>
        /// Construct a string representation of a packet from a byte array
        /// </summary>
        /// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <returns>null for invalid packet, otherwise a constructed packet</returns>
        public string ToString(long epoch, byte[] message)
        {
            if (ToStructs(message, out var header, out var packet) == false)
            {
                return null;
            }

            return ToString(epoch, header, packet);
        }

        /// <summary>
        /// Construct a string representation of a packet from a byte array
        /// </summary>
        /// <param name="epoch">epoch timestamp from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <param name="message">Message byte array received from FFXIV_ACT_Plugin's NetworkReceivedDelegate</param>
        /// <returns>null for invalid packet, otherwise a constructed packet</returns>
        public string ToString(long epoch, MachinaHeaderWrapper header, MachinaPacketWrapper packet)
        {
            return packet.ToString(epoch, header.ActorID);
        }

        public unsafe bool ToStructs(byte[] message, out MachinaHeaderWrapper header, out PacketType packet)
        {
            // Message is too short to contain this packet
            if (message.Length < headerSize + packetSize)
            {
                header = null;
                packet = null;

                return false;
            }

            fixed (byte* messagePtr = message)
            {
                return ToStructs(messagePtr, out header, out packet);
            }
        }

        public unsafe bool ToStructs(void* message, out MachinaHeaderWrapper header, out PacketType packet)
        {
            var ptr = new IntPtr(message);
            return ToStructs(ptr, out header, out packet);
        }

        public unsafe bool ToStructs(IntPtr ptr, out MachinaHeaderWrapper header, out PacketType packet, bool ignoreOpcode = false)
        {
            var headerObj = Marshal.PtrToStructure(ptr, headerType);

            header = new MachinaHeaderWrapper(headerObj);

            if (!ignoreOpcode && header.Opcode != Opcode)
            {
                header = null;
                packet = null;

                return false;
            }

            var packetObj = Marshal.PtrToStructure(ptr, packetType);

            packet = new PacketType
            {
                packetType = packetType,
                packetValue = packetObj
            };

            return true;
        }
    }

    class MachinaHeaderWrapper : IHeaderStruct
    {
        public object header;

        private static Dictionary<Type, Dictionary<string, FieldInfo>> typePropertyMap = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        private Dictionary<string, FieldInfo> propMap = null;

        public MachinaHeaderWrapper(object header, Type headerType = null)
        {
            propMap = typePropertyMap[headerType ?? header.GetType()];
            this.header = header;
        }

        public static void InitTypePropertyMap(Type type)
        {
            // Account for multiple regions sharing the same header
            if (typePropertyMap.ContainsKey(type)) return;

            var dict = new Dictionary<string, FieldInfo>();
            foreach (var fieldInfo in type.GetFields())
            {
                dict.Add(fieldInfo.Name, fieldInfo);
            }
            typePropertyMap.Add(type, dict);
        }

        public T Get<T>(string name)
        {
            // For performance, we don't check that the dictionary entries exist here
            // The only times they would not exist are a compile-time error, or Machina itself removing/renaming a field/struct
            // which would require an OverlayPlugin version bump anyways
            return (T)propMap[name].GetValue(header);
        }

        public uint ActorID => Get<uint>("ActorID");

        public uint Opcode => Get<ushort>("MessageType");
    }

    abstract class MachinaPacketWrapper : IPacketStruct
    {
        public Type packetType;
        public object packetValue;

        private static Dictionary<Type, Dictionary<string, FieldInfo>> typePropertyMap = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        private Dictionary<string, FieldInfo> propMap = null;

        public static void InitTypePropertyMap(Type type)
        {
            var dict = new Dictionary<string, FieldInfo>();
            foreach (var fieldInfo in type.GetFields())
            {
                dict.Add(fieldInfo.Name, fieldInfo);
            }
            typePropertyMap.Add(type, dict);
        }

        public abstract string ToString(long epoch, uint ActorID);

        public T Get<T>(string name)
        {
            // For performance, we don't check that the dictionary entries exist here
            // The only times they would not exist are a compile-time error, or Machina itself removing/renaming a field/struct
            // which would require an OverlayPlugin version bump anyways

            // Cache the map locally for subsequent calls
            if (propMap == null) propMap = typePropertyMap[packetType];
            return (T)propMap[name].GetValue(packetValue);
        }
    }
}
