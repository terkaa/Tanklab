using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

using BestMQTT;
using BestMQTT.Packets.Builders;
using BestMQTT.Packets;
using System;
using System.Text;
using SimpleJSON;


public class MQTT_LD250 : MonoBehaviour
{
    public static MQTTClient client;
    public GameObject BlueROV;
    //public GameObject robotJoint;
    public static bool ConnectDT;

    // Start is called before the first frame update
    void Start()
    {
        ConnectDT = false;
        client = new MQTTClientBuilder()
        .WithOptions(new ConnectionOptionsBuilder().WithWebSocket("rebo.centria.fi", 5349).WithTLS())
        .WithEventHandler(OnConnected)
        .WithEventHandler(OnDisconnected)
        .WithEventHandler(OnStateChanged)
        .WithEventHandler(OnError)
        .CreateClient();
        client.BeginConnect(ConnectPacketBuilderCallback);
    }

   
    private void OnConnected(MQTTClient client)
    {

        client.CreateBulkSubscriptionBuilder()
        .WithTopic(new SubscribeTopicBuilder("BlueROV/current")
        .WithMessageCallback(OnBlueROVMessage))
        //.WithAcknowledgementCallback(OnSubscriptionAcknowledged)
      //  .WithMaximumQoS(QoSLevels.ExactlyOnceDelivery)
     //   .WithTopic(new SubscribeTopicBuilder("urTopic/from")
     //   .WithMessageCallback(OnURMessage))
     // .WithAcknowledgementCallback(OnSubscriptionAcknowledged)
     //   .WithMaximumQoS(QoSLevels.ExactlyOnceDelivery)
      //  .WithTopic(new SubscribeTopicBuilder("urShadowTopic/from")
      //  .WithMessageCallback(OnURShadowMessage))
        .BeginSubscribe();

        //client.AddTopicAlias("ldTopic/from");
        //client.CreateSubscriptionBuilder("ldTopic/from")
        //.WithMessageCallback(OnMessage)
        //.WithAcknowledgementCallback(OnSubscriptionAcknowledged)
        //.WithMaximumQoS(QoSLevels.ExactlyOnceDelivery)
        //.BeginSubscribe();

        // client.CreateApplicationMessageBuilder("best_mqtt/test_topic")
        // .WithPayload("Hello MQTT World!")
        // .WithQoS(QoSLevels.ExactlyOnceDelivery)
        // .WithContentType("text/plain; charset=UTF-8")
        // .BeginPublish();
    }
    private void OnBlueROVMessage(MQTTClient client, SubscriptionTopic topic, string topicName, ApplicationMessage message)
    {
        //public GameObject robojoint1 = UR3_Joint2; 
        // Convert the raw payload to a string
        Debug.Log("Topic:" + topicName);
        float calcX = 0;
        var payload = Encoding.UTF8.GetString(message.Payload.Data, message.Payload.Offset, message.Payload.Count);
        Debug.Log($"Content-Type: '{message.ContentType}' Payload: '{payload}'");
            JSONNode data = JSON.Parse(payload);
            //Debug.Log("Robopose:" + data["roboPose"]);
            //if (data["roboPose"] is Array)
            //{
                Debug.Log("Stage1\n");
                JSONNode LDx = data["roboPose"][0].Value;
                JSONNode LDy = data["roboPose"][1].Value;
                JSONNode LDrot = data["roboPose"][2].Value;
                //roboStatus P = JsonUtility.FromJson<roboStatus>(msg);
                Debug.Log("X: " + LDx + " Y: " + LDy + " LDrot: " + LDrot);
                //BlueROV = GameObject.Find(string.Format("Omron_LD250"));
                Debug.Log("Stage2\n");
                calcX = 5.7f - (LDx / 1000);
                Debug.Log(calcX);
                //if (float.TryParse(String.Format(data["roboPose"][0].Value), NumberStyles.Any, CultureInfo.InvariantCulture, out calcX))
                //{
                    transform.position = new Vector3(5.8f - (LDx / 1000), 0, 2.0f - (LDy / 1000));
                //}
                Debug.Log("Stage3\n");
       // }
        
        // client.CreateUnsubscribePacketBuilder("best_mqtt/test_topic")
       //     .WithAcknowledgementCallback((client, topicFilter, reasonCode) => Debug.Log($"Unsubscribe request to topic filter '{topicFilter}' returned with code: { reasonCode}"))
       //     .BeginUnsubscribe();
    }

    
    private void OnSubscriptionAcknowledged(MQTTClient client, SubscriptionTopic topic, SubscribeAckReasonCodes reasonCode)
    {
        if (reasonCode <= SubscribeAckReasonCodes.GrantedQoS2)
            Debug.Log($"Successfully subscribed with topic filter '{topic.Filter.OriginalFilter}'. QoS granted by the server: {reasonCode}");
        else
            Debug.Log($"Could not subscribe with topic filter '{topic.Filter.OriginalFilter}'! Error code: {reasonCode}");
    }
    private void OnDestroy()
    {
        client.CreateDisconnectPacketBuilder()
        .WithReasonCode(DisconnectReasonCodes.NormalDisconnection)
        .WithReasonString("Bye")
        .BeginDisconnect();
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
            Debug.Log("A session already present for this host.");
        return builder;
    }
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
    //Detect collisions between the GameObjects with Colliders attached
    void OnCollisionStay(Collision col)
    {
        Debug.Log("Collision11");
     
    }
}