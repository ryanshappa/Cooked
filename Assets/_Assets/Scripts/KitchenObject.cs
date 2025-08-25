using UnityEngine;

public class KitchenObject : MonoBehaviour, IInteractable
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent parent;
    private Rigidbody rb;
    private Collider[] cols;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
    }

    // --- IInteractable (for your prompt/UI) ---
    public string GetInteractText() => $"Pick up {kitchenObjectSO.objectName}";
    public Transform GetTransform() => transform;

    public void Interact(Transform interactor)
    {
        // Only allow pickup if player has empty hands
        var carry = interactor.GetComponent<PlayerCarry>();
        if (carry == null || carry.HasKitchenObject()) return;

        SetParent(carry);
    }

    // --- Parent plumbing (works with player, counters, plates, etc.) ---
    public IKitchenObjectParent GetParent() => parent;
    public KitchenObjectSO GetKitchenObjectSO() => kitchenObjectSO;

    public void SetParent(IKitchenObjectParent newParent)
    {
        // unlink old
        if (parent != null) parent.ClearKitchenObject();

        parent = newParent;

        if (newParent != null)
        {
            // held: no physics collisions, snap to follow transform
            if (rb) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            if (cols != null) foreach (var c in cols) c.enabled = false;

            transform.SetParent(newParent.GetKitchenObjectFollowTransform(), worldPositionStays:false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            newParent.SetKitchenObject(this);
        }
        else
        {
            // released into world
            transform.SetParent(null);
            if (cols != null) foreach (var c in cols) c.enabled = true;
            if (rb) rb.isKinematic = false;
        }
    }

    public void DropWithPhysics(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        SetParent(null);
        if (rb)
        {
            rb.linearVelocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}
