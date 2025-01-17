using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BestHTTP.PlatformSupport.Memory;

using BestMQTT;
using BestMQTT.Examples;
using BestMQTT.Examples.Helpers;
using BestMQTT.Packets;
using BestMQTT.Packets.Builders;

using UnityEngine;

public partial class GenericClient
{
    private MQTTClient client;

    // UI instances of SubscriptionListItem
    private List<SubscriptionListItem> subscriptionListItems = new List<SubscriptionListItem>();

    public void OnConnectButton()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (this.transportDropdown.value == 0)
        {
            AddText("<color=red>TCP transport isn't available under WebGL!</color>");
            return;
        }
#endif

        SetConnectingUI();

        var host = this.hostInput.GetValue("broker.mqttdashboard.com");
        AddText($"[{host}] Connecting with client id: <color=green>{SessionHelper.Get(host).ClientId}</color>");

        var options = new ConnectionOptions();
        options.Host = host;
        options.Port = this.portInput.GetIntValue(1883);
        options.Transport = (SupportedTransports)this.transportDropdown.value;
        options.UseTLS = this.isSecureToggle.GetBoolValue();
        options.Path = this.pathInput.GetValue("/mqtt");
        options.ProtocolVersion = (SupportedProtocolVersions)this.protocolVersionDropdown.value;

        this.client = new MQTTClient(options);

        this.client.OnConnected += OnConnected;
        this.client.OnError += OnError;
        this.client.OnDisconnect += OnDisconnected;
        this.client.OnStateChanged += OnStateChanged;

