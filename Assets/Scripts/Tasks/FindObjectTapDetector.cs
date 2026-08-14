using System;
using UnityEngine;

/// <summary>
/// Detects the player's tap/click on a scene <see cref="Interactive"/> object during the
/// Find-Object "guessing" phase and raises <see cref="OnItemSelected"/> with the hit component.
///
/// Deliberately separate from <see cref="CursorHandler"/> — that one is wired to the card-game
/// flow (<c>Interactive.OnClick()</c> → <c>Action()</c> / <c>AddObjectToCard()</c>), which is the
/// wrong semantics for "did the player tap the correct kitchen item?".
///
/// Mobile/desktop input: raycasts from <c>Camera.main</c> through the tap/click screen point on
/// the same <c>"Clickable"</c> layer <see cref="Interactive"/> already assigns itself to.
///
/// XR note: for a future VR/XR build, keep the same event contract — replace
/// <see cref="TryGetPointerRay"/> with the XR ray interactor's ray (or subscribe this class to
/// an <c>XRBaseInteractable.selectEntered</c> event and forward the hit's <see cref="Interactive"/>
/// component through <see cref="OnItemSelected"/>). Callers of this component never need to change.
/// </summary>
public class FindObjectTapDetector : MonoBehaviour
{
    [Tooltip("Layer used by Interactive objects (set automatically in Interactive.Awake()).")]
    [SerializeField] private string clickableLayerName = "Clickable";

    [Tooltip("Max raycast distance.")]
    [SerializeField] private float maxDistance = 1000f;

    /// <summary>Fired with the tapped Interactive component when the player taps a valid scene object.</summary>
    public event Action<Interactive> OnItemSelected;

    /// <summary>Enable/disable tap detection without destroying the component.</summary>
    public bool Listening { get; set; } = true;

    private int _clickableMask;

    private void Awake()
    {
        _clickableMask = LayerMask.GetMask(clickableLayerName);
    }

    private void Update()
    {
        if (!Listening) return;
        if (!TryGetPointerRay(out Ray ray)) return;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, _clickableMask, QueryTriggerInteraction.Ignore))
        {
            Interactive interactive = hit.transform.GetComponentInParent<Interactive>();
            if (interactive != null)
                OnItemSelected?.Invoke(interactive);
        }
    }

    /// <summary>
    /// Returns true and outputs a world-space ray when the player performed a tap/click this frame.
    /// Mobile/desktop implementation uses mouse-down or touch-began from the main camera.
    /// Swap this method's body for an XR ray interactor's ray when porting to VR/XR.
    /// </summary>
    private bool TryGetPointerRay(out Ray ray)
    {
        ray = default;
        if (Camera.main == null) return false;

        Vector2 screenPos;
        bool tapped = false;

        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            tapped = true;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPos = Input.GetTouch(0).position;
            tapped = true;
        }
        else
        {
            screenPos = default;
        }

        if (!tapped) return false;

        ray = Camera.main.ScreenPointToRay(screenPos);
        return true;
    }
}
