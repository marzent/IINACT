using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud
{
    interface IEnmityHudMemory60 : IEnmityHudMemory { }

    class EnmityHudMemory60 : EnmityHudMemory, IEnmityHudMemory60
    {
        private const string enmityHudSignature = "48895C246048897C2470488B3D";
        private static readonly int[] enmityHudPointerPath = new int[] { 0x30, 0x58, 0x98, 0x20 };

        // Offsets from the enmityHudAddress to find various enmity HUD data structures.
        private const int enmityHudCountOffset = 4;
        private const int enmityHudEntryOffset = 16;

        public EnmityHudMemory60(TinyIoCContainer container)
            : base(container, enmityHudSignature, enmityHudPointerPath, enmityHudCountOffset, enmityHudEntryOffset, EnmityHudEntryMemory.Size)
        { }

        public override Version GetVersion()
        {
            return new Version(6, 0);
        }

        [StructLayout(LayoutKind.Explicit, Size = Size)]
        struct EnmityHudEntryMemory
        {
            public const int Size = 24;

            [FieldOffset(0x00)]
            public uint Unknown01;

            [FieldOffset(0x04)]
            public uint HPPercent;

            [FieldOffset(0x08)]
            public uint EnmityPercent;

            [FieldOffset(0x0C)]
            public uint CastPercent;

            [FieldOffset(0x10)]
            public uint ID;

            [FieldOffset(0x14)]
            public uint Unknown02;
        }


        protected override unsafe EnmityHudEntry GetEnmityHudEntryFromBytes(byte[] source, int num = 0)
        {
            if (num < 0) throw new ArgumentException();
            if (num > 8) throw new ArgumentException();

            fixed (byte* p = source)
            {
                EnmityHudEntryMemory mem = *(EnmityHudEntryMemory*)&p[num * EnmityHudEntryMemory.Size];

                EnmityHudEntry enmityHudEntry = new EnmityHudEntry()
                {
                    Order = num,
                    ID = mem.ID,
                    HPPercent = mem.HPPercent > 100 ? 0 : mem.HPPercent,
                    EnmityPercent = mem.EnmityPercent > 100 ? 0 : mem.EnmityPercent,
                    CastPercent = mem.CastPercent > 100 ? 0 : mem.CastPercent,
                };

                return enmityHudEntry;
            }
        }
    }
}
