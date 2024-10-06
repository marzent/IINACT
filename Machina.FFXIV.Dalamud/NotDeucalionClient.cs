using System.Runtime.CompilerServices;

namespace Machina.FFXIV.Dalamud
{
    public class NotDeucalionClient : IDisposable
    {
        public delegate void MessageReceivedHandler(byte[] message);
        public MessageReceivedHandler? MessageReceived;

        public delegate void MessageSentHandler(byte[] message);
        public MessageSentHandler? MessageSent;

        private DalamudClient _dalamudClient;
        private static ConditionalWeakTable<byte[], object /* long */> EpochWeakTable = new ConditionalWeakTable<byte[], object>();

        public NotDeucalionClient()
        {
            _dalamudClient = new DalamudClient();
        }

        public void OnMessageReceived(long epoch, byte[] message)
        {
            EpochWeakTable.Add(message, (object)epoch);
            MessageReceived?.Invoke(message);
        }

        public void Connect(int processId)
        {
            _dalamudClient.MessageReceived += OnMessageReceived;
            _dalamudClient.Connect();
        }

        public void Disconnect()
        {
            _dalamudClient.MessageReceived -= OnMessageReceived;
            _dalamudClient.Disconnect();
        }

        public static (long, byte[]) ConvertDeucalionFormatToPacketFormat(byte[] message)
        {
            if (!EpochWeakTable.TryGetValue(message, out object epoch))
            {
                throw new InvalidOperationException("Message does not have an associated epoch");
            }
            return ((long)epoch, message);
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            Disconnect();
            _dalamudClient.Dispose();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
