using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party
{
    public interface IPartyMemory : IVersionedMemory
    {
        PartyListsStruct GetPartyLists();
    }

    class PartyMemoryManager : IPartyMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private IPartyMemory memory = null;

        public PartyMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IPartyMemory70, PartyMemory70>();
            repository = container.Resolve<FFXIVRepository>();

            var memory = container.Resolve<FFXIVMemory>();
            memory.RegisterOnProcessChangeHandler(FindMemory);
        }

        private void FindMemory(object sender, Process p)
        {
            memory = null;
            if (p == null)
            {
                return;
            }
            ScanPointers();
        }

        public void ScanPointers()
        {
            List<IPartyMemory> candidates = new List<IPartyMemory>();
            candidates.Add(container.Resolve<IPartyMemory70>());
            memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
        }

        public bool IsValid()
        {
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
        }

        Version IVersionedMemory.GetVersion()
        {
            if (!IsValid())
                return null;
            return memory.GetVersion();
        }

        public PartyListsStruct GetPartyLists()
        {
            if (!IsValid())
            {
                return new PartyListsStruct();
            }
            return memory.GetPartyLists();
        }
    }
}
