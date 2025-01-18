using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using BestMQTT;
using BestMQTT.Packets.Builders;
using BestMQTT.Packets;

public abstract class MQTTConnection : MonoBehaviour
{
    [SerializeField]
    private string connectAddress = "rebo.centria.fi";

    [SerializeField]
    private int connectPort = 5349;

    [SerializeField]
    protected string robotJointName = "CNCPose";

    [Header("Receive (from)")]
    [SerializeField]
    private string receiveSubscribeTopic = "Topic/from";

  //  [SerializeField]
   // protected RobotJointAngleTransformer[] receiveRobotJoints;

    [Header("Send (to)")]
    [SerializeField]
    private string sendSubscribeTopic = "Topic/to";

   // [SerializeField]
//    protected RobotJointAngleTransformer[] sendRobotJoints;

    private MQTTClient client;
    private ApplicationMessagePacketBuilder sender;

    public bool ApplyReceivedValues { get; set; } = true; // When true, values received through MQTT are set to robot joints.

    public float[] JointsValues { get; private set; }

    public float[] SendJointValues { get; private set; }

    protected virtual void Awake()
    {
      //  JointsValues = new float[receiveRobotJoints.Length];
      //  SendJointValues = new float[sendRobotJoints.Length];
    }

    private void OnEnable()
    {
       // if (string.IsNullOrEmpty(receiveSubscribeTopic))
       // {
       //     return;
       // }

        client = new MQTTClientBuilder()
            .WithOptions(new ConnectionOptionsBuilder().WithWebSocket(connectAddress, connectPort).WithTLS())
            .WithEventHandler(OnConnected)
            .WithEventHandler(OnDisconnected)
            .WithEventHandler(OnStateChanged)
            .WithEventHandler(OnError)
            .CreateClient();
        client.BeginConnect(ConnectPacketBuilderCallback);
        Debug.Log("connected");
    }

    private ConnectPacketBuilder ConnectPacketBuilderCallback(MQTTClient client, ConnectPacketBuilder builder)
    {
        var host = client.Options.Host;
        if (!SessionHelper.HasAny(host))
        {
            Debug.Log("Creating null session!");
            builder = builder.WithSession(SessionHelper.CreateNullSession(host));
        }
        else
        {
            Debug.Log("A session already present for this host.");
        }
        return builder;
    }

    private void OnConnected(MQTTClient client)
    {
        if (!string.IsNullOrEmpty(receiveSubscribeTopic))
        {
            client.AddTopicAlias(receiveSubscribeTopic);

            client.CreateSubscriptionBuilder(receiveSubscribeTopic)
                .WithAcknowledgementCallback(OnSubscriptionAcknowledged)
                .WithMessageCallback(OnMessage)
                .WithMaximumQoS(QoSLevels.ExactlyOnceDelivery)
                .BeginSubscribe();
        }

        if (!string.IsNullOrEmpty(sendSubscribeTopic))
        {
            client.AddTopicAlias(sendSubscribeTopic);

            sender = client.CreateApplicationMessageBuilder(sendSubscribeTopic)
                .WithQoS(QoSLevels.ExactlyOnceDelivery);
        }
    }

    private void OnSubscriptionAcknowledged(MQTTClient client, SubscriptionTopic topic, SubscribeAckReasonCodes reasonCode)
    {
        if (reasonCode <= SubscribeAckReasonCodes.GrantedQoS2)
            Debug.Log($"Successfully subscribed with topic filter '{topic.Filter.OriginalFilter}'. QoS granted by the server: {reasonCode}");
        else
            Debug.Log($"Could not subscribe with topic filter '{topic.Filter.OriginalFilter}'! Error code: {reasonCode}");
    }

    protected abstract void OnMessage(MQTTClient client, SubscriptionTopic topic, string topicName, ApplicationMessage message);

   public void PublishJointAndGripperValues(float xposition, float yposition, float zposition, float spindlespeed, float coolant)
    {
        string jsonStr = GetValuesJSON(xposition, yposition, zposition, spindlespeed, coolant);
       // string jsonStr = "rqwrqr";
        Debug.Log("Publish msg: "+ jsonStr);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonStr);
        sender.WithPayload(bytes);
        sender.BeginPublish();
    }
   
    protected abstract string GetValuesJSON(float xposition, float yposition, float zposition, float spindlespeed, float coolant);

    private void OnStateChanged(MQTTClient client, ClientStates oldState, ClientStates newState)
    {
        Debug.Log($"{oldState} => {newState}");
    }

    private void OnDisconnected(MQTTClient client, DisconnectReasonCodes code, string reason)
    {
        Debug.Log($"OnDisconnected - code: {code}, reason: '{reason}'");
    }

    private void OnError(MQTTClient client, string reason)
    {
        Debug.Log($"OnError reason: '{reason}'");
    }

    private void OnDisable()
    {
        client?.CreateDisconnectPacketBuilder()
            .WithReasonCode(DisconnectReasonCodes.NormalDisconnection)
            .WithReasonString("Client disconnected.")
            .BeginDisconnect();
    }
}