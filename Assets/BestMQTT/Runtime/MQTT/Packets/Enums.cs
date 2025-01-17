using System;

namespace BestMQTT.Packets
{
    public enum PacketTypes : byte
    {
        Reserved,
        Connect,
        ConnectAck,
        Publish,
        PublishAck,
        PublishReceived,
        PublishRelease,
        PublishComplete,
        Subscribe,
        SubscribeAck,
        Unsubscribe,
        UnsubscribeAck,
        PingRequest,
        PingResponse,
        Disconnect,
        Auth
    }

    public enum PacketProperties : uint
    {
        None = 0x00,
        PayloadFormatIndicator = 0x01,
        MessageExpiryInterval = 0x02,
        ContentType = 0x03,
        ResponseTopic = 0x08,
        CorrelationData = 0x09,
        SubscriptionIdentifier = 0x0B,
        SessionExpiryInterval = 0x11,
        AssignedClientIdentifier = 0x12,
        ServerKeepAlive = 0x13,
        AuthenticationMethod = 0x15,
        AuthenticationData = 0x16,
        RequestProblemInformation = 0x17,
        WillDelayInterval = 0x18,
        RequestResponseInformation = 0x19,
        ResponseInformation = 0x1A,
        ServerReference = 0x1C,
        ReasonString = 0x1F,
        ReceiveMaximum = 0x21,
        TopicAliasMaximum = 0x22,
        TopicAlias = 0x23,
        MaximumQoS = 0x24,
        RetainAvailable = 0x25,
        UserProperty = 0x26,
        MaximumPacketSize = 0x27,
        WildcardSubscriptionAvailable = 0x28,
        SubscriptionIdentifierAvailable = 0x29,
        SharedSubscriptionAvailable = 0x2A
    }

    /// <summary>
    /// Quality of Service Levels
    /// <see cref="https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901103"/>
    /// </summary>
    public enum QoSLevels : byte
    {
        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901235
        /// The message is delivered according to the capabilities of the underlying network. No response is sent by the receiver and no retry is performed by the sender. The message arrives at the receiver either once or not at all.
        /// </summary>
        AtMostOnceDelivery = 0b00,

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901236
        /// This Quality of Service level ensures that the message arrives at the receiver at least once. 
        /// </summary>
        AtLeastOnceDelivery = 0b01,

        /// <summary>
        /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901237
        /// This is the highest Quality of Service level, for use when neither loss nor duplication of messages are acceptable. There is an increased overhead associated with QoS 2.
        /// </summary>
        ExactlyOnceDelivery = 0b10,

        /// <summary>
        /// Must not be used!
        /// </summary>
        Reserved = 0b11
    }

    /// <summary>
    /// https://docs.oasis-open.org/mqtt/mqtt/v5.0/os/mqtt-v5.0-os.html#_Toc3901169
    /// </summary>
    public enum RetaionHandlingOptions : byte
    {
        /// <summary>
        /// Send retained messages at the time of the subscribe.
        /// </summary>
        SendWhenSubscribe,

        /// <summary>
        /// Send retained messages at subscribe only if the subscription does not currently exist.
        /// </summary>
        SendWhenSubscribeIfSubscriptionDoesntExist,

        /// <summary>
        /// Do not send retained messages at the time of the subscribe
        /// </summary>
        DoNotSendRetainedMessages
    }

    public enum PayloadTypes : byte
    {
        Bytes = 0b00,
        UTF8 = 0b01
    }

    public enum SubscribeAckReasonCodes
    {
        GrantedQoS0 = 0x00,
        GrantedQoS1 = 0x01,
        GrantedQoS2 = 0x02,

        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,

        TopicFilterInvalid = 0x8F,
        PacketIdentifierInUse = 0x91,

        QuotaExceeded = 0x97,
        SharedSubscriptionsNotSupported = 0x9E,

        SubscriptionIdentifiersNotSupported = 0xA1,
        WildcardSubscriptionsNotSupported = 0xA2,
    }

    public enum PublishAckAndReceivedReasonCodes
    {
        Success = 0x00,
        NoMatchingSubscribers = 0x10,

        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,

        TopicNameInvalid = 0x90,
        PacketIdentifierInUse = 0x91,
        QuotaExceeded = 0x97,
        PayloadFormatInvalid = 0x99,
    }

    public enum PublishReleaseAndCompleteReasonCodes
    {
        Success = 0x00,

        PacketIdentifierNotFound = 0x92
    }

    public enum UnsubscribeAckReasonCodes
    {
        Success = 0x00,
        NoSubscriptionExisted = 0x11,

        UnspecifiedError = 0x80,
        ImplementationSpecificError = 0x83,
        NotAuthorized = 0x87,

        TopicFilterInvalid = 0x8F,
        PacketIdentifierInUse = 0x91,
    }

    public enum AuthReasonCodes
    {
        Success = 0x00,

        ContinueAuthentication = 0x18,
        ReAuthenticate = 0x19
    }

    public enum ConnectAckReasonCodes : byte
    {
        Success = 0x00,
        UnspecifiedError = 0x80,
        MalformedPacket = 0x81,
        ProtocolError = 0x82,
        ImplementationSpecificError = 0x83,
        UnsupportedProtocolVersion = 0x84,
        ClientIdentifierNotValid = 0x85,
        BadUserNameOrPassword = 0x86,
        NotAuthorized = 0x87,
        ServerBusy = 0x89,
        Banned = 0x8A,
        BadAuthenticationMethod = 0x8C,
        TopicNameInvalid = 0x90,
        PacketTooLarge = 0x95,
        QuotaExceeded = 0x97,
        PayloadFormatInvalid = 0x99,
        RetainNotSupported = 0x9A,
        QoSNotSupported = 0x9B,
        UseAnotherServer = 0x9C,
        ServerMoved = 0x9D,
        ConnectionRateExceeded = 0x9F,
    }
}
