using UnityEngine;

public class DroneFigureEightAndProps : MonoBehaviour
{
    [Header("Propellers")]
    [Tooltip("Assign the 4 propeller transforms here (order doesn't matter).")]
    public Transform[] propellers = new Transform[4];

    [Tooltip("Propeller speed in RPM.")]
    public float propellerRPM = 1000f;

    [Tooltip("Local axis to spin around (usually Vector3.up for FBX props).")]
    public Vector3 propellerLocalAxis = Vector3.up;

    [Tooltip("If true, alternates spin direction like a quadcopter (0/2 one way, 1/3 the other).")]
    public bool alternateSpinDirections = true;

    [Header("Figure-eight motion (horizontal plane)")]
    [Tooltip("Amplitude along world X axis (meters).")]
    public float amplitudeX = 2f;

    [Tooltip("Amplitude along world Z axis (meters).")]
    public float amplitudeZ = 2f;

    [Tooltip("Seconds for one full loop of the figure-eight.")]
    public float periodSeconds = 6f;

    [Tooltip("Optional offset from the start position (world space).")]
    public Vector3 centerOffset = Vector3.zero;

    [Tooltip("Start phase (radians).")]
    public float phase = 0f;

    private Vector3 _startPos;
    private float _fixedHeightY;
    private float _t;

    void Start()
    {
        _startPos = transform.position;
        _fixedHeightY = transform.position.y;
        _t = 0f;
    }

    void Update()
    {
        // --- Spin propellers ---
        // 1000 RPM => 1000 * 360 / 60 = 6000 deg/sec
        float degPerSec = propellerRPM * 6f;
        float step = degPerSec * Time.deltaTime;

        for (int i = 0; i < propellers.Length; i++)
        {
            var p = propellers[i];
            if (!p) continue;

            float dir = 1f;
            if (alternateSpinDirections)
            {
                // 0 & 2 spin forward, 1 & 3 spin backward (common quad layout)
                dir = (i % 2 == 0) ? 1f : -1f;
            }

            p.Rotate(propellerLocalAxis, dir * step, Space.Self);
        }

        // --- Figure-eight motion at current height ---
        // Lemniscate-style param:
        // x = Ax * sin(wt)
        // z = Az * sin(wt) * cos(wt) = (Az/2) * sin(2wt)
        float w = (periodSeconds <= 0.0001f) ? 1f : (2f * Mathf.PI / periodSeconds);
        _t += Time.deltaTime;

        float s = Mathf.Sin(w * _t + phase);
        float c = Mathf.Cos(w * _t + phase);

        float x = amplitudeX * s;
        float z = amplitudeZ * s * c;

        Vector3 center = _startPos + centerOffset;
        transform.position = new Vector3(center.x + x, _fixedHeightY, center.z + z);
    }
}
