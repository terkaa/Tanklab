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
    public GameObject QTMReference;
    public float  Xoffset;
    public float  Yoffset;
    public float  Zoffset;


    [SerializeField]
    private VirtualJoystick leftJoystick;
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


     // Update is called once per frame
    void Update()
    {
         if (leftJoystick.InputDirection != Vector3.zero)
    {
        HandleLeftJoystickDirection(leftJoystick.InputDirection);
    }
        else
        {
        //    PublishJointAndGripperValues(0);
        }
        

    }


     void HandleLeftJoystickDirection(Vector3 input)
{
    // Determine if the movement is more horizontal or vertical
    if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
    {
        // Horizontal movement (Left or Right)
        switch (input.x)
        {
            case float n when (n > 0):
                Debug.Log("Left Joystick moved Right");
                PublishJointAndGripperValues(3);
                break;
            case float n when (n < 0):
                Debug.Log("Left Joystick moved Left");
                PublishJointAndGripperValues(1);
                break;
        }
    }
    else
    {
        // Vertical movement (Up or Down)
        switch (input.y)
        {
            case float n when (n > 0):
                Debug.Log("Left Joystick moved Up");
                PublishJointAndGripperValues(1);
                break;
            case float n when (n < 0):
                Debug.Log("Left Joystick moved Down");
                PublishJointAndGripperValues(2);
                break;
        }
    }
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
        JSONNode LDrx = data[robotJointName][3].Value;
        JSONNode LDry = data[robotJointName][4].Value;
        JSONNode LDrz = data[robotJointName][5].Value;      
        //roboStatus P = JsonUtility.FromJson<roboStatus>(msg);
        Debug.Log("X: " + LDx + " Y: " + LDy + " LDz: " + LDz);
        //BlueROV = GameObject.Find(string.Format("Omron_LD250"));
        Debug.Log("Stage2\n");
        //calcX = 5.7f - (LDx / 1000);
        //if (float.TryParse(String.Format(data["roboPose"][0].Value), NumberStyles.Any, CultureInfo.InvariantCulture, out calcX))
        //{
        BlueROV.transform.position = new Vector3(Xoffset+LDx, Zoffset + LDz, Yoffset + LDy);
        BlueROV.transform.eulerAngles = new Vector3(LDrx,LDry,LDrz);
     
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
