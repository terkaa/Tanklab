using System;
using System.Collections.Generic;

using BestHTTP.PlatformSupport.Memory;

using BestMQTT.Packets;
using BestMQTT.Packets.Utils;

namespace BestMQTT
{
    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901106
    /// </summary>
    public readonly struct ApplicationMessage
    {
        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901108
        /// </summary>
        internal readonly UInt16 PacketId;

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901117
        /// </summary>
        internal readonly UInt32 SubscriptionId;

        /// <summary>
        /// Set to true if it's not the first ocassion the broker sent this application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901102"/>
        public readonly bool IsDuplicate;

        /// <summary>
        /// QoS this application message sent with.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901103"/>
        public readonly QoSLevels QoS;

        /// <summary>
        /// Set to true if this is a retained application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901104"/>
        public readonly bool Retain;

        /// <summary>
        /// The topic's name this application message is publish to.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901107"/>
        public readonly string Topic;

        /// <summary>
        /// Payload type (binary or text).
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901111"/>
        public readonly PayloadTypes PayloadFormat;

        /// <summary>
        /// Expiry interval of the application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901112"/>
        public readonly TimeSpan ExpiryInterval;

        /// <summary>
        /// Topic alias index the broker used.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901113"/>
        internal readonly UInt16 TopicAlias;

        /// <summary>
        /// Topic name where the publisher waiting for a response to this application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901114"/>
        public readonly string ResponseTopic;

        /// <summary>
        /// Arbitrary data sent with the application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901115"/>
        public readonly BufferSegment CorrelationData;

        /// <summary>
        /// Key-value pairs sent with the application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901116"/>
        public readonly List<KeyValuePair<string, string>> UserProperties;

        /// <summary>
        /// Arbitrary value set by the publisher.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901118"/>
        public readonly string ContentType;

        /// <summary>
        /// Payload of the application message.
        /// </summary>
        /// <see href="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901119"/>
        public readonly BufferSegment Payload;

        internal ApplicationMessage(UInt32 sId, Packet packet)
        {
            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901117
            // Multiple Subscription Identifiers will be included if the publication is the result of a match to more than one subscription, in this case their order is not significant.
            this.SubscriptionId = sId;

            this.IsDuplicate = packet.Flags[3];
            this.QoS = (QoSLevels)packet.Flags.Range(2, 1);
            this.Retain = packet.Flags[0];

            this.Topic = packet.VariableHeaderFields[0].UTF8String.Key;

            // The Packet Identifier field is only present in PUBLISH packets where the QoS level is 1 or 2.
            this.PacketId = this.QoS >= QoSLevels.AtLeastOnceDelivery ? (UInt16)packet.VariableHeaderFields[1].Integer : (UInt16)0;

            Properties properties = packet.VariableHeaderFields.Properties;

            this.PayloadFormat = properties.TryFindData(PacketProperties.PayloadFormatIndicator, DataTypes.Byte, out var data) ? (PayloadTypes)data.Integer : PayloadTypes.Bytes;
            this.ExpiryInterval = properties.TryFindData(PacketProperties.MessageExpiryInterval, DataTypes.FourByteInteger, out data) ? TimeSpan.FromSeconds(data.Integer) : TimeSpan.MaxValue;
            this.TopicAlias = properties.TryFindData(PacketProperties.TopicAlias, DataTypes.TwoByteInteger, out data) ? (UInt16)data.Integer : (UInt16)0;
            this.ResponseTopic = properties.TryFindData(PacketProperties.ResponseTopic, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;
            this.CorrelationData = properties.TryFindData(PacketProperties.CorrelationData, DataTypes.Binary, out data) ? data.Binary : BufferSegment.Empty;
            this.UserProperties = properties.ConvertAll<KeyValuePair<string, string>>(PacketProperties.UserProperty, kvp_data => kvp_data.UTF8String);
            this.ContentType = properties.TryFindData(PacketProperties.ContentType, DataTypes.UTF8String, out data) ? data.UTF8String.Key : null;

            this.Payload = packet.Payload.Count > 0 ? packet.Payload[0].Binary : BufferSegment.Empty;
        }
    }
}
