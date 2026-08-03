using UnityEngine;

public class PlayerCarry : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private Transform cameraTransform; // Assign your camera transform
    [SerializeField] private Vector3 holdOffset = new Vector3(0, -0.2f, 0.5f); // Offset relative to camera

    private KitchenObject held;
    private Transform dynamicHoldPoint;

    void Awake()
    {
        // Create a dynamic hold point that follows the camera
        GameObject holdPointObj = new GameObject("DynamicHoldPoint");
        dynamicHoldPoint = holdPointObj.transform;
    }

    void Update()
    {
        // Update hold point position every frame for immediate response
        if (dynamicHoldPoint != null && cameraTransform != null)
        {
            dynamicHoldPoint.position = cameraTransform.position + cameraTransform.TransformDirection(holdOffset);
            dynamicHoldPoint.rotation = cameraTransform.rotation;
        }
    }

    public Transform GetKitchenObjectFollowTransform() => dynamicHoldPoint;
    public bool CanAcceptKitchenObject(KitchenObject incoming) => true;
    public void SetKitchenObject(KitchenObject obj) => held = obj;
    public KitchenObject GetKitchenObject() => held;
    public void ClearKitchenObject() => held = null;
    public bool HasKitchenObject() => held != null;
}
