using UnityEngine;

public class XZVibration : MonoBehaviour
{
    public float amplitude = 0.1f; // Adjust the vibration intensity
    public float frequency = 2.0f; // Adjust the vibration speed

    private Vector3 originalPosition;
    private float timeElapsed = 0.0f;

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void Update()
    {
        // Calculate the new position based on a sine wave
        timeElapsed += Time.deltaTime;
        float xOffset = amplitude * Mathf.Sin(2 * Mathf.PI * frequency * timeElapsed);
        float zOffset = amplitude * Mathf.Cos(2 * Mathf.PI * frequency * timeElapsed);

        // Apply the vibration to the object
        transform.position = originalPosition + new Vector3(xOffset, 0f, zOffset);
    }
}
