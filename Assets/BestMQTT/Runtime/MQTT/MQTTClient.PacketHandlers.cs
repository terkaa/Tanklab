using System;

using BestHTTP.PlatformSupport.Memory;

using static BestHTTP.HTTPManager;

using BestMQTT.Packets.Builders;
using BestMQTT.Packets.Readers;
using BestMQTT.Packets.Utils;
using BestMQTT.Packets;

namespace BestMQTT
{
    public sealed partial class MQTTClient
    {
        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901251
        /// </summary>
        private void IncrementAndReplenishQueue()
        {
            this._sendQuota = Math.Min(this._maxQuota, (UInt16)(this._sendQuota + 1));

            Logger.Verbose(nameof(MQTTClient), $"{nameof(IncrementAndReplenishQueue)} SendQuota: ({this._sendQuota})", this.Context);

            while (this._sendQuota > 0)
            {
                var (found, packetId, packet) = this.Session.QueuedPackets.TryDequeue();

                if (!found)
                    return;

                if (packetId == 0)
                {
                    Logger.Error(nameof(MQTTClient), $"{nameof(IncrementAndReplenishQueue)} found a packet with packetId set to zero!", this.Context);
                    continue;
                }

                SendPublishPacket(packetId, in packet);
            }
        }

        private void HandleAuthPacket(Packet packet)
        {
            var authMessage = new AuthenticationMessage(packet);
            try
            {
                this.OnAuthenticationMessage?.Invoke(this, authMessage);
            }
            catch (MQTTException ex)
            {
                Logger.Exception(nameof(MQTTClient), nameof(HandleAuthPacket), ex, this.Context);

                this.MQTTError(nameof(HandleAuthPacket), ex);
            }
            catch (Exception ex)
            {
                Logger.Exception(nameof(MQTTClient), nameof(HandleAuthPacket), ex, this.Context);
            }
        }

