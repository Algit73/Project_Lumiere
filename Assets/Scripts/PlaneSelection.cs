using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

//
// This script allows us to create anchors with
// a prefab attached in order to visbly discern where the anchors are created.
// Anchors are a particular point in space that you are asking your device to track.
//

[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class PlaneSelection : MonoBehaviour
{
    // This is the prefab that will appear every time an anchor is created.
    [SerializeField]
    // GameObject m_AnchorPrefab;
    AndroidJavaClass logClass;

    private bool is_plane_selected = true;

    // public GameObject AnchorPrefab
    // {
    //     get => m_AnchorPrefab;
    //     set => m_AnchorPrefab = value;
    // }

    // Removes all the anchors that have been created.
    // public void RemoveAllAnchors()
    // {
    //     foreach (var anchor in m_AnchorPoints)
    //     {
    //         Destroy(anchor);
    //     }
    //     m_AnchorPoints.Clear();
    // }

    // On Awake(), we obtains a reference to all the required components.
    // The ARRaycastManager allows us to perform raycasts so that we know where to place an anchor.
    // The ARPlaneManager detects surfaces we can place our objects on.
    // The ARAnchorManager handles the processing of all anchors and updates their position and rotation.
    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        // m_AnchorManager = GetComponent<ARAnchorManager>();
        m_PlaneManager = GetComponent<ARPlaneManager>();
        // m_AnchorPoints = new List<ARAnchor>();
        logClass = new AndroidJavaClass("android.util.Log");
    }

    void Update()
    {
        if (!is_plane_selected)
        {
            // If there is no tap, then simply do nothing until the next call to Update().
            // if (Input.touchCount == 0)
            //     return;

            // var touch = Input.GetTouch(0);
            // if (touch.phase != TouchPhase.Began)
            //     return;

            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {

                TouchControl touch = Touchscreen.current.touches[0];
                logClass.CallStatic<int>("d", "AnchorCreator", $"Screen Touched");

                if (touch.press.isPressed)
                {
                    Vector2 touchPosition = touch.position.ReadValue();
                    if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))
                    // if (m_RaycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
                    {
                        is_plane_selected = true;
                        logClass.CallStatic<int>("d", "AnchorCreator", $"m_RaycastManager hit");
                        // Raycast hits are sorted by distance, so the first one
                        // will be the closest hit.
                        var hitPose = s_Hits[0].pose;
                        var hitTrackableId = s_Hits[0].trackableId;
                        var hitPlane = m_PlaneManager.GetPlane(hitTrackableId);
                        logClass.CallStatic<int>("d", "PlaneSelection", $"hitPose: {hitPose}");
                        logClass.CallStatic<int>("d", "PlaneSelection", $"hitTrackableId: {hitTrackableId}");
                        logClass.CallStatic<int>("d", "PlaneSelection", $"hitPlane: {hitPlane}");

                        
                        Vector3 convertedVector = new Vector3(hitPose.position.x, hitPose.position.y, hitPose.position.z);

                        CommandsCenter.Manager.SetEnvPos(convertedVector);


                    }
                }
            }
        }
    }

    public void ready_to_select()
    {
        is_plane_selected = false;
        logClass.CallStatic<int>("d", "PlaneSelection", $"ready_to_select");
    }

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    // List<ARAnchor> m_AnchorPoints;

    ARRaycastManager m_RaycastManager;

    // ARAnchorManager m_AnchorManager;

    ARPlaneManager m_PlaneManager;
}
