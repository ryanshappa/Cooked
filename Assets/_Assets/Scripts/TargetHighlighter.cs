using System.Collections.Generic;
using UnityEngine;

/// Draws a blue outline shell around whatever the interaction system is
/// currently targeting for Pickup or Use (Cooking Simulator-style). The
/// outline follows the target, so it disappears the moment the item is
/// picked up (it stops being a target).
public class TargetHighlighter : MonoBehaviour
{
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private Material outlineMaterial;

    private GameObject current;
    private readonly List<GameObject> shells = new List<GameObject>();

    void LateUpdate()
    {
        var target = playerInteract != null ? playerInteract.GetHighlightTarget() : null;
        if (target == current) return;

        ClearShells();
        current = target;
        if (current != null) BuildShells(current);
    }

    private void BuildShells(GameObject target)
    {
        foreach (var mr in target.GetComponentsInChildren<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !mr.enabled) continue;

            var shell = new GameObject("OutlineShell");
            shell.transform.SetParent(mr.transform, false);
            shell.layer = mr.gameObject.layer;
            shell.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var smr = shell.AddComponent<MeshRenderer>();
            smr.sharedMaterial = outlineMaterial;
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.receiveShadows = false;
            shells.Add(shell);
        }
    }

    private void ClearShells()
    {
        foreach (var s in shells)
            if (s != null) Destroy(s);
        shells.Clear();
    }
}
