using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    [Header("Held physics follow")]
    [SerializeField] private float followStrength = 20f;   // how hard the item is pulled to the hold point
    [SerializeField] private float maxFollowSpeed = 15f;
    [SerializeField] private float rotateStrength = 15f;

    private IKitchenObjectParent parent;
    private Rigidbody rb;
    private Collider[] cols;
    private int originalLayer;
    private Vector3 localCenter;   // combined collider center in root-local space

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
        originalLayer = gameObject.layer;
        if (rb) rb.interpolation = RigidbodyInterpolation.Interpolate;

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

    void FixedUpdate()
    {
        // Physics-driven carry: the item stays a real collider-enabled body and is
        // steered to the hold point by velocity, so it can never pass through geometry.
        if (parent is PlayerCarry playerCarry && rb != null)
        {
            var follow = playerCarry.GetKitchenObjectFollowTransform();
            if (follow == null) return;

            Quaternion targetRot = follow.rotation;
            Vector3 targetPos = follow.position - targetRot * Vector3.Scale(localCenter, transform.lossyScale);

            rb.linearVelocity = Vector3.ClampMagnitude((targetPos - rb.position) * followStrength, maxFollowSpeed);

            Quaternion delta = targetRot * Quaternion.Inverse(rb.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            rb.angularVelocity = (angle != 0f && !float.IsInfinity(axis.x))
                ? axis.normalized * (angle * Mathf.Deg2Rad * rotateStrength)
                : Vector3.zero;
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
                // Held: stays fully physical (colliders ON, dynamic body), but moves to
                // the "Held" layer so the player's movement casts and the interaction
                // ray ignore it.
                SetLayerRecursive(LayerMask.NameToLayer("Held"));
                if (cols != null) foreach (var c in cols) c.enabled = true;
                if (rb)
                {
                    rb.isKinematic = false;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                transform.SetParent(null); // world space; FixedUpdate steers it
            }
            else
            {
                // Counters and static surfaces: snap to the anchor
                SetLayerRecursive(originalLayer);
                if (rb)
                {
                    rb.isKinematic = false; // Make dynamic FIRST
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false; // No gravity so it stays on the anchor
                    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
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
            SetLayerRecursive(originalLayer);
            transform.SetParent(null);
            if (cols != null) foreach (var c in cols) c.enabled = true;
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
        }
    }

    public void DropWithPhysics(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        if (parent != null && parent.GetKitchenObject() == this)
            parent.ClearKitchenObject();
        parent = null;

        SetLayerRecursive(originalLayer);
        transform.SetParent(null);
        if (cols != null) foreach (var c in cols) c.enabled = true;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.linearVelocity = linearVelocity;
            rb.angularVelocity = angularVelocity;
        }
    }

    private void SetLayerRecursive(int layer)
    {
        if (layer < 0) return;
        foreach (var t in GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;
    }
}
