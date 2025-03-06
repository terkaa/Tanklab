using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualJoystickMovement : MonoBehaviour
{
    [SerializeField]
    VirtualJoystick leftJoystick;

    [SerializeField]
    VirtualJoystick rightJoystick;

    public float acceleration = 50; // how fast you accelerate
    public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
    public float lookSensitivity = 1; // mouse look sensitivity
    public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input

    Vector3 velocity; // current velocity



    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (leftJoystick.InputDirection != Vector3.zero || rightJoystick.InputDirection != Vector3.zero)
            UpdateInput();



        // Physics
        velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;
    }

    private void UpdateInput()
    {
        velocity += GetAccelerationVector() * Time.deltaTime;

        Vector2 lookDelta = lookSensitivity * new Vector2(rightJoystick.InputDirection.x, -rightJoystick.InputDirection.y);
        Quaternion rotation = transform.rotation;
        Quaternion horiz = Quaternion.AngleAxis(lookDelta.x, Vector3.up);
        Quaternion vert = Quaternion.AngleAxis(lookDelta.y, Vector3.right);
        transform.rotation = horiz * rotation * vert;


    }

    private Vector3 GetAccelerationVector()
    {
        Vector3 moveInput = default;

        moveInput += leftJoystick.InputDirection.y * Vector3.forward;
        moveInput += leftJoystick.InputDirection.x * Vector3.right;
        
        Vector3 direction = transform.TransformVector(moveInput.normalized);

        return direction * acceleration;
    }
}
