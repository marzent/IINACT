using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud
{
    public abstract class EnmityHudMemory : IEnmityHudMemory
    {
        private FFXIVMemory memory;
        private ILogger logger;

        private IntPtr enmityHudAddress = IntPtr.Zero;
        private IntPtr enmityHudDynamicAddress = IntPtr.Zero;

        private string enmityHudSignature;
        private int[] enmityHudPointerPath;
        private int enmityHudCountOffset;
        private int enmityHudEntryOffset;
        private int enmityHudEntrySize;

        private DateTimeOffset lastDateTimeDynamicAddressChecked = DateTimeOffset.UtcNow;

        public EnmityHudMemory(TinyIoCContainer container, string enmityHudSignature, int[] enmityHudPointerPath, int enmityHudCountOffset, int enmityHudEntryOffset, int enmityHudEntrySize)
        {
            this.enmityHudSignature = enmityHudSignature;
            this.enmityHudPointerPath = enmityHudPointerPath;
            this.enmityHudCountOffset = enmityHudCountOffset;
            this.enmityHudEntryOffset = enmityHudEntryOffset;
            this.enmityHudEntrySize = enmityHudEntrySize;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        private void ResetPointers()
        {
            enmityHudAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (enmityHudAddress == IntPtr.Zero)
                return false;
            return true;
        }

        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                return false;

            return true;
        }

        public void ScanPointers()
        {
            ResetPointers();
            if (!memory.IsValid())
                return;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(enmityHudSignature, 0, true);
            if (list != null && list.Count == 1)
            {
                enmityHudAddress = list[0];

                if (enmityHudAddress == IntPtr.Zero)
                {
                    fail.Add(nameof(enmityHudAddress));
                }
            }
            else
            {
                enmityHudAddress = IntPtr.Zero;
                fail.Add(nameof(enmityHudAddress));
            }

            logger.Log(LogLevel.Debug, "enmityHudAddress: 0x{0:X}", enmityHudAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found enmity HUD memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find enmity HUD memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return;
        }

        public abstract Version GetVersion();

        private bool GetDynamicPointerAddress()
        {
            if (enmityHudAddress == IntPtr.Zero) return false;

            // Resolve Dynamic Pointers.
            // The static sigscan address fetched in GetPointerAddress is statically allocated.
            // However, the HUD entry is dynamically allocated every time the HUD is refreshed.
            // Walk down the pointer tree to the actual address we want, determined by enmityHudPointerPath.
            if (DateTimeOffset.UtcNow - lastDateTimeDynamicAddressChecked > TimeSpan.FromSeconds(30))
            {
                lastDateTimeDynamicAddressChecked = DateTimeOffset.UtcNow;
                var tmpEnmityHudDynamicAddress = memory.ReadIntPtr(enmityHudAddress);
                for (int i = 0; i < enmityHudPointerPath.Length; i++)
                {
                    var p = enmityHudPointerPath[i];
                    tmpEnmityHudDynamicAddress = tmpEnmityHudDynamicAddress + p;
                    tmpEnmityHudDynamicAddress = memory.ReadIntPtr(tmpEnmityHudDynamicAddress);
                    if (tmpEnmityHudDynamicAddress == IntPtr.Zero)
                    {
                        enmityHudDynamicAddress = IntPtr.Zero;
                        return false;
                    }
                }
                enmityHudDynamicAddress = new IntPtr(tmpEnmityHudDynamicAddress.ToInt64());
            }
            return true;
        }

        public List<EnmityHudEntry> GetEnmityHudEntries()
        {
            var entries = new List<EnmityHudEntry>(); ;

            if (!GetDynamicPointerAddress())
            {
                return entries;
            }

            // Get EnmityHud Count, Empty(Min) = 0, Max = 8
            var count = memory.GetInt32(enmityHudDynamicAddress, enmityHudCountOffset);
            if (count < 0) count = 0;
            if (count > 8) count = 8;

            // Get data from memory (all 8 entries)
            byte[] buffer = memory.GetByteArray(enmityHudDynamicAddress + enmityHudEntryOffset, 8 * enmityHudEntrySize);

            // Parse data
            for (int i = 0; i < count; i++)
            {
                entries.Add(GetEnmityHudEntryFromBytes(buffer, i));
            }

            return entries;
        }

        protected abstract unsafe EnmityHudEntry GetEnmityHudEntryFromBytes(byte[] source, int num);

    }
}
