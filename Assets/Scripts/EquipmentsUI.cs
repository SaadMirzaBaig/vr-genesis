using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class EquipmentsUI : MonoBehaviour
{
   [Header("Assign in Inspector")]
    public GameObject popupPanel;   
    public Transform xrCamera;      
    [Header("Panel Placement")]
    public float distance = 1.2f;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"[EquipmentsUI] No XRSimpleInteractable found on {gameObject.name}");
            return;
        }

        interactable.selectEntered.RemoveListener(OnSelect);
        interactable.selectEntered.AddListener(OnSelect);
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelect);
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        Debug.Log($"[EquipmentsUI] SELECTED via XR: {gameObject.name}");
        Open();
    }

    public void Open()
    {
        if (popupPanel == null)
        {
            Debug.LogError("[EquipmentsUI] popupPanel is NOT assigned in Inspector.");
            return;
        }

        if (xrCamera == null)
        {
            Debug.LogError("[EquipmentsUI] xrCamera is NOT assigned in Inspector.");
            return;
        }

        popupPanel.SetActive(true);

        Vector3 fwd = xrCamera.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = xrCamera.forward;
        fwd.Normalize();

        popupPanel.transform.position = xrCamera.position + fwd * distance;
        popupPanel.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

}
