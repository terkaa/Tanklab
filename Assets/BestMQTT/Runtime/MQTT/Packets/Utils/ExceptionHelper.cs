using System;

namespace BestMQTT.Packets.Utils
{
    internal static class ExceptionHelper
    {
        public static void ThrowIfV311(SupportedProtocolVersions version, string msg)
        {
            if (version < SupportedProtocolVersions.MQTT_5_0)
                throw new Exception(msg);
        }
    }
}
