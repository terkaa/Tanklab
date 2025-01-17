using System;
using System.Collections.Generic;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    public delegate void UnSubscribeAcknowledgementDelegate(MQTTClient client, string topicName, UnsubscribeAckReasonCodes reasonCode);

    internal readonly struct UnsubscribeTopicFilter
    {
        public readonly string Filter;
        public readonly UnSubscribeAcknowledgementDelegate AcknowledgementCallback;

        public UnsubscribeTopicFilter(string filter, UnSubscribeAcknowledgementDelegate acknowledgementCallback)
        {
            this.Filter = filter;
            this.AcknowledgementCallback = acknowledgementCallback;
        }
    }

    public struct UnsubscribeTopicFilterBuilder
    {
        private string _filter;
        private UnSubscribeAcknowledgementDelegate _acknowledgementCallback;

        public UnsubscribeTopicFilterBuilder(string filter)
        {
            this._filter = filter;
            this._acknowledgementCallback = null;
        }

        public UnsubscribeTopicFilterBuilder WithAcknowledgementCallback(UnSubscribeAcknowledgementDelegate acknowledgementCallback)
        {
            this._acknowledgementCallback = acknowledgementCallback;
            return this;
        }

        internal UnsubscribeTopicFilter Build(BulkUnsubscribePacketBuilder parentBuilder, ref Packet packet)
        {
            if (string.IsNullOrEmpty(this._filter))
                throw new ArgumentException("No Topic Filter is set!");

            packet.AddPayload(Data.FromString(this._filter));

            return new UnsubscribeTopicFilter(this._filter, this._acknowledgementCallback);
        }
    }

    public struct UnsubscribePacketBuilder
    {
        private MQTTClient _client;
        private UnsubscribeTopicFilterBuilder _topic;

        internal UnsubscribePacketBuilder(MQTTClient client, string topicFilter)
        {
            this._client = client;
            this._topic = new UnsubscribeTopicFilterBuilder(topicFilter);
        }

        public UnsubscribePacketBuilder WithAcknowledgementCallback(UnSubscribeAcknowledgementDelegate acknowledgementCallback)
        {
            this._topic.WithAcknowledgementCallback(acknowledgementCallback);
            return this;
        }

        public void BeginUnsubscribe()
        {
            new BulkUnsubscribePacketBuilder(this._client)
                .WithTopicFilter(this._topic)
                .BeginUnsubscribe();
        }
    }

    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901179
    /// </summary>
    public struct BulkUnsubscribePacketBuilder
    {
        private MQTTClient _client;
        private Properties _properties;
        private List<UnsubscribeTopicFilterBuilder> _topics;

        internal BulkUnsubscribePacketBuilder(MQTTClient client)
        {
            this._client = client;

            this._properties = default(Properties);
            this._topics = null;
        }

        public BulkUnsubscribePacketBuilder WithUserProperty(string key, string value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithUserProperty)} is available with MQTT v5.0 or newer.");

            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });
            return this;
        }

        public BulkUnsubscribePacketBuilder WithTopicFilter(UnsubscribeTopicFilterBuilder topic)
        {
            if (this._topics == null)
                this._topics = new List<UnsubscribeTopicFilterBuilder>();

            this._topics.Add(topic);

            return this;
        }

        public void BeginUnsubscribe() => this._client.BeginUnsubscribe(this);

        internal Packet Build()
        {
            if (this._topics == null || this._topics.Count == 0)
                throw new ArgumentException("At least ONE Topic Filter must be set!");

            var packet = new Packet();
            packet.Type = PacketTypes.Unsubscribe;
            packet.Flags = new BitField(0b0010);

            var packetID = this._client.GetNextPacketID();
            packet.AddVariableHeader(Data.FromTwoByteInteger(packetID));

            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.FromProperties(this._properties));

            List<UnsubscribeTopicFilter> ackTopics = new List<UnsubscribeTopicFilter>();
            for (int i = 0; i < this._topics.Count; ++i)
                ackTopics.Add(this._topics[i].Build(this, ref packet));

            this._client.AddUnsubscription(packetID, ackTopics);

            return packet;
        }
    }
}
