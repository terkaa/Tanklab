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

public class MQTTConnectionCNC : MQTTConnection
{
    [Header("ROV settings")]
    [SerializeField]
    private string robotGripperName = "gripper";
    public GameObject BlueROV;
    public GameObject QTMReference;
    public InputField QTMoffsetX;
    public InputField QTMoffsetY;
    public InputField QTMoffsetZ;
    public InputField QTMrotationX;
    public InputField QTMrotationY;
    public InputField QTMrotationZ;
    
    //New topic 8 lights on
    //9 lights off
    //10 Gripper close
    //11 Gripper open

    [SerializeField]
    private VirtualJoystick leftJoystick;
    [SerializeField]
    private VirtualJoystick rightJoystick;
    //public GameObject zaxisscaleObject;
    //private ArticulationBody xscale;
    //private ArticulationBody yscale;
    //private ArticulationBody zscale;
    [SerializeField]
    public Toggle toggleLights;
    public Toggle toggleGripper;

    private void Start()
    {

        if (toggleLights != null)
        {
            toggleLights.onValueChanged.AddListener(ToggleROVLights);
        }
        else
        {
            //Debug.LogError("ToggleLights GameObject is not assigned.");
        }

        if (toggleGripper != null)
        {
            toggleGripper.onValueChanged.AddListener(ToggleROVGripper);
        }
        else
        {
            //Debug.LogError("ToggleLights or Gripper GameObject is not assigned.");
        }


        if (QTMReference != null)
        {
            Vector3 position = QTMReference.transform.position;
            Vector3 rotation = QTMReference.transform.eulerAngles;

            QTMoffsetX.text = position.x.ToString("F3", CultureInfo.InvariantCulture);
            QTMoffsetY.text = position.y.ToString("F3", CultureInfo.InvariantCulture);
            QTMoffsetZ.text = position.z.ToString("F3", CultureInfo.InvariantCulture);

            QTMrotationX.text = rotation.x.ToString("F3", CultureInfo.InvariantCulture);
            QTMrotationY.text = rotation.y.ToString("F3", CultureInfo.InvariantCulture);
            QTMrotationZ.text = rotation.z.ToString("F3", CultureInfo.InvariantCulture);

            // Add event listeners to update transform when values change
            QTMoffsetX.onEndEdit.AddListener(delegate { UpdateTransform(); });
            QTMoffsetY.onEndEdit.AddListener(delegate { UpdateTransform(); });
            QTMoffsetZ.onEndEdit.AddListener(delegate { UpdateTransform(); });

            QTMrotationX.onEndEdit.AddListener(delegate { UpdateTransform(); });
            QTMrotationY.onEndEdit.AddListener(delegate { UpdateTransform(); });
            QTMrotationZ.onEndEdit.AddListener(delegate { UpdateTransform(); });
        }
        else
        {
            //Debug.LogError("QTMReference GameObject is not assigned.");
        }
    }


    // Update is called once per frame
    void Update()
    {

        if (rightJoystick.InputDirection == Vector3.zero && leftJoystick.InputDirection == Vector3.zero)
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
                    //Debug.Log("Left Joystick moved Right");
                    PublishJointAndGripperValues(4, toggleLights.isOn, toggleGripper.isOn);
                    break;
                case float n when (n < 0):
                    //Debug.Log("Left Joystick moved Left");
                    PublishJointAndGripperValues(3, toggleLights.isOn, toggleGripper.isOn);
                    break;
            }
        }
        else
        {
            // Vertical movement (Up or Down)
            switch (input.y)
            {
                case float n when (n > 0):
                    //Debug.Log("Left Joystick moved Up");
                    PublishJointAndGripperValues(1, toggleLights.isOn, toggleGripper.isOn);
                    break;
                case float n when (n < 0):
                    //Debug.Log("Left Joystick moved Down");
                    PublishJointAndGripperValues(2, toggleLights.isOn, toggleGripper.isOn);
                    break;
            }
        }
    }


    void HandleRightJoystickDirection(Vector3 input)
    {
        // Determine if the movement is more horizontal or vertical
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            // Horizontal movement (Left or Right)
            switch (input.x)
            {
                case float n when (n > 0):
                    //Debug.Log("Right Joystick moved Right");
                    PublishJointAndGripperValues(8, toggleLights.isOn, toggleGripper.isOn);
                    break;
                case float n when (n < 0):
                    //Debug.Log("Left Joystick moved Left");
                    PublishJointAndGripperValues(7, toggleLights.isOn, toggleGripper.isOn);
                    break;
            }
        }
        else
        {
            // Vertical movement (Up or Down)
            switch (input.y)
            {
                case float n when (n > 0):
                    //Debug.Log("Left Joystick moved Up");
                    PublishJointAndGripperValues(5, toggleLights.isOn, toggleGripper.isOn);
                    break;
                case float n when (n < 0):
                    //Debug.Log("Left Joystick moved Down");
                    PublishJointAndGripperValues(6, toggleLights.isOn, toggleGripper.isOn);
                    break;
            }
        }
    }

    private void ToggleROVLights(bool lightisOn)
    {
        PublishJointAndGripperValues(0, toggleLights.isOn, toggleGripper.isOn);
    }

    private void ToggleROVGripper(bool isOn)
    {
        PublishJointAndGripperValues(0, toggleLights.isOn, toggleGripper.isOn);
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
        BlueROV.transform.position = new Vector3(LDx, LDz+0.45f, LDy);
        BlueROV.transform.eulerAngles = new Vector3(LDrx, -LDrz+90.0f, LDry);
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

    private void UpdateTransform()
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
    }
}


