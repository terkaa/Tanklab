using UnityEngine;
using UnityEngine.UI;

public class CameraActivator : MonoBehaviour
{
    // Reference to the Unity Button on the canvas
    public Button ridebutton;
    public Button cancelridebutton;
    public Toggle togglewater;
    public Toggle toggleceiling;
    public Toggle togglQTMreference;

    // Reference to the camera component to be activated
    public Camera rideCamera;

    // Reference to the main camera to be deactivated
    public Camera mainCamera;

    // Reference to the joystick component to be activated
    public GameObject ROVJoystick;

    // References to the joystick components to be deactivated
    public GameObject LeftJoystick;
    public GameObject RightJoystick;
    public GameObject Water;
    public GameObject Ceiling;
    public GameObject QTMReference;
    public GameObject QTMReferenceInputs;
    // References to the GameObjects to be toggle
    public InputField MessageInput; 
    private string message;
    private bool lights;
    private bool gripper;
    


    private void Start()
    {
        // Ensure references are assigned
        if (ridebutton == null || cancelridebutton == null || rideCamera == null || mainCamera == null)
        {
            Debug.LogError("RideButton, CancelRideButton, RideCamera, or MainCamera is not assigned in the inspector.");
            return;
        }

        // Disable the ride camera initially
        rideCamera.gameObject.SetActive(false);

        // Hide the cancel button initially
        cancelridebutton.gameObject.SetActive(false);

        // Add listeners to the buttons' onClick events
        ridebutton.onClick.AddListener(ActivateRideCamera);
        cancelridebutton.onClick.AddListener(DeactivateRideCamera);

        // Add listener to the messageInput change
        cancelridebutton.onClick.AddListener(DeactivateRideCamera);
       

        // Ensure toggles and related objects are assigned
        if (togglewater != null && Water != null)
        {
            togglewater.onValueChanged.AddListener(ToggleWaterVisibility);
        }
        else
        {
            Debug.LogError("ToggleWater or Water GameObject is not assigned.");
        }

        if (toggleceiling != null && Ceiling != null)
        {
            toggleceiling.onValueChanged.AddListener(ToggleCeilingVisibility);
        }
        else
        {
            Debug.LogError("ToggleCeiling or Ceiling GameObject is not assigned.");
        }
        if (togglQTMreference != null && QTMReference != null)
        {
            togglQTMreference.onValueChanged.AddListener(ToggleQTMreferenceVisibility);
        }
        else
        {
            Debug.LogError("ToggleQTMReference or QTMReference GameObject is not assigned.");
        }


       // Add listener for InputField (checking for Enter key press)
        if (MessageInput != null)
        {
            MessageInput.onEndEdit.AddListener(OnMessageInputEndEdit);
        }
    }

    private void ActivateRideCamera()
    {
        // Activate the ride camera
        rideCamera.gameObject.SetActive(true);

        // Activate the ride joystick, deactivate left and right
        ROVJoystick.gameObject.SetActive(true);
        LeftJoystick.gameObject.SetActive(false);
        RightJoystick.gameObject.SetActive(false);

        // Deactivate the main camera
        mainCamera.gameObject.SetActive(false);

        // Hide the ride button and show the cancel button
        ridebutton.gameObject.SetActive(false);
        cancelridebutton.gameObject.SetActive(true);

        Debug.Log("RideCamera activated, MainCamera deactivated, and buttons updated.");
    }

    private void DeactivateRideCamera()
    {
        // Deactivate the ride camera
        rideCamera.gameObject.SetActive(false);

        // Deactivate the ride joystick and activate the left and right
        ROVJoystick.gameObject.SetActive(false);
        LeftJoystick.gameObject.SetActive(true);
        RightJoystick.gameObject.SetActive(true);

        // Activate the main camera
        mainCamera.gameObject.SetActive(true);

        // Show the ride button and hide the cancel button
        ridebutton.gameObject.SetActive(true);
        cancelridebutton.gameObject.SetActive(false);

        Debug.Log("RideCamera deactivated, MainCamera activated, and buttons updated.");
    }

    private void ToggleWaterVisibility(bool isOn)
    {
        Water.SetActive(isOn);
        Debug.Log($"Water visibility toggled: {(isOn ? "Enabled" : "Disabled")}");
    }

    private void ToggleCeilingVisibility(bool isOn)
    {
        Ceiling.SetActive(!isOn);
        Debug.Log($"Ceiling visibility toggled: {(isOn ? "Enabled" : "Disabled")}");
    }

    private void ToggleQTMreferenceVisibility(bool isOn)
    {
        QTMReference.SetActive(!isOn);
        QTMReferenceInputs.SetActive(!isOn);
        Debug.Log($"QTMReference visibility toggled: {(isOn ? "Enabled" : "Disabled")}");
    }

       private void OnMessageInputEndEdit(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Debug.Log("Message Sent: " + text);
            MessageInput.text = ""; // Optionally clear the input field after sending
        }
    }

    private void OnDestroy()
    {
        // Remove the listeners when the object is destroyed to avoid memory leaks
        if (ridebutton != null)
        {
            ridebutton.onClick.RemoveListener(ActivateRideCamera);
        }

        if (cancelridebutton != null)
        {
            cancelridebutton.onClick.RemoveListener(DeactivateRideCamera);
        }

        if (togglewater != null)
        {
            togglewater.onValueChanged.RemoveListener(ToggleWaterVisibility);
        }

        if (toggleceiling != null)
        {
            toggleceiling.onValueChanged.RemoveListener(ToggleCeilingVisibility);
        }
    }
}
