//using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls;
using BestMQTT;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
///using UnityEngine.InputSystem;
///


public class MQTTConnectionCNC : MQTTConnection
{
    [Header("ROV settings")]
    [SerializeField]
    private string robotGripperName = "gripper";
    public GameObject BlueROV;
    /* public GameObject QTMReference;
     public InputField QTMoffsetX;
     public InputField QTMoffsetY;
     public InputField QTMoffsetZ;
     public InputField QTMrotationX;
     public InputField QTMrotationY;
     public InputField QTMrotationZ;
     */
   

    private void Start()
    {

    }
        // Update is called once per frame
    void Update()
    {

       /* if (rightJoystick.InputDirection == Vector3.zero && leftJoystick.InputDirection == Vector3.zero)
        {
            //PublishJointAndGripperValues(0, toggleLights.isOn, toggleGripper.isOn);
        }

        else if (leftJoystick.InputDirection != Vector3.zero)
        {
            HandleLeftJoystickDirection(leftJoystick.InputDirection);
        }
        else if (rightJoystick.InputDirection != Vector3.zero)
        {
            HandleRightJoystickDirection(rightJoystick.InputDirection);
        }*/

       
    }


   
    protected override void OnMessage(MQTTClient client, SubscriptionTopic topic, string topicName, ApplicationMessage message)
    {

        var payload = Encoding.UTF8.GetString(message.Payload.Data, message.Payload.Offset, message.Payload.Count);
        var data = JSON.Parse(payload);
        //Debug.Log(data[0]);
        //Debug.Log(data[0][0]);

        //Debug.Log("Stage1\n");
        JSONNode LDx = data[robotJointName][0].Value;
        JSONNode LDy = data[robotJointName][1].Value;
        JSONNode LDz = data[robotJointName][2].Value;
        JSONNode LDrx = data[robotJointName][3].Value;
        JSONNode LDry = data[robotJointName][4].Value;
        JSONNode LDrz = data[robotJointName][5].Value;

        LDrx = 0.0f;
        //JSONNode LDdirection = data[robotJointName][6].Value;

        //roboStatus P = JsonUtility.FromJson<roboStatus>(msg);
        //Debug.Log("Positions X: " + LDx + " Y: " + LDz + " LDz: " + LDy + "\n");
        //BlueROV = GameObject.Find(10.103.141.123string.Format("Omron_LD250"));
        //Debug.Log("Stage2\n");
        //calcX = 5.7f - (LDx / 1000);
        //if (float.TryParse(String.Format(data["roboPose"][0].Value), NumberStyles.Any, CultureInfo.InvariantCulture, out calcX))
        //{
        //BlueROV.transform.position = new Vector3(-LDx*30.0f, (LDz + 0.45f) * 30.0f, -LDy * 30.0f);
        BlueROV.transform.position = new Vector3(-LDx, (LDz + 0.45f), -LDy);
        BlueROV.transform.eulerAngles = new Vector3(LDrx, -LDrz + 155.0f, LDry);
        //Debug.Log("Rotations X: " + LDrx + " Y: " + LDry + " LDz: " + LDrz+"\n");
        //}
        //Debug.Log("Stage3\n");
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

        jointsStr += $"{xposition.ToString("0.000", CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{yposition.ToString("0.000", CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{zposition.ToString("0.000", CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{spindlespeed.ToString("0.000", CultureInfo.InvariantCulture)}, ";
        jointsStr += $"{coolant.ToString("0.000", CultureInfo.InvariantCulture)}, ";

        jointsStr = $"\"{jointKey}\":[{jointsStr.Substring(0, jointsStr.Length - 2)}]";
        return $"{{{jointsStr}}}";
    }

    /*private void UpdateTransform()
    {
        if (QTMReference == null) return;

        float posX, posY, posZ, rotX, rotY, rotZ;

        if (float.TryParse(QTMoffsetX.text, NumberStyles.Float, CultureInfo.InvariantCulture, out posX) &&
            float.TryParse(QTMoffsetY.text, NumberStyles.Float, CultureInfo.InvariantCulture, out posY) &&
            float.TryParse(QTMoffsetZ.text, NumberStyles.Float, CultureInfo.InvariantCulture, out posZ))
        {
            QTMReference.transform.position = new Vector3(posX, posY, posZ);
        }

        if (float.TryParse(QTMrotationX.text, NumberStyles.Float, CultureInfo.InvariantCulture, out rotX) &&
            float.TryParse(QTMrotationY.text, NumberStyles.Float, CultureInfo.InvariantCulture, out rotY) &&
            float.TryParse(QTMrotationZ.text, NumberStyles.Float, CultureInfo.InvariantCulture, out rotZ))
        {
            QTMReference.transform.eulerAngles = new Vector3(rotX, rotY, rotZ);
        }
    }*/
}


