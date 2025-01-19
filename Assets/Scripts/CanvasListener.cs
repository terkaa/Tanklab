using UnityEngine;
using UnityEngine.UI;

public class CameraActivator : MonoBehaviour
{
    // Reference to the Unity Button on the canvas
    public Button ridebutton;
    public Button cancelridebutton;

    // Reference to the camera component to be activated
    public Camera rideCamera;

    // Reference to the main camera to be deactivated
    public Camera mainCamera;

    // Reference to the joystick component to be activated
    public GameObject ROVJoystick;

    // References to the joystick components to be deactivated
    public GameObject LeftJoystick;
    public GameObject RightJoystick;


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
    }

    private void ActivateRideCamera()
    {
        // Activate the ride camera
        rideCamera.gameObject.SetActive(true);

        // Activate the ride joystick deactivate left and right
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
    }
}
