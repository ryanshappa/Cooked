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

    // While set, the hold point glides to this pose instead of the camera
    // offset — used by the hover-tool (knife floating over the cut point).
    private bool hasHoldOverride;
    private Vector3 overridePos;
    private Quaternion overrideRot;
    [SerializeField] private float overrideGlideSpeed = 14f;

    public void SetHoldOverride(Vector3 pos, Quaternion rot)
    {
        hasHoldOverride = true;
        overridePos = pos;
        overrideRot = rot;
    }

    public void ClearHoldOverride() => hasHoldOverride = false;

    void Update()
    {
        if (dynamicHoldPoint == null || cameraTransform == null) return;

        if (hasHoldOverride)
        {
            float k = overrideGlideSpeed * Time.deltaTime;
            dynamicHoldPoint.position = Vector3.Lerp(dynamicHoldPoint.position, overridePos, k);
            dynamicHoldPoint.rotation = Quaternion.Slerp(dynamicHoldPoint.rotation, overrideRot, k);
        }
        else
        {
            // Zero-lag carry (the shipped feel — don't smooth this).
            dynamicHoldPoint.position = cameraTransform.position + cameraTransform.TransformDirection(holdOffset);

            // Tools stay LEVEL (yaw-only): a knife keeps its blade down and
            // horizontal no matter how far you pitch the camera. Everything
            // else glues to the full camera rotation as before.
            var heldTool = held != null ? held.GetComponent<Tool>() : null;
            if (heldTool != null)
                dynamicHoldPoint.rotation =
                    Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * heldTool.HeldRotationOffset;
            else
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
