using EzySlice;
using UnityEngine;

/// Pure geometry: split a mesh by a world-space plane. Wraps EzySlice so the
/// backend can be swapped (OpenFracture / Mesh Slicer) without touching game
/// code. Deterministic for a given (mesh, plane) — Phase 4 replicates the
/// plane, not the vertices. See Docs/MeshSlicing.md.
public static class MeshSlicingService
{
    /// Slices the mesh on `visual` (its MeshFilter, in its own transform) by the
    /// plane through `worldPoint` with `worldNormal`. `a` is the side the
    /// normal points to, `b` the other side. Returns false if the plane misses.
    public static bool Slice(GameObject visual, Vector3 worldPoint, Vector3 worldNormal, Material capMaterial,
                             out Mesh a, out Mesh b)
    {
        a = b = null;
        var mf = visual.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;
        if (!mf.sharedMesh.isReadable)
        {
            Debug.LogError($"[MeshSlicing] {mf.sharedMesh.name} is not Read/Write enabled.", visual);
            return false;
        }

        SlicedHull hull = visual.Slice(worldPoint, worldNormal, capMaterial);
        if (hull == null || hull.upperHull == null || hull.lowerHull == null) return false;

        a = hull.upperHull;
        b = hull.lowerHull;
        a.name = mf.sharedMesh.name + "_a";
        b.name = mf.sharedMesh.name + "_b";
        a.RecalculateBounds();
        b.RecalculateBounds();
        return true;
    }

    /// Signed-tetrahedra volume in the mesh's local units (closed meshes only).
    public static float LocalVolume(Mesh mesh)
    {
        var v = mesh.vertices;
        var t = mesh.triangles;
        double vol = 0;
        for (int i = 0; i < t.Length; i += 3)
        {
            Vector3 p1 = v[t[i]], p2 = v[t[i + 1]], p3 = v[t[i + 2]];
            vol += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6.0;
        }
        return Mathf.Abs((float)vol);
    }

    /// Volume in cubic centimetres for a mesh rendered with the given world scale.
    public static float WorldVolumeCm3(Mesh mesh, Vector3 lossyScale)
    {
        float m3 = LocalVolume(mesh) * Mathf.Abs(lossyScale.x * lossyScale.y * lossyScale.z);
        return m3 * 1_000_000f;
    }
}
