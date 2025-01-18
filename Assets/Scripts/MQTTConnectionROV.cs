//using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls;
using BestMQTT;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
///using UnityEngine.InputSystem;

public class MQTTConnectionCNC : MQTTConnection
{
    [Header("ROV settings")]
    [SerializeField]
    private string robotGripperName = "gripper";
    public GameObject BlueROV;
    public float  Xoffset;
    public float  Yoffset;
    public float  Zoffset;

    //public GameObject zaxisscaleObject;

    //private ArticulationBody xscale;
    //private ArticulationBody yscale;
    //private ArticulationBody zscale;

    private void Start()
    {
        //if (xaxisscaleObject != null && yaxisscaleObject != null && zaxisscaleObject != null)
        //{
        //    xscale = xaxisscaleObject.GetComponent<ArticulationBody>();
        //    yscale = yaxisscaleObject.GetComponent<ArticulationBody>();
        //    zscale = zaxisscaleObject.GetComponent<ArticulationBody>();
       // }
    }
    protected override void OnMessage(MQTTClient client, SubscriptionTopic topic, string topicName, ApplicationMessage message)
    {

        var payload = Encoding.UTF8.GetString(message.Payload.Data, message.Payload.Offset, message.Payload.Count);
        var data = JSON.Parse(payload);
        Debug.Log(data[0]);
        Debug.Log(data[0][0]);

        Debug.Log("Stage1\n");
        JSONNode LDx = data[robotJointName][0].Value;
        JSONNode LDy = data[robotJointName][1].Value;
        JSONNode LDz = data[robotJointName][2].Value;
        //roboStatus P = JsonUtility.FromJson<roboStatus>(msg);
        Debug.Log("X: " + LDx + " Y: " + LDy + " LDz: " + LDz);
        //BlueROV = GameObject.Find(string.Format("Omron_LD250"));
        Debug.Log("Stage2\n");
        //calcX = 5.7f - (LDx / 1000);
        //if (float.TryParse(String.Format(data["roboPose"][0].Value), NumberStyles.Any, CultureInfo.InvariantCulture, out calcX))
        //{
        BlueROV.transform.position = new Vector3(Xoffset+LDx, Zoffset + LDz, Yoffset + LDy);
        //}
        Debug.Log("Stage3\n");
    }

    
    protected override string GetValuesJSON(float xposition, float yposition, float zposition, float spindlespeed, float coolant)
    {
        string jointKey = robotJointName;
        float[] jointValues = SendJointValues;
        string jointsStr = "";

        /*jointsStr += $"{xposition.ToString(CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{yposition.ToString(CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{zposition.ToString(CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{spindlespeed.ToString(CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{coolant.ToString(CultureInfo.InvariantCulture)}, ";
        */

        jointsStr += $"{xposition.ToString("0.000",CultureInfo.InvariantCulture)}, ";
            jointsStr += $"{yposition.ToString("0.000",CultureInfo.InvariantCulture)}, ";
            jointsStr += $"{zposition.ToString("0.000",CultureInfo.InvariantCulture)}, ";
            jointsStr += $"{spindlespeed.ToString("0.000",CultureInfo.InvariantCulture)}, ";
            jointsStr += $"{coolant.ToString("0.000",CultureInfo.InvariantCulture)}, ";
        
        jointsStr = $"\"{jointKey}\":[{jointsStr.Substring(0, jointsStr.Length - 2)}]";
        return $"{{{jointsStr}}}";
    }

}
