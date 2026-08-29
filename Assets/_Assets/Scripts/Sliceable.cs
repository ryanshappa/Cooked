using System;
using UnityEngine;

/// Runtime behaviour for a knife-sliceable food. Added automatically by
/// KitchenObject when its KitchenObjectSO has a SliceProfileSO (the data-side
/// feature flag) — never hand-placed on prefabs. Every tunable comes from the
/// profile; this component only carries per-piece runtime state (lineage).
/// On a cut, the visual mesh is split on the blade plane into two new
/// KitchenObjects (KitchenObject.SpawnSlice) that inherit the SO, and so the
/// profile, and can be cut again. Docs/MeshSlicing.md.
[RequireComponent(typeof(KitchenObject))]
public class Sliceable : MonoBehaviour
{
    /// How many cuts separate this piece from the original whole ingredient.
    public int Generation { get; private set; }
    /// Fraction of the original ingredient's volume this piece holds (1 for a whole one).
    public float VolumeFraction { get; private set; } = 1f;
    /// Shared across every piece cut from the same original ingredient.
    public Guid RootId { get; private set; } = Guid.Empty;

    public float Grams => WholeGrams() * VolumeFraction;

    public SliceProfileSO Profile
    {
        get
        {
            var so = GetComponent<KitchenObject>().GetKitchenObjectSO();
            return so != null ? so.sliceProfile : null;
        }
    }

    /// Raised on the piece being cut, just before it is destroyed. (a, b) are the new pieces.
    public event Action<Sliceable, Sliceable> OnSliced;

    private float wholeGramsCache = -1f;

    public MeshFilter GetVisualMeshFilter() => GetComponentInChildren<MeshFilter>();

    /// Attempts a cut. Returns false (and changes nothing) when there is no
    /// profile, the plane misses, the mesh is unreadable, or either side would
    /// be below the profile's min-mass gate.
    public bool TrySlice(Vector3 worldPoint, Vector3 worldNormal, out Sliceable a, out Sliceable b)
    {
        a = b = null;
        var profile = Profile;
        if (profile == null) return false;
        var mf = GetVisualMeshFilter();
        if (mf == null) return false;

        var rend = mf.GetComponent<Renderer>();
        Material cap = profile.interiorMaterial != null ? profile.interiorMaterial : (rend != null ? rend.sharedMaterial : null);

        if (!MeshSlicingService.Slice(mf.gameObject, worldPoint, worldNormal, cap, out Mesh meshA, out Mesh meshB))
            return false;

        Vector3 scale = mf.transform.lossyScale;
        float volA = MeshSlicingService.WorldVolumeCm3(meshA, scale);
        float volB = MeshSlicingService.WorldVolumeCm3(meshB, scale);
        float total = volA + volB;
        if (total <= 0f) { Destroy(meshA); Destroy(meshB); return false; }

        float gramsA = volA * profile.densityGramsPerCm3, gramsB = volB * profile.densityGramsPerCm3;
        if (gramsA < profile.minPieceGrams || gramsB < profile.minPieceGrams)
        {
            Debug.Log($"[Sliceable] cut refused on {name}: pieces {gramsA:F1}g / {gramsB:F1}g, min {profile.minPieceGrams}g");
            Destroy(meshA); Destroy(meshB);
            return false;
        }

        var self = GetComponent<KitchenObject>();
        Guid root = RootId == Guid.Empty ? Guid.NewGuid() : RootId;
        float whole = WholeGrams();

        a = KitchenObject.SpawnSlice(self, mf, meshA, cap);
        b = KitchenObject.SpawnSlice(self, mf, meshB, cap);
        a.InitLineage(root, Generation + 1, VolumeFraction * volA / total, whole);
        b.InitLineage(root, Generation + 1, VolumeFraction * volB / total, whole);
        Debug.Log($"[Sliceable] {name} → {a.Grams:F0}g + {b.Grams:F0}g (gen {Generation + 1})");

        // Open a blade-width gap and ignore sibling collisions for a moment so
        // the two overlapping convex hulls don't get punted by depenetration.
        a.transform.position += worldNormal * (profile.cutGap * 0.5f);
        b.transform.position -= worldNormal * (profile.cutGap * 0.5f);
        a.StartCoroutine(IgnoreSiblingCollisions(a, b, profile.siblingIgnoreSeconds));

        // The slice falls away from the block. Model the blade shoving the TOP
        // of the smaller piece sideways while its base stays on the board: the
        // piece rotates about its bottom edge, which is what actually makes a
        // thin slice flop over (a spin about the centre of mass can't beat
        // gravity's restoring torque). Chunky pieces just scoot.
        Sliceable small = volA <= volB ? a : b;
        Vector3 away = (small == a ? worldNormal : -worldNormal).normalized;
        var smallRb = small.GetComponent<Rigidbody>();
        if (smallRb != null)
        {
            var sb = small.GetComponentInChildren<Renderer>().bounds;
            float thickness = Mathf.Abs(Vector3.Dot(sb.size, away));
            float height = Mathf.Max(0.01f, sb.size.y);
            float thinness = Mathf.Clamp01(1f - thickness / height);   // 1 = paper-thin, 0 = as thick as it is tall
            smallRb.maxAngularVelocity = 60f;
            if (thinness > 0.4f)
            {
                float vTop = profile.sliceTopKickSpeed * Mathf.Lerp(0.75f, 1f, thinness);   // measured: a 2 cm slice needs ≈ 0.7 m/s at the top to flop
                Vector3 tipAxis = Vector3.Cross(Vector3.up, away);
                smallRb.linearVelocity  = away * (vTop * 0.5f);         // COM moves at half the top speed …
                smallRb.angularVelocity = tipAxis * (vTop / height);    // … so the base is (nearly) at rest: pivot on the edge (sign verified empirically — Unity is left-handed)
            }
            else
            {
                smallRb.linearVelocity = away * profile.chunkScootSpeed;
            }
        }

        OnSliced?.Invoke(a, b);
        self.DestroySelf();
        return true;
    }

    public void InitLineage(Guid rootId, int generation, float volumeFraction, float wholeGrams)
    {
        RootId = rootId;
        Generation = generation;
        VolumeFraction = volumeFraction;
        wholeGramsCache = wholeGrams;
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.mass = Mathf.Max(0.01f, Grams / 1000f);
    }

    private float WholeGrams()
    {
        if (wholeGramsCache < 0f)
        {
            var mf = GetVisualMeshFilter();
            var profile = Profile;
            wholeGramsCache = mf != null && profile != null && mf.sharedMesh != null && mf.sharedMesh.isReadable
                ? MeshSlicingService.WorldVolumeCm3(mf.sharedMesh, mf.transform.lossyScale) * profile.densityGramsPerCm3
                : 0f;
        }
        return wholeGramsCache;
    }

    private static System.Collections.IEnumerator IgnoreSiblingCollisions(Sliceable a, Sliceable b, float seconds)
    {
        var ca = a.GetComponentsInChildren<Collider>();
        var cb = b.GetComponentsInChildren<Collider>();
        foreach (var x in ca) foreach (var y in cb) Physics.IgnoreCollision(x, y, true);
        yield return new WaitForSeconds(seconds);
        if (a == null || b == null) yield break;
        foreach (var x in ca) foreach (var y in cb) if (x && y) Physics.IgnoreCollision(x, y, false);
    }
}
