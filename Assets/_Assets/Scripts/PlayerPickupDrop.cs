using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickupDrop : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform  playerCameraTransform;
    [SerializeField] private LayerMask  interactMask;
    [SerializeField] private float      maxDistance = 2.2f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
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
            // pick up
            if (Physics.Raycast(playerCameraTransform.position, playerCameraTransform.forward,
                                out RaycastHit hit, maxDistance, interactMask))
            {
                // robust: grab component from parent in case you hit a child collider
                var ko = hit.collider ? hit.collider.GetComponentInParent<KitchenObject>() : null;
                if (ko != null && (ko.GetParent() == null || ko.GetParent() is not PlayerCarry))
                    ko.SetParent(carry);
            }
        }
        else
        {
            // place onto a holder if looking at one
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

            // else drop with a nudge
            held.DropWithPhysics(playerCameraTransform.forward * 1.5f, Vector3.zero);
        }
    }
}
