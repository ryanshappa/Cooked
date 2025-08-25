using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupDrop : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private LayerMask interactMask;        // Interactable layer (objects + counters)
    [SerializeField] private float maxDistance = 2.2f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions; // your InputSystem_Actions
    private InputAction interactAction;

    private PlayerCarry carry;

    void Awake()
    {
        carry = GetComponent<PlayerCarry>();

        var map = inputActions.FindActionMap("Player", true);
        interactAction = map.FindAction("Interact", true);
    }

    void OnEnable()  => interactAction.Enable();
    void OnDisable() => interactAction.Disable();

    void Update()
    {
        if (!interactAction.WasPressedThisFrame()) return;

        if (!carry.HasKitchenObject())
        {
            // Try pick up a KitchenObject
            if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward,
                                out RaycastHit hit, maxDistance, interactMask))
            {
                if (hit.collider && hit.collider.TryGetComponent(out KitchenObject ko))
                {
                    // Optional: only if it's not already parented somewhere
                    if (ko.GetParent() == null) ko.SetParent(carry);
                }
            }
        }
        else
        {
            // Try place on a holder surface
            var held = carry.GetKitchenObject();

            if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward,
                                out RaycastHit hit, maxDistance, interactMask))
            {
                if (hit.collider && hit.collider.TryGetComponent(out IKitchenObjectParent surface)
                    && !surface.HasKitchenObject())
                {
                    held.SetParent(surface);
                    return;
                }
            }

            // Otherwise drop with a little forward toss
            held.DropWithPhysics(playerCameraTransform.forward * 1.5f, Vector3.zero);
            carry.ClearKitchenObject();
        }
    }
}
