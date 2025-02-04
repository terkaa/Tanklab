using UnityEngine;

public class CameraFollowOnClick : MonoBehaviour
{
    private Camera mainCamera;

    // Public references for the three viewpoint objects
    public GameObject viewpoint1;   // First viewpoint
    public GameObject viewpoint2;   // Second viewpoint
    public GameObject viewpoint3;   // Third viewpoint

    void Start()
    {
        // Get the main camera in the scene
        mainCamera = Camera.main;

    }

    void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Perform a raycast to detect the clicked object
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedViewpoint = hit.transform.gameObject;

                // Check if the clicked object is one of the viewpoints
                if (clickedViewpoint == viewpoint1 || clickedViewpoint == viewpoint2 || clickedViewpoint == viewpoint3)
                {

                    // Move the camera to the position and rotation of the clicked viewpoint
                    mainCamera.transform.position = hit.transform.position + new Vector3(0, 1.0f, 0);
                    mainCamera.transform.rotation = hit.transform.rotation;
                }
            }
        }
    }
}
