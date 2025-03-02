using System;
using System.Collections.Generic;

using BestHTTP.PlatformSupport.Memory;

namespace BestMQTT.Packets
{
    public readonly struct AuthenticationMessage
    {
        public readonly AuthReasonCodes ReasonCode;
        public readonly string Reason;

        public readonly string Method;
        public readonly BufferSegment Data;

        public readonly List<KeyValuePair<string, string>> UserProperties;

        internal AuthenticationMessage(Packet packet)
        {
            this.ReasonCode = (AuthReasonCodes)packet.VariableHeaderFields[0].Integer;
            this.Reason = packet.VariableHeaderFields.Properties.TryFindData(PacketProperties.ReasonString, Utils.DataTypes.UTF8String, out var data) ? data.UTF8String.Key : null;

            this.Method = packet.VariableHeaderFields.Properties.TryFindData(PacketProperties.AuthenticationMethod, Utils.DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;
            this.Data = packet.VariableHeaderFields.Properties.TryFindData(PacketProperties.AuthenticationData, Utils.DataTypes.Binary, out data) ? data.Binary : BufferSegment.Empty;

            this.UserProperties = packet.VariableHeaderFields.Properties.ConvertAll<KeyValuePair<string, string>>(PacketProperties.UserProperty, kvp_data => kvp_data.UTF8String);
        }
    }
}
