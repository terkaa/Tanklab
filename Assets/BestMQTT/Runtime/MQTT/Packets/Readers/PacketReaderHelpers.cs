using System;

namespace BestMQTT.Packets.Readers
{
    internal static class PacketReaderHelpers
    {
        public static void Expect(bool expected, Func<string> message)
        {
            if (!expected)
                throw new MalformedPacketException(message());
        }
    }
}
