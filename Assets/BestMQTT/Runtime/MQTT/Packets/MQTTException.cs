using System;

namespace BestMQTT.Packets
{
    public enum MQTTErrorTypes
    {
        MalformedPacket,
        ProtocolError,
        ReceiveMaximumExceeded,
        PacketTooLarge
    }

    public class MQTTException : Exception
    {
        public MQTTErrorTypes MQTTError { get; private set; }

        public MQTTException(MQTTErrorTypes errorType, string message) : base(message) { this.MQTTError = errorType; }
    }

    public sealed class ProtocolErrorException : MQTTException
    {
        public ProtocolErrorException(string message) : base(MQTTErrorTypes.ProtocolError, message) { }
    }

    public sealed class MalformedPacketException : MQTTException
    {
        public MalformedPacketException(string message) : base(MQTTErrorTypes.MalformedPacket, message) { }
    }
}
