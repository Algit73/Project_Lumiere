using UnityEngine;
// using UnityEngine.XR.ARFoundation;

public class RotateToCamera : MonoBehaviour
{
    private Transform cameraTransform;
    // [SerializeField] private ARCameraManager arCameraManager;
    // AndroidJavaClass logClass;

    private void Awake()
    {
        // arCameraManager = FindObjectOfType<ARCameraManager>();
        // arCameraManager = FindObjectOfType<ARCameraManager>();
        // logClass = new AndroidJavaClass("android.util.Log");
    }

    private void Start()
    {
        // Assuming your camera is tagged as "MainCamera"
        cameraTransform = Camera.main.transform;
        // cameraTransform = arCameraManager.transform;
    }

    private void Update()
    {
        // Calculate the direction from object to camera
        // Vector3 toCamera = arCameraManager.transform.position - transform.position;
        // logClass.CallStatic<int>("d", "RotateToCamera", $"transform.position: {arCameraManager.transform.position}");
        // logClass.CallStatic<int>("d", "RotateToCamera", $"toCamera: {toCamera}");
        Vector3 toCamera = cameraTransform.position - transform.position;

        // Calculate the rotation angle (in degrees) around the Y-axis
        float rotationAngle = Mathf.Atan2(toCamera.x, toCamera.z) * Mathf.Rad2Deg + 180;
        // logClass.CallStatic<int>("d", "RotateToCamera", $"rotationAngle: {rotationAngle}");

        // Apply the rotation
        transform.rotation = Quaternion.Euler(0f, rotationAngle, 0f);
        // int rotationSpeed = 1;
        // transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
