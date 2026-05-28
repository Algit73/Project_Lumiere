using UnityEngine;

/// <summary>
/// Mobile-friendly camera controller for 3D scenes.
///
/// On Android/iOS:
///   - Uses the device gyroscope so tilting the phone rotates the view
///     (phone acts as a window into the 3D world).
///
/// In the Unity Editor / PC:
///   - Hold right-mouse-button + drag to look around (standard editor-style).
///
/// SETUP:
///   Attach to the Main Camera in any 3D scene, or let SceneReturnOnInput
///   add it automatically at runtime.
/// </summary>
public class MobileCameraController : MonoBehaviour
{
    [Header("Gyroscope (Mobile)")]
    [Tooltip("Smoothing applied to gyro rotation. Lower = snappier, higher = smoother.")]
    [SerializeField] private float gyroSmoothing = 0.1f;

    [Header("Mouse Look (Editor / PC)")]
    [SerializeField] private float mouseSensitivity = 2f;

    // ── State ──────────────────────────────────────────────────────────────────
    private bool       _gyroAvailable;
    private Quaternion _gyroOffset;       // corrects gyro coordinate system → Unity space
    private Quaternion _targetRotation;

    // ── Unity ──────────────────────────────────────────────────────────────────

    private void Start()
    {
        _targetRotation = transform.rotation;

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            _gyroAvailable     = true;

            // Gyro reports attitude in its own coordinate space.
            // This offset converts it to Unity's left-handed, Y-up coordinate space
            // with the camera initially facing "into" the scene.
            _gyroOffset = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            Debug.Log("[MobileCameraController] Gyroscope not available — using mouse look.");
        }
    }

    private void Update()
    {
        if (_gyroAvailable)
            UpdateGyro();
        else
            UpdateMouseLook();
    }

    // ── Private ────────────────────────────────────────────────────────────────

    private void UpdateGyro()
    {
        // Convert gyro attitude (right-hand, Z-forward) → Unity (left-hand, Y-up, Z-forward)
        Quaternion raw      = Input.gyro.attitude;
        Quaternion unified  = new Quaternion(raw.x, raw.y, -raw.z, -raw.w);
        _targetRotation     = unified * _gyroOffset;

        transform.rotation  = Quaternion.Slerp(transform.rotation, _targetRotation, gyroSmoothing);
    }

    private void UpdateMouseLook()
    {
        if (!Input.GetMouseButton(1)) return;   // only while right-mouse held

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        Vector3 euler = transform.eulerAngles;
        euler.y += mouseX;
        euler.x -= mouseY;
        euler.x  = ClampAngle(euler.x, -80f, 80f);

        transform.eulerAngles = euler;
    }

    /// <summary>Clamps an angle that may wrap around 360.</summary>
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
