using System;

namespace BestMQTT.Packets.Utils
{
    public static class PacketIdentifier
    {
        private static int nextPID = 1;

        public static UInt16 Acquire()
        {
            System.Threading.Interlocked.CompareExchange(ref nextPID, 1, UInt16.MaxValue);
            return (UInt16)System.Threading.Interlocked.Increment(ref nextPID);
        }

        public static void Release(UInt16 pid)
        {

        }
    }
}
