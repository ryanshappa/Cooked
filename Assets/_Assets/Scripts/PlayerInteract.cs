using UnityEngine;

/// Unified first-person interaction: one cast per frame decides what the
/// reticle is on, one contextual action runs on Interact. Replaces the old
/// PlayerInteract + PlayerPickupDrop pair.
public class PlayerInteract : MonoBehaviour
{
    public enum InteractAction { None, Pickup, Place, Use, Drop }

    [Header("Targeting")]
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private float assistRadius = 0.1f;   // spherecast fallback when the precise ray misses
    [SerializeField] private LayerMask interactMask;

    [Header("Refs")]
    [SerializeField] private GameInput input;
    [SerializeField] private Transform cameraTransform;   // falls back to Camera.main

    [Header("Drop")]
    [SerializeField] private float dropTossSpeed = 1.5f;

    private PlayerCarry carry;

    private InteractAction action;
    private KitchenObject targetKitchenObject;
    private IKitchenObjectParent targetSurface;
    private IInteractable targetInteractable;
    private IWorkStation targetWorkStation;

    void Awake()
    {
        carry = GetComponent<PlayerCarry>();
    }

    void Update()
    {
        if (!cameraTransform)
        {
            if (Camera.main) cameraTransform = Camera.main.transform;
            else return;
        }

        ResolveTarget();

        if (input.IsInteractPressed())
        {
            PerformAction();
            ResolveTarget(); // action changed world state; re-resolve so nothing reads a stale target this frame
        }

        if (input.IsAttackPressed() && targetWorkStation != null)
            targetWorkStation.Work(transform);
    }

    void ResolveTarget()
    {
        action = InteractAction.None;
        targetKitchenObject = null;
        targetSurface = null;
        targetInteractable = null;
        targetWorkStation = null;

        // Precise ray first so the prompt matches the reticle exactly;
        // small spherecast as a forgiveness fallback for thin objects.
        bool hasHit = Physics.Raycast(cameraTransform.position, cameraTransform.forward,
                          out RaycastHit hit, maxDistance, interactMask, QueryTriggerInteraction.Collide)
                   || Physics.SphereCast(cameraTransform.position, assistRadius, cameraTransform.forward,
                          out hit, maxDistance, interactMask, QueryTriggerInteraction.Collide);

        if (hasHit)
            targetWorkStation = hit.collider.GetComponentInParent<IWorkStation>();

        if (carry.HasKitchenObject())
        {
            if (hasHit)
            {
                var surface = hit.collider.GetComponentInParent<IKitchenObjectParent>();
                if (surface != null && !ReferenceEquals(surface, carry))
                {
                    var held = carry.GetKitchenObject();
                    if (!surface.HasKitchenObject() && surface.CanAcceptKitchenObject(held))
                    {
                        targetSurface = surface;
                        action = InteractAction.Place;
                    }
                    else if (surface.HasKitchenObject())
                    {
                        // Occupied — but the occupant may itself be a holder (a plate
                        // on a counter): place onto it.
                        var occupant = surface.GetKitchenObject();
                        var nested = occupant != null ? occupant.GetComponent<IKitchenObjectParent>() : null;
                        if (nested != null && !nested.HasKitchenObject() && nested.CanAcceptKitchenObject(held)
                            && occupant != held)
                        {
                            targetSurface = nested;
                            action = InteractAction.Place;
                        }
                    }
                    // Otherwise: no action — never toss into the counter face.
                    return;
                }
            }
            action = InteractAction.Drop;
        }
        else
        {
            if (!hasHit) return;

            var ko = hit.collider.GetComponentInParent<KitchenObject>();
            if (ko != null && ko.GetParent() is not PlayerCarry)
            {
                targetKitchenObject = ko;
                action = InteractAction.Pickup;
                return;
            }

            // Hitting an occupied surface counts as aiming at its item (slot
            // colliders can enclose the item and shadow it from the ray).
            var surface2 = hit.collider.GetComponentInParent<IKitchenObjectParent>();
            if (surface2 != null && !ReferenceEquals(surface2, carry) && surface2.HasKitchenObject())
            {
                targetKitchenObject = surface2.GetKitchenObject();
                action = InteractAction.Pickup;
                return;
            }

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                targetInteractable = interactable;
                action = InteractAction.Use;
            }
        }
    }

    void PerformAction()
    {
        switch (action)
        {
            case InteractAction.Pickup:
                targetKitchenObject.SetParent(carry);
                break;
            case InteractAction.Place:
                carry.GetKitchenObject().SetParent(targetSurface);
                break;
            case InteractAction.Use:
                targetInteractable.Interact(transform);
                break;
            case InteractAction.Drop:
                carry.GetKitchenObject().DropWithPhysics(cameraTransform.forward * dropTossSpeed, Vector3.zero);
                break;
        }
    }

    public InteractAction GetCurrentAction() => action;

    public bool HasPrompt(out string text)
    {
        switch (action)
        {
            case InteractAction.Pickup:
                text = $"Pick up {GetDisplayName(targetKitchenObject)}";
                return true;
            case InteractAction.Place:
                text = $"Place {GetDisplayName(carry.GetKitchenObject())}";
                return true;
            case InteractAction.Use:
                text = targetInteractable.GetInteractText();
                return true;
            case InteractAction.Drop:
                text = "Drop";
                return true;
            default:
                text = null;
                return false;
        }
    }

    private static string GetDisplayName(KitchenObject ko)
    {
        if (ko == null) return "item";
        var so = ko.GetKitchenObjectSO();
        return so != null && !string.IsNullOrEmpty(so.objectName) ? so.objectName : ko.name;
    }
}