        this.client.BeginConnect(ConnectPacketBuilderCallback);
    }

    private void OnConnected(MQTTClient client)
    {
        SetConnectedUI();
    }

    private void OnDisconnected(MQTTClient client, DisconnectReasonCodes code, string reason)
    {
        SetDisconnectedUI();

        AddText($"[{client.Options.Host}] OnDisconnected - code: <color=blue>{code}</color>, reason: <color=red>{reason}</color>");
    }

    private void OnError(MQTTClient client, string reason)
    {
        AddText($"[{client.Options.Host}] OnError reason: <color=red>{reason}</color>");
    }

    public void OnDisconnectButton()
    {
        this.connectButton.interactable = false;
        this.client?.CreateDisconnectPacketBuilder().BeginDisconnect();
    }

    private void OnStateChanged(MQTTClient client, ClientStates oldState, ClientStates newState)
    {
        AddText($"[{client.Options.Host}] <color=yellow>{oldState}</color> => <color=green>{newState}</color>");
    }

    private ConnectPacketBuilder ConnectPacketBuilderCallback(MQTTClient client, ConnectPacketBuilder builder)
    {
        AddText($"[{client.Options.Host}] Creating connect packet.");

        var userName = this.userNameInput.GetValue(null);
        var password = this.passwordInput.GetValue(null);

        var session = SessionHelper.HasAny(client.Options.Host) ? SessionHelper.Get(client.Options.Host) : SessionHelper.CreateNullSession(client.Options.Host);
        builder.WithSession(session);

        if (!string.IsNullOrEmpty(userName))
            builder.WithUserName(userName);

        if (!string.IsNullOrEmpty(password))
            builder.WithPassword(password);

        builder.WithKeepAlive((ushort)this.keepAliveInput.GetIntValue(60));

        // setup last-will

        var lastWillTopic = this.lastWill_TopicInput.GetValue(null);
        var lastWillMessage = this.lastWill_MessageInput.GetValue(null);
        var retain = this.lastWill_RetainToggle.GetBoolValue();

        if (!string.IsNullOrEmpty(lastWillTopic) && !string.IsNullOrEmpty(lastWillMessage))
            builder.WithLastWill(new LastWillBuilder()
                                    .WithTopic(lastWillTopic)
                                    .WithContentType("text/utf-8")
                                    .WithPayload(Encoding.UTF8.GetBytes(lastWillMessage))
                                    .WithQoS(this.lastWill_QoSDropdown.GetQoS())
                                    .WithRetain(retain));

        return builder;
    }

    public void OnPublishButtonClicked()
    {
        string topic = this.publish_TopicInput.GetValue("best_mqtt/test");
        QoSLevels qos = this.publish_QoSDropdown.GetQoS();
        bool retain = this.publish_RetainToggle.GetBoolValue();
        string message = this.publish_MessageInput.GetValue("Hello MQTT World...");

        this.client.CreateApplicationMessageBuilder(topic)
                      .WithQoS(qos)
                      .WithRetain(retain)
                      .WithPayload(message)
                      .BeginPublish();
    }    

    public void OnSubscribeButtonClicked()
    {
        var colorValue = this.subscribe_ColorInput.GetValue("000000");
        if (!ColorUtility.TryParseHtmlString("#" + colorValue, out var color))
        {
            AddText($"[{client.Options.Host}] <color=red>Couldn't parse '#{colorValue}'</color>");
            return;
        }

        var qos = this.subscribe_QoSDropdown.GetQoS();
        var topic = this.subscribe_TopicInput.GetValue("best_mqtt/#");

        this.client?.CreateBulkSubscriptionBuilder()
                        .WithTopic(new SubscribeTopicBuilder(topic)
                            .WithMaximumQoS(qos)
                            .WithAcknowledgementCallback(OnSubscriptionAcknowledgement)
                            .WithMessageCallback(OnApplicationMessage))
                        .BeginSubscribe();

        AddText($"[{client.Options.Host}] Subscribe request for topic <color=#{colorValue}>{topic}</color> sent...");

        AddSubscriptionUI(topic, colorValue);
    }

    private void OnSubscriptionAcknowledgement(MQTTClient client, SubscriptionTopic topic, SubscribeAckReasonCodes reasonCode)
    {
        var subscription = FindSubscriptionItem(topic.Filter.OriginalFilter);

        string reasonColor = reasonCode <= SubscribeAckReasonCodes.GrantedQoS2 ? "green" : "red";
        AddText($"[{client.Options.Host}] Subscription request to topic <color=#{subscription.Color}>{topic.Filter.OriginalFilter}</color> returned with reason code: <color={reasonColor}>{reasonCode}</color>");
    }

    private void AddSubscriptionUI(string topic, string color)
    {
        var item = Instantiate<SubscriptionListItem>(this.subscription_ListItem, this.subscribe_ListItemRoot);
        item.Set(this, topic, color);

        this.subscriptionListItems.Add(item);
    }

    public void Unsubscribe(string topic)
    {
        this.client.CreateUnsubscribePacketBuilder(topic)
                        .WithAcknowledgementCallback(OnUnsubscribed)
                        .BeginUnsubscribe();

        var subscription = FindSubscriptionItem(topic);

        AddText($"[{client.Options.Host}] Unsubscribe request for topic <color=#{subscription.Color}>{topic}</color> sent...");
    }

    private void OnUnsubscribed(MQTTClient client, string topic, BestMQTT.Packets.UnsubscribeAckReasonCodes reason)
    {
        var instance = this.subscriptionListItems.FirstOrDefault(s => s.Topic.OriginalFilter == topic);
        this.subscriptionListItems.Remove(instance);
        Destroy(instance.gameObject);

        string reasonColor = reason == UnsubscribeAckReasonCodes.Success ? "green" : "red";

        AddText($"[{client.Options.Host}] Unsubscription request to topic <color=#{instance.Color}>{topic}</color> returned with reason code: <color={reasonColor}>{reason}</color>");
    }

    private void OnApplicationMessage(MQTTClient client, SubscriptionTopic topic, string topicName, ApplicationMessage applicationMessage)
    {
        // find matching subscription for its color
        var subscription = FindSubscriptionItem(topicName);

        string payload = string.Empty;
        
        // Here we going to try to convert the payload as an UTF-8 string. Note that it's not guaranteed that the payload is a string!
        // While MQTT supports an additional Content-Type field in this demo we can't rely on its presense.
        if (applicationMessage.Payload != BufferSegment.Empty)
        {
            payload = Encoding.UTF8.GetString(applicationMessage.Payload.Data, applicationMessage.Payload.Offset, applicationMessage.Payload.Count);

            const int MaxPayloadLength = 512;
            if (payload.Length > MaxPayloadLength)
                payload = payload?.Remove(MaxPayloadLength);
        }

        // Display the Content-Type if present
        string contentType = string.Empty;
        if (applicationMessage.ContentType != null)
            contentType = $" ({applicationMessage.ContentType}) ";

        // Add the final text to the demo's log view.
        AddText($"[{client.Options.Host}] <color=#{subscription.Color}>[{topicName}] {contentType}{payload}</color>");
    }

    private SubscriptionListItem FindSubscriptionItem(string topicName) => this.subscriptionListItems.FirstOrDefault(s => s.Topic.IsMatching(topicName));

    private void OnDestroy()
    {
        this.client?.CreateDisconnectPacketBuilder().BeginDisconnect();
    }
}
