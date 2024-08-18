using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomCamera : MonoBehaviour
{
    private float scorll_speed = 10f;

    private Camera zoom_camera;
    // Start is called before the first frame update
    void Start()
    {
        zoom_camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (zoom_camera.orthographic)
            zoom_camera.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * scorll_speed;
        
        else
            zoom_camera.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * scorll_speed;
    }
}
