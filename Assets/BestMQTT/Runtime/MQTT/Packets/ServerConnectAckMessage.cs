using System;
using System.Collections.Generic;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets
{
    public readonly struct ServerConnectAckMessage
    {
        /// <summary>
        /// True if the server could resume to a previous session.
        /// </summary>
        public readonly bool SessionPresent;
        public readonly ConnectAckReasonCodes ReasonCode;
        public readonly UInt32? SessionExpiryInterval;

        /// <summary>
        /// The Server uses this value to limit the number of QoS 1 and QoS 2 publications that it is willing to process concurrently for the Client.
        /// It does not provide a mechanism to limit the QoS 0 publications that the Client might try to send.
        /// If the Receive Maximum value is absent, then its value defaults to 65,535.
        /// </summary>
        public readonly UInt16 ReceiveMaximum;
        public readonly QoSLevels MaximumQoS;
        public readonly bool RetainAvailable;

        /// <summary>
        /// Maximum Packet Size the Server is willing to accept
        /// </summary>
        public readonly UInt32 MaximumPacketSize;
        public readonly string AssignedClientIdentifier;
        public readonly UInt16 TopicAliasMaximum;
        public readonly string ReasonString;
        public readonly List<KeyValuePair<string, string>> UserProperties;
        public readonly bool WildcardSubscriptionAvailable;
        public readonly bool SubscriptionIdentifiersAvailable;
        public readonly bool SharedSubscriptionAvailable;
        public readonly UInt16? ServerKeepAlive;
        public readonly string ResponseInformation;
        public readonly string ServerReference;
        public readonly string AuthenticationMethod;
        public readonly BestHTTP.PlatformSupport.Memory.BufferSegment AuthenticationData;

        internal ServerConnectAckMessage(Packet packet)
        {
            // Session Present (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901078)
            var flags = packet.VariableHeaderFields[0].Bits;
            this.SessionPresent = flags[0];

            // Connect Reason Code
            this.ReasonCode = (ConnectAckReasonCodes)packet.VariableHeaderFields[1].Integer;

            // CONNACK Properties (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901080)
            Properties properties = packet.VariableHeaderFields.Properties;

            // If the Session Expiry Interval is absent the value in the CONNECT Packet used.
            this.SessionExpiryInterval = properties.TryFindData(PacketProperties.SessionExpiryInterval, DataTypes.FourByteInteger, out var data) ? data.Integer : default(UInt32?);

            // If the Receive Maximum value is absent, then its value defaults to 65,535.
            this.ReceiveMaximum = properties.TryFindData(PacketProperties.ReceiveMaximum, DataTypes.TwoByteInteger, out data) ? (UInt16)data.Integer : UInt16.MaxValue;

            // If the Maximum QoS is absent, the Client uses a Maximum QoS of 2. (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901084)
            this.MaximumQoS = properties.TryFindData(PacketProperties.MaximumQoS, DataTypes.Byte, out data) ? (QoSLevels)data.Integer : QoSLevels.ExactlyOnceDelivery;

            // A value of 0 means that retained messages are not supported. A value of 1 means retained messages are supported. If not present, then retained messages are supported.
            this.RetainAvailable = properties.TryFindData(PacketProperties.RetainAvailable, DataTypes.Byte, out data) ? data.Integer > 0 : true;

            // If the Maximum Packet Size is not present, there is no limit on the packet size imposed beyond the limitations in the protocol as a result of the remaining length encoding and the protocol header sizes.
            this.MaximumPacketSize = properties.TryFindData(PacketProperties.MaximumPacketSize, DataTypes.FourByteInteger, out data) ? data.Integer : UInt32.MaxValue;

            this.AssignedClientIdentifier = properties.TryFindData(PacketProperties.AssignedClientIdentifier, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;

            // If the Topic Alias Maximum property is absent, the default value is 0.
            this.TopicAliasMaximum = properties.TryFindData(PacketProperties.TopicAliasMaximum, DataTypes.TwoByteInteger, out data) ? (UInt16)data.Integer : (UInt16)0;

            this.ReasonString = properties.TryFindData(PacketProperties.ReasonString, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;

            // The User Property is allowed to appear multiple times to represent multiple name, value pairs.
            List<KeyValuePair<string, string>> tmpList = null;

            properties.ForEach(PacketProperties.UserProperty, d =>
            {
                if (tmpList == null)
                    tmpList = new List<KeyValuePair<string, string>>();
                tmpList.Add(d.UTF8String);
            });
            this.UserProperties = tmpList;

            // If not present, then Wildcard Subscriptions are supported.
            this.WildcardSubscriptionAvailable = properties.TryFindData(PacketProperties.WildcardSubscriptionAvailable, DataTypes.Byte, out data) ? data.Integer > 0 : true;

            // If not present, then Subscription Identifiers are supported.
            this.SubscriptionIdentifiersAvailable = properties.TryFindData(PacketProperties.SubscriptionIdentifierAvailable, DataTypes.Byte, out data) ? data.Integer > 0 : true;

            // If not present, then Shared Subscriptions are supported.
            this.SharedSubscriptionAvailable = properties.TryFindData(PacketProperties.SharedSubscriptionAvailable, DataTypes.Byte, out data) ? data.Integer > 0 : true;

            // If the Server does not send the Server Keep Alive, the Server MUST use the Keep Alive value set by the Client on CONNECT [MQTT-3.2.2-22].
            this.ServerKeepAlive = properties.TryFindData(PacketProperties.ServerKeepAlive, DataTypes.TwoByteInteger, out data) ? (UInt16)data.Integer : default(UInt16?);

            this.ResponseInformation = properties.TryFindData(PacketProperties.ResponseInformation, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;

            this.ServerReference = properties.TryFindData(PacketProperties.ServerReference, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;

            this.AuthenticationMethod = properties.TryFindData(PacketProperties.AuthenticationMethod, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;

            this.AuthenticationData = properties.TryFindData(PacketProperties.AuthenticationData, DataTypes.Binary, out data) ? data.Binary : BestHTTP.PlatformSupport.Memory.BufferSegment.Empty;
        }
    }
}
