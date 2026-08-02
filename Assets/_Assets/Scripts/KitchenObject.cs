using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent parent;
    private Rigidbody rb;
    private Collider[] cols;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
    }

    // ----- Parent plumbing -----
    public IKitchenObjectParent GetParent() => parent;
    public KitchenObjectSO GetKitchenObjectSO() => kitchenObjectSO;

    void Update()
    {
        // Smooth positioning when held by player
        if (parent is PlayerCarry playerCarry)
        {
            var followTransform = playerCarry.GetKitchenObjectFollowTransform();
            if (followTransform != null)
            {
                // Directly set position and rotation for immediate response
                transform.position = followTransform.position;
                transform.rotation = followTransform.rotation;
            }
        }
    }

    public void SetParent(IKitchenObjectParent newParent)
    {
        if (parent == newParent) return;

        // unlink old
        if (parent != null && parent.GetKitchenObject() == this)
            parent.ClearKitchenObject();

        parent = newParent;

        if (newParent != null)
        {
            // For player carrying, disable physics & collisions
            if (newParent is PlayerCarry)
            {
                if (cols != null) foreach (var c in cols) c.enabled = false;
                if (rb)
                {
                    rb.linearVelocity        = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic     = true;  // Set kinematic AFTER clearing velocities
                }
                transform.SetParent(null); // Keep in world space for smooth updates
            }
            else
            {
                // For counters and static surfaces, make dynamic first, then set properties
                if (rb)
                {
                    rb.isKinematic = false; // Make dynamic FIRST
                    rb.linearVelocity = Vector3.zero; // Now we can safely set velocities
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false; // No gravity so it stays on counter
                }
                // Keep colliders enabled for interaction - this is crucial!
                if (cols != null) 
                {
                    foreach (var c in cols) c.enabled = true; // Ensure colliders are enabled
                }
                
                var follow = newParent.GetKitchenObjectFollowTransform();
                transform.SetParent(follow, worldPositionStays: false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            newParent.SetKitchenObject(this);
        }
        else
        {
            // released to world
            transform.SetParent(null);
            if (cols != null) foreach (var c in cols) c.enabled = true;
            if (rb) 
            {
                rb.isKinematic = false;
                rb.useGravity = true; // Re-enable gravity when released
            }
        }
    }

    public void DropWithPhysics(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        // detach + re-enable physics before setting velocities
        if (parent != null && parent.GetKitchenObject() == this)
            parent.ClearKitchenObject();
        parent = null;

        transform.SetParent(null);
        if (cols != null) foreach (var c in cols) c.enabled = true;

        if (rb)
        {
            rb.isKinematic     = false;              // <— important
            rb.useGravity      = true;               // Re-enable gravity when dropped
            rb.linearVelocity        = linearVelocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}
