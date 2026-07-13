using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class PlacementBlock : MonoBehaviour
{
    public event Action<Vector3, Quaternion> OnReleased;

    private XRGrabInteractable grabInteractable;
    private bool interactionEnabled;
    private bool hasSubmittedThisTrial;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectExited.AddListener(HandleSelectExited);
    }

    private void OnDisable()
    {
        grabInteractable.selectExited.RemoveListener(HandleSelectExited);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        interactionEnabled = isEnabled;
        grabInteractable.enabled = isEnabled;
        if (isEnabled) hasSubmittedThisTrial = false;
    }

    private void HandleSelectExited(SelectExitEventArgs args)
    {
        if (!interactionEnabled || hasSubmittedThisTrial) return;

        hasSubmittedThisTrial = true;
        OnReleased?.Invoke(transform.position, transform.rotation);
    }
}
