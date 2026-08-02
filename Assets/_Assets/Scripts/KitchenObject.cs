using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent parent;
    private Rigidbody rb;
    private Collider[] cols;
    private Vector3 localCenter;   // combined collider center in root-local space

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();

        // Cache the collider-bounds center so a held item centers its BODY on the
        // hold point, not its pivot (pivots on these models are often offset).
        if (cols.Length > 0)
        {
            var b = cols[0].bounds;
            foreach (var c in cols) b.Encapsulate(c.bounds);
            localCenter = transform.InverseTransformPoint(b.center);
        }
    }

    public IKitchenObjectParent GetParent() => parent;
    public KitchenObjectSO GetKitchenObjectSO() => kitchenObjectSO;

    /// The only sanctioned way to create a kitchen object from data.
    /// Centralized so the Phase 4 network conversion has a single spawn seam.
    public static KitchenObject Spawn(KitchenObjectSO so, IKitchenObjectParent parent = null)
    {
        var instance = Instantiate(so.prefab);
        var ko = instance.GetComponent<KitchenObject>();
        if (parent != null) ko.SetParent(parent);
        return ko;
    }

    /// The only sanctioned way to destroy a kitchen object (unlinks its parent first).
    public void DestroySelf()
    {
        if (parent != null && parent.GetKitchenObject() == this)
            parent.ClearKitchenObject();
        Destroy(gameObject);
    }

    void Update()
    {
        // Held: glued to the hold point every frame (zero lag), centered on the reticle.
        if (parent is PlayerCarry playerCarry)
        {
            var follow = playerCarry.GetKitchenObjectFollowTransform();
            if (follow != null)
            {
                transform.rotation = follow.rotation;
                transform.position = follow.position - follow.rotation * Vector3.Scale(localCenter, transform.lossyScale);
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
            if (newParent is PlayerCarry)
            {
                // For player carrying, disable physics & collisions
                if (cols != null) foreach (var c in cols) c.enabled = false;
                if (rb)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity  = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.isKinematic = true;
                }
                transform.SetParent(null); // world space; Update() glues it to the hold point
            }
            else
            {
                // Counters and static surfaces: snap to the anchor as an immovable
                // display — kinematic so collisions (tossed items) can never shove
                // it off the counter. Colliders stay on so it's targetable.
                if (rb)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity  = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.isKinematic = true;
                }
                if (cols != null) foreach (var c in cols) c.enabled = true;

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
                rb.useGravity = true;
            }
        }
    }

    public void DropWithPhysics(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        if (parent != null && parent.GetKitchenObject() == this)
            parent.ClearKitchenObject();
        parent = null;

        transform.SetParent(null);
        if (cols != null) foreach (var c in cols) c.enabled = true;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.linearVelocity  = linearVelocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}