        private void HandlePublishAckPacket(Packet packet)
        {
            // The send quota is incremented by 1: Each time a PUBACK or PUBCOMP packet is received, regardless of whether the PUBACK or PUBCOMP carried an error code.
            IncrementAndReplenishQueue();

            UInt16 packetId = (UInt16)packet.VariableHeaderFields[0].Integer;

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties.
            PublishAckAndReceivedReasonCodes code = PublishAckAndReceivedReasonCodes.Success;
            if (packet.VariableHeaderFields.Count > 1 && packet.VariableHeaderFields[1].Type == DataTypes.Byte)
                code = (PublishAckAndReceivedReasonCodes)packet.VariableHeaderFields[1].Integer;

            Logger.Information(nameof(MQTTClient), $"{nameof(HandlePublishAckPacket)}({packetId}, {code})", this.Context);

            bool foundPacket = this.Session.UnacknowledgedPackets.TryRemoveByPacketId(packetId);
            if (!foundPacket)
                Logger.Warning(nameof(MQTTClient), $"{nameof(HandlePublishAckPacket)}: Couldn't found packet for packetId {packetId}!", this.Context);
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901151
        /// </summary>
        private void HandlePublishCompletePacket(Packet packet)
        {
            // The send quota is incremented by 1: Each time a PUBACK or PUBCOMP packet is received, regardless of whether the PUBACK or PUBCOMP carried an error code.
            IncrementAndReplenishQueue();

            UInt16 packetId = (UInt16)packet.VariableHeaderFields[0].Integer;

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties.
            PublishReleaseAndCompleteReasonCodes code = PublishReleaseAndCompleteReasonCodes.Success;
            if (packet.VariableHeaderFields.Count > 1 && packet.VariableHeaderFields[1].Type == DataTypes.Byte)
                code = (PublishReleaseAndCompleteReasonCodes)packet.VariableHeaderFields[1].Integer;

            Logger.Information(nameof(MQTTClient), $"{nameof(HandlePublishCompletePacket)}({packetId}, {code})", this.Context);

            // Discard packetId
            this.Session.PublishReleasedPacketIDs.Remove(packetId);
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901131
        /// </summary>
        private void HandlePublishReceivedPacket(Packet packet)
        {
            UInt16 packetId = (UInt16)packet.VariableHeaderFields[0].Integer;

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties.
            PublishAckAndReceivedReasonCodes code = PublishAckAndReceivedReasonCodes.Success;
            if (packet.VariableHeaderFields.Count > 1 && packet.VariableHeaderFields[1].Type == DataTypes.Byte)
                code = (PublishAckAndReceivedReasonCodes)packet.VariableHeaderFields[1].Integer;

            Logger.Information(nameof(MQTTClient), $"{nameof(HandlePublishReceivedPacket)}({packetId}, {code})", this.Context);

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901237
            // MUST treat the PUBLISH packet as “unacknowledged” until it has received the corresponding PUBREC packet from the receiver [MQTT-4.3.3-3].
            bool foundPacket = this.Session.UnacknowledgedPackets.TryRemoveByPacketId(packetId);

            // If it has sent a PUBREC with a Reason Code of 0x80 or greater, the receiver MUST treat any subsequent PUBLISH packet that contains that Packet Identifier as being a new Application Message [MQTT-4.3.3-9].
            if (code >= PublishAckAndReceivedReasonCodes.UnspecifiedError)
            {
                // The send quota is incremented by 1: Each time a PUBREC packet is received with a Return Code of 0x80 or greater.                
                IncrementAndReplenishQueue();
            }
            else
            {
                // MUST send a PUBREL packet when it receives a PUBREC packet from the receiver with a Reason Code value less than 0x80.
                // This PUBREL packet MUST contain the same Packet Identifier as the original PUBLISH packet [MQTT-4.3.3-4].
                if (foundPacket)
                    this.Session.PublishReleasedPacketIDs.Add(packetId);

                var outPacket = new PublishReleasePacketBuilder(this)
                                     .WithPacketID(packetId)
                                     .WithReasonCode(foundPacket ? PublishReleaseAndCompleteReasonCodes.Success : PublishReleaseAndCompleteReasonCodes.PacketIdentifierNotFound)
                                     .Build();
                this.Send(in outPacket);
            }
        }

        private void HandleUnsubscribeAckPacket(Packet packet)
        {
            UInt16 packetId = (UInt16)packet.VariableHeaderFields[0].Integer;

            if (this.pendingUnsubscriptions.TryRemove(packetId, out var topicUnsubscribeRequests))
            {
                if (topicUnsubscribeRequests.Count != packet.Payload.Count && this.Options.ProtocolVersion >= SupportedProtocolVersions.MQTT_5_0)
                    Logger.Warning(nameof(MQTTClient), $"Un-subscribe ACK received with different payload count({packet.Payload.Count}) than the original request ({topicUnsubscribeRequests.Count})", this.Context);

                for (int i = 0; i < topicUnsubscribeRequests.Count; i++)
                {
                    var topic = topicUnsubscribeRequests[i];

                    try
                    {
                        topic.AcknowledgementCallback?.Invoke(this, topic.Filter, (UnsubscribeAckReasonCodes)packet.Payload[i].Integer);
                    }
                    catch (MQTTException ex)
                    {
                        Logger.Exception(nameof(MQTTClient), $"{nameof(topic.AcknowledgementCallback)}(\"{topic.Filter}\", {(SubscribeAckReasonCodes)packet.Payload[i].Integer})", ex, this.Context);
                        this.MQTTError(nameof(HandleUnsubscribeAckPacket), ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(nameof(MQTTClient), $"{nameof(topic.AcknowledgementCallback)}(\"{topic.Filter}\", {(SubscribeAckReasonCodes)packet.Payload[i].Integer})", ex, this.Context);
                    }

                    // Remove subscription

                    var keys = this.subscriptions.Keys;

                    foreach (var key in keys)
                        if (this.subscriptions.TryGetValue(key, out var subscription))
                        {
                            var (topicFound, removeSubscription) = subscription.TryRemoveTopic(topic.Filter);

                            if (topicFound && removeSubscription)
                                this.subscriptions.TryRemove(key, out _);
                        }
                }
            }
            else
                Logger.Warning(nameof(MQTTClient), $"No un-subscribe request could be found with packet ID {packetId}", this.Context);
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html
        /// </summary>
        private void HandlePublishReleasePacket(Packet packet)
        {
            UInt16 packetId = (UInt16)packet.VariableHeaderFields[0].Integer;

            // The Reason Code and Property Length can be omitted if the Reason Code is 0x00 (Success) and there are no Properties.
            PublishReleaseAndCompleteReasonCodes code = PublishReleaseAndCompleteReasonCodes.Success;
            if (packet.VariableHeaderFields.Count > 1)
                code = (PublishReleaseAndCompleteReasonCodes)packet.VariableHeaderFields[1].Integer;

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901238
            // If PUBACK or PUBREC is received containing a Reason Code of 0x80 or greater the corresponding PUBLISH packet is treated as acknowledged,
            // and MUST NOT be retransmitted [MQTT-4.4.0-2].
            if (code != PublishReleaseAndCompleteReasonCodes.Success)
            {
                // ?
            }

            // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901237
            // MUST respond to a PUBREL packet by sending a PUBCOMP packet containing the same Packet Identifier as the PUBREL [MQTT-4.3.3-11].
            var outPacket = new PublishCompletePacketBuilder(this)
                .WithPacketID(packetId)
                .WithReasonCode(PublishReleaseAndCompleteReasonCodes.Success)
                .Build();
            this.Send(in outPacket);

            this.Session.PublishReceivedPacketIDs.Remove(packetId);
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901100
        /// </summary>
        private void HandlePublishPacket(Packet packet)
        {
            // One application message can have multiple subscriber ids
            System.Collections.Generic.List<ApplicationMessage> applicationMessages = null;
            switch (this.Options.ProtocolVersion)
            {
                case SupportedProtocolVersions.MQTT_3_1_1: applicationMessages = PacketReaderImplementations.CreateApplicationMessagesV311(packet, this.subscriptions); break;
                case SupportedProtocolVersions.MQTT_5_0: applicationMessages = PacketReaderImplementations.CreateApplicationMessages(packet); break;
                default: throw new NotImplementedException($"{this.Options.ProtocolVersion}");
            }

            for (int i = 0; i < applicationMessages.Count; ++i)
            {
                var applicationMessage = applicationMessages[i];

                if (applicationMessage.QoS == QoSLevels.ExactlyOnceDelivery)
                {
                    // Until it has received the corresponding PUBREL packet, the receiver MUST acknowledge any subsequent PUBLISH packet with the same Packet Identifier by sending a PUBREC.
                    // It MUST NOT cause duplicate messages to be delivered to any onward recipients in this case [MQTT-4.3.3-10]
                    if (this.Session.PublishReceivedPacketIDs.Contains(applicationMessage.PacketId))
                        break;
                }

                string topicName = applicationMessage.Topic;

                // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901113
                // The sender decides whether to use a Topic Alias and chooses the value.
                // It sets a Topic Alias mapping by including a non-zero length Topic Name and a Topic Alias in the PUBLISH packet.
                // The receiver processes the PUBLISH as normal but also sets the specified Topic Alias mapping to this Topic Name.
                if (applicationMessage.TopicAlias > 0)
                {
                    if (this._serverTopicAliasMapping == null)
                        this._serverTopicAliasMapping = new System.Collections.Generic.Dictionary<ushort, string>();

                    if (!string.IsNullOrEmpty(topicName))
                        this._serverTopicAliasMapping[applicationMessage.TopicAlias] = topicName;
                    else
                        this._serverTopicAliasMapping.TryGetValue(applicationMessage.TopicAlias, out topicName);
                }

                if (this.subscriptions.TryGetValue(applicationMessage.SubscriptionId, out var subscription))
                {
                    switch (subscription.Topics.Count)
                    {
                        // error?
                        case 0: break;

                        case 1:
                            {
                                var topic = subscription.Topics[0];
                                try
                                {
                                    topic.MessageCallback?.Invoke(this, topic, topicName, applicationMessage);
                                }
                                catch (MQTTException ex)
                                {
                                    Logger.Exception(nameof(MQTTClient), $"{nameof(topic.MessageCallback)}(\"{topicName}\")", ex, this.Context);
                                    this.MQTTError(nameof(HandlePublishPacket), ex);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Exception(nameof(MQTTClient), $"{nameof(topic.MessageCallback)}(\"{topicName}\")", ex, this.Context);
                                }
                                break;
                            }

                        default:
                            bool hadAnyMatch = false;
                            for (int topicIdx = 0; topicIdx < subscription.Topics.Count; ++topicIdx)
                            {
                                var topic = subscription.Topics[topicIdx];
                                if (topic.Filter.IsMatching(topicName))
                                {
                                    Logger.Information(nameof(MQTTClient), $"'{topicName}' matched with filter '{topic.Filter.OriginalFilter}'", this.Context);
                                    try
                                    {
                                        topic.MessageCallback?.Invoke(this, topic, topicName, applicationMessage);
                                    }
                                    catch (MQTTException ex)
                                    {
                                        Logger.Exception(nameof(MQTTClient), $"{nameof(topic.MessageCallback)}(\"{topicName}\")", ex, this.Context);
                                        this.MQTTError(nameof(HandlePublishPacket), ex);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Exception(nameof(MQTTClient), $"{nameof(topic.MessageCallback)}(\"{topicName}\")", ex, this.Context);
                                    }
                                    hadAnyMatch = true;
                                }
                            }

                            if (!hadAnyMatch)
                            {
                                // Log a warning, however also: "A Client could also receive messages that do not match any of its explicit Subscriptions.
                                // This can happen if the Server automatically assigned a subscription to the Client." https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901239

                                Logger.Warning(nameof(MQTTClient), $"{topicName} didn't match any of the subscription topics!", this.Context);
                            }
                            break;
                    }
                }

                try
                {
                    this.OnApplicationMessage?.Invoke(this, applicationMessage);
                }
                catch (MQTTException ex)
                {
                    Logger.Exception(nameof(MQTTClient), nameof(this.OnApplicationMessage), ex, this.Context);
                }
            }

            // All of the generated application messages are the very same except the subscription ID,
            //  so we are safe here only accessing and operating only on the first one until it doesn't
            //  depend on the subscription id!
            if (applicationMessages.Count > 0)
            {
                var msg = applicationMessages[0];
                BufferPool.Release(msg.Payload);

                switch (msg.QoS)
                {
                    case QoSLevels.AtMostOnceDelivery: break;

                    // Take ownership of the Application Message by confirming its delivery.
                    case QoSLevels.AtLeastOnceDelivery:
                        var ackPacket = new PublishAckBuilder(this)
                            .WithPacketID(msg.PacketId)
                            .WithReasonCode(PublishAckAndReceivedReasonCodes.Success)
                            .Build();
                        this.Send(in ackPacket);
                        break;

                    case QoSLevels.ExactlyOnceDelivery:
                        // Until it has received the corresponding PUBREL packet, the receiver MUST acknowledge any subsequent PUBLISH packet with the same Packet Identifier by sending a PUBREC.
                        // It MUST NOT cause duplicate messages to be delivered to any onward recipients in this case [MQTT-4.3.3-10].
                        if (!this.Session.PublishReceivedPacketIDs.Contains(msg.PacketId))
                            this.Session.PublishReceivedPacketIDs.Add(msg.PacketId);

                        // The Client uses this value to limit the number of QoS 1 and QoS 2 publications that it is willing to process concurrently. https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901049
                        if (this.Session.PublishReceivedPacketIDs.Count() >= this.NegotiatedOptions.ClientReceiveMaximum)
                            this.MQTTError(nameof(HandlePublishPacket),
                                           MQTTErrorTypes.ReceiveMaximumExceeded,
                                           $"this.publishReceivedPacketIDs.Count() ({this.Session.PublishReceivedPacketIDs.Count()}) >= this.NegotiatedOptions.ClientReceiveMaximum ({this.NegotiatedOptions.ClientReceiveMaximum})");

                        var receivedPacket = new PublishReceivedPacketBuilder(this)
                            .WithPacketID(msg.PacketId)
                            .WithReasonCode(PublishAckAndReceivedReasonCodes.Success)
                            .Build();
                        this.Send(in receivedPacket);
                        break;
                }
            }

            applicationMessages.Clear();
        }

        private void HandleSubscribeAckPacket(Packet packet)
        {
            UInt16 packetId = (UInt16)packet.VariableHeaderFields[0].Integer;

            if (this.pendingSubscriptions.TryRemove(packetId, out var subscription))
            {
                if (subscription.Topics.Count != packet.Payload.Count)
                    Logger.Warning(nameof(MQTTClient), $"Subscription's (pID: {packetId}, sID: {subscription.ID}) topic count ({subscription.Topics.Count}) is different from Ack's payload count({packet.Payload.Count})!", this.Context);

                for (int i = 0; i < subscription.Topics.Count; i++)
                {
                    var topic = subscription.Topics[i];

                    try
                    {
                        topic.AcknowledgementCallback?.Invoke(this, topic, (SubscribeAckReasonCodes)packet.Payload[i].Integer);
                    }
                    catch (MQTTException ex)
                    {
                        Logger.Exception(nameof(MQTTClient), $"{nameof(topic.AcknowledgementCallback)}(\"{topic.Filter}\", {(SubscribeAckReasonCodes)packet.Payload[i].Integer})", ex, this.Context);
                        this.MQTTError(nameof(HandleSubscribeAckPacket), ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(nameof(MQTTClient), $"{nameof(topic.AcknowledgementCallback)}(\"{topic.Filter}\", {(SubscribeAckReasonCodes)packet.Payload[i].Integer})", ex, this.Context);
                    }
                }
            }
            else
                Logger.Warning(nameof(MQTTClient), $"No subscription could be found with packet ID {packetId}", this.Context);
        }

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901205
        /// </summary>
        private void HandleDisconnectPacket(Packet packet)
        {
            var properties = packet.VariableHeaderFields.Properties;
            // Byte 1 in the Variable Header is the Disconnect Reason Code. If the Remaining Length is less than 1 the value of 0x00 (Normal disconnection) is used.
            DisconnectReasonCodes reasonCode = packet.VariableHeaderFields.Count > 0 ? (DisconnectReasonCodes)packet.VariableHeaderFields[0].Integer : DisconnectReasonCodes.NormalDisconnection;

            string reason = null;

            var reasonProp = properties.Find(PacketProperties.ReasonString);
            if (reasonProp.Type != PacketProperties.None)
                reason = reasonProp.Data.UTF8String.Key;
            else
            {
                if (reasonCode != DisconnectReasonCodes.NormalDisconnection)
                    reason = $"Disconnect packet with reason code: {reasonCode}";
            }

            // "HandleDisconnectPacket - Code: KeepAliveTimeout, reason: \"The client was idle for too long without sending an MQTT control packet.\""
            Logger.Verbose(nameof(MQTTClient), $"{nameof(HandleDisconnectPacket)} - Code: {reasonCode}, reason: \"{reason}\"", this.Context);

            //if (reasonCode != DisconnectReasonCodes.NormalDisconnection)
            //    Error("Disconnect packet", reason);
            //else
            //{
            //    this.transport.BeginDisconnect();
            //    //this.State = ClientStates.Disconnected;
            //    SetDisconnected(reasonCode, reason);
            //}

            SetDisconnected(reasonCode, reason);

            this.Session.QueuedPackets.Clear(false);
        }

        private void HandleConnectAckPacket(Packet packet)
        {
            var msg = this.NegotiatedOptions.ServerOptions = new ServerConnectAckMessage(packet);

            Logger.Information(nameof(MQTTClient), $"ConnectAck - reason code received: {msg.ReasonCode}", this.Context);

            if (msg.ReasonCode == ConnectAckReasonCodes.Success)
            {
                // The Client or Server MUST set its initial send quota to a non-zero value not exceeding the Receive Maximum [MQTT-4.9.0-1].
                this._sendQuota = this._maxQuota = Math.Max((UInt16)(msg.ReceiveMaximum - 1), (UInt16)1);

                // https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901087
                // The Client Identifier which was assigned by the Server because a zero length Client Identifier was found in the CONNECT packet.
                if (!string.IsNullOrEmpty(msg.AssignedClientIdentifier))
                    this.Session = SessionHelper.Get(this.Options.Host, msg.AssignedClientIdentifier);

                // Client doesn't need to store pending QoS 1&2 messages: https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901230
                this.Session.QueuedPackets.Clear(false);

                // If a CONNECT packet is received with Clean Start is set to 1, the Client and Server MUST discard any existing Session and start a new Session [MQTT-3.1.2-4].

                // The Session Present flag informs the Client whether the Server is using Session State from a previous connection for this ClientID.
                // This allows the Client and Server to have a consistent view of the Session State. (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901078)

                // When a Client reconnects with Clean Start set to 0 and a session is present,
                // both the Client and Server MUST resend any unacknowledged PUBLISH packets (where QoS > 0) and PUBREL packets using their original Packet Identifiers.
                // This is the only circumstance where a Client or Server is REQUIRED to resend messages.
                // Clients and Servers MUST NOT resend messages at any other time [MQTT-4.4.0-1]. (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901238)
                if (!msg.SessionPresent)
                {
                    this.Session.PublishReceivedPacketIDs.Clear(false);
                    this.Session.PublishReleasedPacketIDs.Clear(false);

                    this.Session.UnacknowledgedPackets.Clear(false);
                }
                else
                {
                    MessageDeliveryRetry();
                }

                // A receiver MUST NOT carry forward any Topic Alias mappings from one Network Connection to another [MQTT-3.3.2-7].
                this.Session.ClientTopicAliasMapping.Clear(false);

                this.lastPacketSentAt = DateTime.Now;
                this.State = ClientStates.Connected;

                try
                {
                    this.OnConnected?.Invoke(this);
                }
                catch(Exception ex)
                {
                    Logger.Exception(nameof(MQTTClient), nameof(OnConnected), ex, this.Context);
                }

                this.connectBag?.completionSource?.TrySetResult(this);
            }

            try
            {
                this.OnServerConnectAckMessage?.Invoke(this, msg);
            }
            catch (Exception ex)
            {
                Logger.Exception(nameof(MQTTClient), nameof(OnServerConnectAckMessage), ex, this.Context);
            }

            if (msg.ReasonCode != ConnectAckReasonCodes.Success)
            {
                DisconnectReasonCodes SwitchCode(ConnectAckReasonCodes from) => Enum.IsDefined(typeof(DisconnectReasonCodes), (byte)from) ? (DisconnectReasonCodes)from : DisconnectReasonCodes.UnspecifiedError;

                string errorDescription = msg.ReasonString ?? $"Couldn't connect! Server sent reason code: {msg.ReasonCode} and reason: '{msg.ReasonString}'";
                Error("ConnectACK", SwitchCode(msg.ReasonCode), errorDescription);
            }

            this.connectBag = null;
        }

        /// <summary>
        /// When a Client reconnects with Clean Start set to 0 and a session is present,
        /// both the Client and Server MUST resend any unacknowledged PUBLISH packets (where QoS > 0) and PUBREL packets using their original Packet Identifiers. 
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901238
        /// </summary>
        private void MessageDeliveryRetry()
        {
            // Publish packets
            {
                // Re-send all unacknowledged packets when connected.
                // Ideally, the count of the packets is the same as the initial send quota sent by the server.
                // What could be problematic if the server sends a lower value.

                if (this._sendQuota == 0)
                    return;

                var (found, packetId, packet) = this.Session.UnacknowledgedPackets.GetNext(0);

                while (found)
                {
                    Logger.Verbose(nameof(MQTTClient), $"{nameof(MessageDeliveryRetry)} packetId: {packetId}, SendQuota: ({this._sendQuota})", this.Context);

                    this._sendQuota--;

                    // Set DUP flag to true (https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901102)
                    packet.Flags[3] = true;

                    // Switch Topic Alias to Topic Name
                    if (packet.VariableHeaderFields.Properties.TryFindData(PacketProperties.TopicAlias, DataTypes.TwoByteInteger, out var topicAliasData))
                    {
                        var topicName = this.Session.ClientTopicAliasMapping.Find((UInt16)topicAliasData.Integer);
                        if (string.IsNullOrEmpty(topicName))
                            Logger.Error(nameof(MQTTClient), $"{nameof(MessageDeliveryRetry)} packetId: {packetId} Topic Alias({(UInt16)topicAliasData.Integer}) found in packet but no Topic Name!", this.Context);

                        packet.VariableHeaderFields[0] = Data.FromString(topicName);

                        if (packet.VariableHeaderFields[2].Type == DataTypes.Property)
                            packet.VariableHeaderFields[2].Properties.RemoveProperty(PacketProperties.TopicAlias);
                        else
                            Logger.Error(nameof(MQTTClient), $"{nameof(MessageDeliveryRetry)} packetId: {packetId} Variable Header's second place expected to be Properties, but '{packet.VariableHeaderFields[2].Type}' found!", this.Context);
                    }

                    this.Send(in packet);

                    if (this._sendQuota == 0)
                        return;

                    (found, packetId, packet) = this.Session.UnacknowledgedPackets.GetNext(packetId);
                }
            }

            // Publish Release packets
            {
                var (found, packetId) = this.Session.PublishReleasedPacketIDs.GetNext(0);

                while (found)
                {
                    var outPacket = new PublishReleasePacketBuilder(this).WithPacketID(packetId).WithReasonCode(PublishReleaseAndCompleteReasonCodes.Success).Build();
                    this.Send(in outPacket);

                    (found, packetId) = this.Session.PublishReleasedPacketIDs.GetNext(packetId);
                }
            }
        }
    }
}
