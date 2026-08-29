using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent parent;
    private Rigidbody rb;
    private Collider[] cols;
    private Tool tool;
    private Vector3 localCenter;   // combined collider center in root-local space

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        cols = GetComponentsInChildren<Collider>();
        tool = GetComponent<Tool>();

        // Data-driven feature flag: a KitchenObjectSO with a SliceProfile is
        // knife-sliceable. The runtime Sliceable is added here so prefabs
        // never need hand-wiring (see Docs/MeshSlicing.md).
        if (kitchenObjectSO != null && kitchenObjectSO.IsSliceable && GetComponent<Sliceable>() == null)
            gameObject.AddComponent<Sliceable>();

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

    /// The only sanctioned way to create a cut piece of an ingredient. The piece
    /// is a fresh root at the source visual's world pose (so the sliced mesh,
    /// which is in that visual's local space, lands exactly where the parent
    /// was), with the mesh as both its visual and its convex collider. It is
    /// spawned loose (no IKitchenObjectParent) — pieces rest on the board by
    /// physics. See Docs/MeshSlicing.md.
    public static Sliceable SpawnSlice(KitchenObject source, MeshFilter sourceVisual, Mesh mesh, Material capMaterial)
    {
        var go = new GameObject($"{source.name}_piece");
        go.layer = source.gameObject.layer;
        go.transform.SetPositionAndRotation(sourceVisual.transform.position, sourceVisual.transform.rotation);
        go.transform.localScale = sourceVisual.transform.lossyScale;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var srcRend = sourceVisual.GetComponent<Renderer>();
        var mats = new System.Collections.Generic.List<Material>(srcRend != null ? srcRend.sharedMaterials : new Material[0]);
        // EzySlice appends the cap as a new submesh unless capMaterial already
        // exists in the material list (then it merges into that submesh).
        if (mesh.subMeshCount > mats.Count) mats.Add(capMaterial);
        mr.sharedMaterials = mats.ToArray();
        mr.shadowCastingMode = srcRend != null ? srcRend.shadowCastingMode : UnityEngine.Rendering.ShadowCastingMode.On;

        var mc = go.AddComponent<MeshCollider>();
        mc.convex = true;
        mc.sharedMesh = mesh;

        var srcRb = source.GetComponent<Rigidbody>();
        var rb = go.AddComponent<Rigidbody>();
        rb.interpolation = srcRb ? srcRb.interpolation : RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = srcRb ? srcRb.collisionDetectionMode : CollisionDetectionMode.Discrete;

        // KitchenObject first so its SO (and therefore the slice profile) is
        // set before the Sliceable reads it.
        var ko = go.AddComponent<KitchenObject>();
        ko.kitchenObjectSO = source.kitchenObjectSO;
        var sliceable = go.GetComponent<Sliceable>();
        if (sliceable == null) sliceable = go.AddComponent<Sliceable>();
        return sliceable;
    }

    /// The only sanctioned way to destroy a kitchen object (unlinks its parent first).
    public void DestroySelf()
    {
        if (parent != null && parent.GetKitchenObject() == this)
            parent.ClearKitchenObject();
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        // Held: glued to the hold point every frame (zero lag), centered on the
        // reticle. All rotation policy (tool poses, hover override) lives in
        // PlayerCarry's hold point — the glue just follows it. LateUpdate,
        // after PlayerToolUse/PlayerCarry (execution order), so the chain is
        // jitter-free.
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
                // For player carrying, disable physics & collisions.
                // Live query, not the cached array: a plate may carry stacked
                // items whose colliders must switch off/on with it.
                foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
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
                foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;

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
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
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
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.linearVelocity  = linearVelocity;
            rb.angularVelocity = angularVelocity;
        }
    }
}
