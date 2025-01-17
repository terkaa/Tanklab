using System;
using System.Collections.Generic;

using BestMQTT.Packets.Utils;

namespace BestMQTT.Packets.Builders
{
    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901161
    /// </summary>
    public struct SubscribePacketBuilder
    {
        private MQTTClient _client;

        private Properties _properties;
        private SubscribeTopicBuilder _topicBuilder;

        internal SubscribePacketBuilder(MQTTClient client, string topicFilter)
        {
            this._client = client;

            this._properties = default(Properties);
            this._topicBuilder = new SubscribeTopicBuilder(topicFilter);
            this._topicBuilder.WithMaximumQoS(client.NegotiatedOptions.ServerOptions.MaximumQoS);
        }

        public SubscribePacketBuilder WithUserProperty(string key, string value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithUserProperty)} is available with MQTT v5.0 or newer.");

            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });
            return this;
        }

        public SubscribePacketBuilder WithMaximumQoS(QoSLevels maxQoS)
        {
            this._topicBuilder.WithMaximumQoS(maxQoS);
            return this;
        }

        public SubscribePacketBuilder WithNoLocal()
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithNoLocal)} is available with MQTT v5.0 or newer.");

            this._topicBuilder.WithNoLocal();
            return this;
        }

        public SubscribePacketBuilder WithRetainAsPublished()
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithRetainAsPublished)} is available with MQTT v5.0 or newer.");

            this._topicBuilder.WithRetainAsPublished();
            return this;
        }

        public SubscribePacketBuilder WithRetainHandlingOptions(RetaionHandlingOptions options)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithRetainHandlingOptions)} is available with MQTT v5.0 or newer.");

            this._topicBuilder.WithRetainHandlingOptions(options);
            return this;
        }

        
        public SubscribePacketBuilder WithAcknowledgementCallback(SubscriptionAcknowledgementDelegate callback)
        {
            this._topicBuilder.WithAcknowledgementCallback(callback);
            return this;
        }

        public SubscribePacketBuilder WithMessageCallback(SubscriptionMessageDelegate callback)
        {
            this._topicBuilder.WithMessageCallback(callback);
            return this;
        }

        public void BeginSubscribe()
        {
            var builder = this._client.CreateBulkSubscriptionBuilder()
                .WithTopic(this._topicBuilder);

            if (this._client.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                builder.WithProperties(this._properties);

            builder.BeginSubscribe();
        }
    }

    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901161
    /// </summary>
    public struct BulkSubscribePacketBuilder
    {
        private MQTTClient _client;

        private Properties _properties;
        private SubscribeTopicBuilder _topicBuilder;
        private List<SubscribeTopicBuilder> _topicBuilders;
        
        internal BulkSubscribePacketBuilder(MQTTClient client)
        {
            this._client = client;

            this._properties = default(Properties);
            this._topicBuilder = default(SubscribeTopicBuilder);
            this._topicBuilders = null;
        }

        internal BulkSubscribePacketBuilder WithProperties(Properties properties)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithProperties)} is available with MQTT v5.0 or newer.");

            this._properties = properties;
            return this;
        }

        public BulkSubscribePacketBuilder WithUserProperty(string key, string value)
        {
            ExceptionHelper.ThrowIfV311(this._client.Options.ProtocolVersion, $"{nameof(WithUserProperty)} is available with MQTT v5.0 or newer.");

            this._properties.AddProperty(new Property { Type = PacketProperties.UserProperty, Data = Data.FromStringPair(key, value) });
            return this;
        }

        public BulkSubscribePacketBuilder WithTopic(SubscribeTopicBuilder topicBuilder)
        {
            if (this._topicBuilders != null)
                this._topicBuilders.Add(topicBuilder);
            else if (this._topicBuilder.IsSet)
            {
                this._topicBuilders = new List<SubscribeTopicBuilder>();
                this._topicBuilders.Add(this._topicBuilder);
                this._topicBuilders.Add(topicBuilder);
                this._topicBuilder = new SubscribeTopicBuilder();
            }
            else this._topicBuilder = topicBuilder;

            return this;
        }

        public void BeginSubscribe() => this._client.BeginSubscribe(this);

        internal (Packet, Subscription) Build(MQTTClient parentClient)
        {
            if ((this._topicBuilders == null || this._topicBuilders.Count == 0) && !this._topicBuilder.IsSet)
                throw new ArgumentException("At least one topic must be added!");

            var packet = new Packet();

            packet.Type = PacketTypes.Subscribe;
            packet.Flags = new BitField(0b_0000_0010);

            UInt16 pid = PacketIdentifier.Acquire();
            packet.AddVariableHeader(Data.FromTwoByteInteger(pid));

            var sID = parentClient.GetNextSubscriptionID();

            var subscription = new Subscription(parentClient, sID);

            this._properties.AddProperty(new Property { Type = PacketProperties.SubscriptionIdentifier, Data = Data.FromVariableByteInteger(sID) });

            if (parentClient.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                packet.AddVariableHeader(Data.FromProperties(this._properties));

            if (this._topicBuilders != null)
            {
                for (int i = 0; i < this._topicBuilders.Count; ++i)
                    this._topicBuilders[i].Build(parentClient, subscription, ref packet);
            }
            else if (this._topicBuilder.IsSet)
                this._topicBuilder.Build(parentClient, subscription, ref packet);

            parentClient.AddSubscription(pid, subscription);

            return (packet, subscription);
        }
    }

    public struct SubscribeTopicBuilder
    {
        internal bool IsSet { get; private set; }

        internal SubscriptionTopic _topic;
        private BitField _flags;

        public SubscribeTopicBuilder(string topicFilter)
        {
            this.IsSet = true;
            this._topic = new SubscriptionTopic(topicFilter);
            this._flags = new BitField();
        }

        public SubscribeTopicBuilder WithMaximumQoS(QoSLevels maxQoS)
        {
            this._flags.ClearRange(0, 1);
            switch (maxQoS)
            {
                case QoSLevels.AtLeastOnceDelivery: this._flags[0] = true; break;
                case QoSLevels.ExactlyOnceDelivery: this._flags[1] = true; break;
            }

            return this;
        }

        public SubscribeTopicBuilder WithNoLocal()
        {
            this._flags.Set(2, true);

            return this;
        }

        public SubscribeTopicBuilder WithRetainAsPublished()
        {
            this._flags.Set(3, true);

            return this;
        }

        public SubscribeTopicBuilder WithRetainHandlingOptions(RetaionHandlingOptions options)
        {
            switch (options)
            {
                case RetaionHandlingOptions.SendWhenSubscribeIfSubscriptionDoesntExist: this._flags[4] = true; break;
                case RetaionHandlingOptions.DoNotSendRetainedMessages: this._flags[5] = true; break;
            }

            return this;
        }

        public SubscribeTopicBuilder WithAcknowledgementCallback(SubscriptionAcknowledgementDelegate callback)
        {
            this._topic.AcknowledgementCallback += callback;
            return this;
        }

        public SubscribeTopicBuilder WithMessageCallback(SubscriptionMessageDelegate callback)
        {
            this._topic.MessageCallback += callback;
            return this;
        }

        internal void Build(MQTTClient client, Subscription subscription, ref Packet packet)
        {
            if (!this.IsSet)
                throw new ArgumentException("No Topic Filter is set!");

            if (client.Options.ProtocolVersion < SupportedProtocolVersions.MQTT_5_0)
            {
                if (this._flags.IsSet(2))
                    throw new Exception($"{nameof(WithNoLocal)} is available with MQTT v5.0 or newer.");
                if (this._flags.IsSet(3))
                    throw new Exception($"{nameof(WithRetainAsPublished)} is available with MQTT v5.0 or newer.");
                if (this._flags.IsSet(4) || this._flags.IsSet(5))
                    throw new Exception($"{nameof(WithRetainHandlingOptions)} is available with MQTT v5.0 or newer.");
            }

            packet.AddPayload(Data.FromString(this._topic.Filter.OriginalFilter));
            packet.AddPayload(Data.FromBitField(this._flags));

            subscription.AddTopic(this._topic);
        }
    }
}
