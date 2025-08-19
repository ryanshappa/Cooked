using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private LayerMask interactMask;

    [Header("Input")]
    [SerializeField] private GameInput input;

    Transform cam;              // <- actual rendering camera
    IInteractable current;

    void Awake()
    {
        // safe fallback; we'll also refresh at Start in case Camera.main wasn't ready
        cam = Camera.main ? Camera.main.transform : null;
    }

    void Start()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!cam) Debug.LogWarning("PlayerInteract: No Main Camera found. Raycasts will fail.");
    }

    void Update()
    {
        if (!cam) { cam = Camera.main ? Camera.main.transform : null; return; }

        current = FindInteractable(cam);

        // Debug ray
        Debug.DrawRay(cam.position, cam.forward * maxDistance,
                      current != null ? Color.green : Color.red);

        if (current != null && input.Interact != null && input.Interact.WasPressedThisFrame())
        {
            Debug.Log($"Interact pressed on: {current.GetTransform().name}");
            current.Interact(transform);
        }
    }

    public IInteractable GetInteractableObject() => current;

    IInteractable FindInteractable(Transform cameraTf)
    {
        var origin = cameraTf.position;
        var dir    = cameraTf.forward;

        // spherecast is forgiving for thin edges
        if (Physics.SphereCast(origin, 0.12f, dir, out var hit, maxDistance,
                               interactMask, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<IInteractable>();

        if (Physics.Raycast(origin, dir, out hit, maxDistance,
                            interactMask, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<IInteractable>();

        return null;
    }
}
