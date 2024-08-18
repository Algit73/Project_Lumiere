using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class UserMovement : MonoBehaviour
{
    [SerializeField] private float speed = 2.0f;
    [SerializeField] private float sensitivity = 2.0f;


    private void Update()
    {
        // Mouse look (camera rotation)
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // // Rotate the player horizontally (around the Y-axis)
        // transform.Rotate(Vector3.up, mouseX);

        // // Rotate the camera vertically (around the X-axis)
        // Camera.main.transform.Rotate(Vector3.left, mouseY);

        // Get the camera's forward and right vectors
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Ignore vertical component (optional)
        cameraForward.y = 0;
        cameraRight.y = 0;

        // Normalize vectors to ensure consistent speed
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Keyboard movement (WASD) relative to camera
        if (Input.GetKey(KeyCode.UpArrow))
            transform.position += cameraForward * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.position -= cameraRight * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))
            transform.position -= cameraForward * speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.RightArrow))
            transform.position += cameraRight * speed * Time.deltaTime;

    }
}



