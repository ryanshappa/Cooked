using EzySlice;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Step-1 verification for Docs/MeshSlicing.md: slices the selected ingredient's visual mesh
/// with a hard-coded vertical plane and drops the two hulls into the scene next to it.
/// Editor-only smoke test; the real runtime path lands in step 2 (MeshSlicingService/Sliceable).
/// </summary>
public static class SliceTestMenu
{
    private const string MenuRoot = "Yes Chef/Slicing/";

    [MenuItem(MenuRoot + "Slice Selected (vertical, through centre)")]
    private static void SliceSelectedCentre() => SliceSelected(0.5f);

    [MenuItem(MenuRoot + "Slice Selected (vertical, 20% from left)")]
    private static void SliceSelectedLeft() => SliceSelected(0.2f);

    [MenuItem(MenuRoot + "Report Read-Write Status of Ingredient Meshes")]
    private static void ReportReadable()
    {
        string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/_Assets/Meshes", "Assets/Pandazole_Ultimate_Pack/Pandazole Kitchen Food/Models" });
        int readable = 0;
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Mesh m)
                {
                    if (m.isReadable) readable++;
                    if (path.Contains("Cheese") || path.Contains("Tomato") || path.Contains("Steak"))
                        Debug.Log($"[SliceTest] {path} :: {m.name} readable={m.isReadable} tris={m.triangles.Length / 3}");
                }
        }
        Debug.Log($"[SliceTest] readable meshes: {readable}/{guids.Length}");
    }

    private static void SliceSelected(float t)
    {
        GameObject sel = Selection.activeGameObject;
        if (sel == null) { Debug.LogWarning("[SliceTest] Select an ingredient in the scene first."); return; }

        MeshFilter mf = sel.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) { Debug.LogWarning("[SliceTest] No MeshFilter under selection."); return; }
        if (!mf.sharedMesh.isReadable) { Debug.LogError($"[SliceTest] {mf.sharedMesh.name} is not Read/Write enabled."); return; }

        Renderer rend = mf.GetComponent<Renderer>();
        Bounds b = rend.bounds;
        // Vertical plane, normal along world X, positioned t (0..1) across the item's X extent.
        Vector3 normal = Vector3.right;
        Vector3 origin = new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), b.center.y, b.center.z);

        Material cap = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SliceCap_Test", color = new Color(1f, 0.85f, 0.3f) };

        SlicedHull hull = mf.gameObject.Slice(origin, normal, cap);
        if (hull == null) { Debug.LogError("[SliceTest] Slice returned null (plane missed the mesh, or mesh unreadable)."); return; }

        Transform parent = mf.transform.parent;
        GameObject upper = hull.CreateUpperHull(mf.gameObject, cap);
        GameObject lower = hull.CreateLowerHull(mf.gameObject, cap);
        foreach (var (go, sign) in new[] { (upper, 1f), (lower, -1f) })
        {
            go.name = $"{sel.name}_{(sign > 0 ? "upper" : "lower")}";
            go.transform.SetParent(parent, false);
            go.transform.position += normal * sign * 0.03f; // pull apart so the caps are visible
            Undo.RegisterCreatedObjectUndo(go, "Slice Test");
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            Debug.Log($"[SliceTest] {go.name}: verts={m.vertexCount} tris={m.triangles.Length / 3} submeshes={m.subMeshCount}");
        }
        Undo.RecordObject(rend, "Slice Test");
        rend.enabled = false; // keep the original around for undo; hide it
        Selection.activeGameObject = upper;
        Debug.Log($"[SliceTest] Sliced {sel.name} at t={t:F2} (origin {origin}, normal {normal}). Undo to restore.");
    }
}
