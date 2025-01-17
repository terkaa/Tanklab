using System;

namespace BestMQTT
{
    /// <summary>
    /// Helper class to help safely use MQTTClient's BeginPacketBuffer-EndPacketBuffer pairs in a using.
    /// </summary>
    public struct PacketBufferHelper : IDisposable
    {
        private readonly MQTTClient _client;
        public PacketBufferHelper(MQTTClient client)
        {
            this._client = client;
            this._client?.BeginPacketBuffer();
        }

        public void Dispose() => this._client?.EndPacketBuffer();
    }

    public static class PacketBufferHelperExtensions
    {
        public static PacketBufferHelper CreatePacketBufferHelper(this MQTTClient client) => new PacketBufferHelper(client);
    }
}
