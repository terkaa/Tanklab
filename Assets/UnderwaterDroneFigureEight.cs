using UnityEngine;
using UnityEngine.UI;

public class UnderwaterDroneFigureEight : MonoBehaviour
{
    [Header("Propellers")]
    public Toggle toggleSimulate;

    [Header("Propellers")]
    public Transform[] propellers = new Transform[4];
    public float propellerRPM = 1000f;
    public Vector3 propellerLocalAxis = Vector3.up;
    public bool alternateSpinDirections = true;

    [Header("Figure-eight path (world XZ plane)")]
    public float amplitudeX = 3f;
    public float amplitudeZ = 2f;
    public float periodSeconds = 10f;
    public Vector3 centerOffset = Vector3.zero;
    public float phase = 0f;

    [Header("Natural turning (body rotation)")]
    [Tooltip("How quickly the body aligns to direction of travel (deg/sec-ish). Higher = snappier.")]
    public float yawResponsiveness = 6f;

    [Tooltip("Max roll angle when turning (degrees).")]
    public float maxRollDegrees = 18f;

    [Tooltip("How strongly roll reacts to turning rate. Higher = more roll.")]
    public float rollFromTurnStrength = 1.2f;

    [Tooltip("How quickly roll returns/smooths.")]
    public float rollSmoothing = 6f;

    [Tooltip("If the model's forward axis isn't +Z, set an extra rotation here (e.g., (0,90,0)).")]
    public Vector3 modelForwardCorrectionEuler = Vector3.zero;

    [Header("Optional bob/slow sway")]
    public float yawNoiseDegrees = 0.75f;
    public float yawNoiseSpeed = 0.25f;

    private Vector3 _startPos;
    private float _fixedY;
    private float _t;

    private Vector3 _prevPos;
    private Vector3 _prevVel;
    private float _currentRoll;
    private bool Simulate;

    private Quaternion _modelForwardCorrection;

    void Start()
    {
        _startPos = transform.position;
        _fixedY = transform.position.y;
        _prevPos = transform.position;
        _prevVel = Vector3.forward;

        _modelForwardCorrection = Quaternion.Euler(modelForwardCorrectionEuler);

        if (toggleSimulate != null)
        {
            toggleSimulate.onValueChanged.AddListener(ToggleSimulate);
        }
        else
        {
            Debug.LogError("ToggleSimulation GameObject is not assigned.");
        }
    }

    private void ToggleSimulate(bool isOn)
    {
        Simulate = (isOn);
        Debug.Log($"Simulate: {(isOn ? "Enabled" : "Disabled")}");
    }


    void Update()
    {
        if(!Simulate)
            return;
        // ---- Prop spin (visual) ----
        float degPerSec = propellerRPM * 6f; // RPM -> deg/sec
        float step = degPerSec * Time.deltaTime;
        transform.position += Vector3.up * 1.0f;

        for (int i = 0; i < propellers.Length; i++)
        {
            var p = propellers[i];
            if (!p) continue;

            float dir = 1f;
            if (alternateSpinDirections)
                dir = (i % 2 == 0) ? 1f : -1f;

            p.Rotate(propellerLocalAxis, dir * step, Space.Self);
        }

        // ---- Figure-eight position (kinematic) ----
        float w = (periodSeconds <= 0.0001f) ? 1f : (2f * Mathf.PI / periodSeconds);
        _t += Time.deltaTime;

        float s = Mathf.Sin(w * _t + phase);
        float c = Mathf.Cos(w * _t + phase);

        // Lemniscate-like: x = Ax*sin(t), z = Az*sin(t)*cos(t)
        float x = amplitudeX * s;
        float z = amplitudeZ * s * c;

        Vector3 center = _startPos + centerOffset;
        Vector3 newPos = new Vector3(center.x + x, _fixedY, center.z + z);

        // ---- Compute velocity for natural orientation ----
        Vector3 vel = (newPos - _prevPos) / Mathf.Max(Time.deltaTime, 1e-6f);
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);

        // Move
        transform.position = newPos;

        // ---- Body yaw to face direction of travel + roll into turns ----
        if (velXZ.sqrMagnitude > 1e-6f)
        {
            Vector3 forward = velXZ.normalized;

            // Desired yaw rotation (world up)
            Quaternion desiredYaw = Quaternion.LookRotation(forward, Vector3.up);

            // Smooth yaw
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredYaw * _modelForwardCorrection,
                1f - Mathf.Exp(-yawResponsiveness * Time.deltaTime)
            );

            // Turning rate estimate (signed) using cross product around up axis
            Vector3 prevF = (_prevVel.sqrMagnitude > 1e-6f) ? _prevVel.normalized : forward;
            Vector3 currF = forward;

            float signedTurn = Vector3.SignedAngle(prevF, currF, Vector3.up); // degrees per frame
            float turnRate = signedTurn / Mathf.Max(Time.deltaTime, 1e-6f);   // deg/sec

            // Target roll: opposite sign typically feels “bank into turn”
            float targetRoll = Mathf.Clamp(-turnRate * rollFromTurnStrength, -maxRollDegrees, maxRollDegrees);

            _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, 1f - Mathf.Exp(-rollSmoothing * Time.deltaTime));

            // Apply roll around local forward axis after yaw
            transform.rotation = transform.rotation * Quaternion.AngleAxis(_currentRoll, Vector3.forward);

            _prevVel = currF;
        }

        // Small noise to reduce robotic feel (optional)
        if (yawNoiseDegrees > 0f)
        {
            float n = (Mathf.PerlinNoise(0f, Time.time * yawNoiseSpeed) - 0.5f) * 2f;
            transform.rotation = Quaternion.AngleAxis(n * yawNoiseDegrees, Vector3.up) * transform.rotation;
        }

        _prevPos = newPos;
    }
}
