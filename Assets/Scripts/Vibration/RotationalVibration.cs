using UnityEngine;

public class RotationalVibration : MonoBehaviour
{
    public float amplitudeDegrees = 10f; // Adjust the vibration intensity (in degrees)
    public float frequency = 2.0f;       // Adjust the vibration speed

    private Quaternion originalRotation;
    private float timeElapsed = 0.0f;

    private void Start()
    {
        // Store the original rotation of the object
        originalRotation = transform.rotation;
    }

    private void Update()
    {
        // Calculate the new rotation based on a sine wave
        timeElapsed += Time.deltaTime;
        float angleOffset = amplitudeDegrees * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);

        // Apply the rotational vibration to the object
        Quaternion targetRotation = originalRotation * Quaternion.Euler(angleOffset, 0f, 0f);
        transform.rotation = targetRotation;
    }
}
