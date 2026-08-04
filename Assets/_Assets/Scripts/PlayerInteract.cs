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

            // Walk all hits front-to-back, piercing through EMPTY surfaces so
            // loose items resting inside slot collider volumes (a knife on the
            // table) stay reachable. Anything with its own interactable that
            // is NOT the surface itself (fridge doors, grab points) stops the
            // ray — no grabbing through closed doors.
            var hits = Physics.RaycastAll(cameraTransform.position, cameraTransform.forward,
                maxDistance, interactMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            IInteractable fallbackUse = null;
            foreach (var h in hits)
            {
                var ko = h.collider.GetComponentInParent<KitchenObject>();
                if (ko != null && ko.GetParent() is not PlayerCarry)
                {
                    targetKitchenObject = ko;
                    action = InteractAction.Pickup;
                    return;
                }

                var inter = h.collider.GetComponentInParent<IInteractable>();
                var surf = h.collider.GetComponentInParent<IKitchenObjectParent>();

                if (surf != null && !ReferenceEquals(surf, carry)
                    && (inter == null || ReferenceEquals(inter, surf)))
                {
                    if (surf.HasKitchenObject())
                    {
                        // occupied surface counts as aiming at its item
                        targetKitchenObject = surf.GetKitchenObject();
                        action = InteractAction.Pickup;
                        return;
                    }
                    if (fallbackUse == null) fallbackUse = inter;
                    continue;   // empty surface: pierce through
                }

                if (inter != null)
                {
                    targetInteractable = inter;
                    action = InteractAction.Use;
                    return;
                }

                break;   // plain world geometry blocks the ray
            }

            if (fallbackUse != null)
            {
                targetInteractable = fallbackUse;
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

    /// The object the highlight outline should wrap: pickup targets and
    /// dedicated interactables (doors, grab points) — not bare counters.
    public GameObject GetHighlightTarget()
    {
        if (action == InteractAction.Pickup && targetKitchenObject != null)
            return targetKitchenObject.gameObject;
        if (action == InteractAction.Use && targetInteractable != null
            && targetInteractable is not BaseCounter)
            return targetInteractable.GetTransform().gameObject;
        return null;
    }

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
