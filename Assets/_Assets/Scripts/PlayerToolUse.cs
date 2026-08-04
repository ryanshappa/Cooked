using System.Collections;
using UnityEngine;

/// The hover-tool: while holding a knife and aiming at a cutting board with
/// a choppable item, the knife glides out to hover over the aim point with a
/// projected cut-guide line; LMB chops at that position (CookingSim-style —
/// your look direction is the cursor).
public class PlayerToolUse : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameInput input;
    [SerializeField] private Transform cameraTransform;   // falls back to Camera.main

    [Header("Targeting")]
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private LayerMask interactMask;

    [Header("Hover pose")]
    [SerializeField] private float hoverHeight = 0.14f;
    [SerializeField] private float chopDipTime = 0.07f;
    [SerializeField] private float chopRaiseTime = 0.09f;

    private PlayerCarry carry;
    private LineRenderer guide;
    private bool hovering;
    private bool chopping;

    private CuttingCounter targetCounter;
    private float cutT;
    private Vector3 cutPointWorld;
    private Quaternion hoverRot;

    void Awake()
    {
        carry = GetComponent<PlayerCarry>();

        var guideGo = new GameObject("CutGuide");
        guide = guideGo.AddComponent<LineRenderer>();
        guide.positionCount = 2;
        guide.startWidth = guide.endWidth = 0.008f;
        guide.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        guide.material.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        guide.enabled = false;
    }

    void Update()
    {
        if (!cameraTransform)
        {
            if (Camera.main) cameraTransform = Camera.main.transform;
            else return;
        }

        if (chopping) return;   // pose is driven by the chop coroutine

        if (!TryResolveHover())
        {
            EndHover();
            return;
        }

        hovering = true;
        carry.SetHoldOverride(cutPointWorld + Vector3.up * hoverHeight, hoverRot);

        if (input.IsAttackPressed())
            StartCoroutine(ChopRoutine());
    }

    /// Valid hover = holding a knife + aiming at a cutting board with a
    /// choppable occupant. Computes the cut position from the aim point.
    private bool TryResolveHover()
    {
        var held = carry.GetKitchenObject();
        var tool = held ? held.GetComponent<Tool>() : null;
        if (tool == null || tool.Type != Tool.ToolType.Knife) return false;

        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward,
                out RaycastHit hit, maxDistance, interactMask, QueryTriggerInteraction.Collide))
            return false;

        var counter = hit.collider.GetComponentInParent<CuttingCounter>();
        if (counter == null || !counter.HasChoppableOccupant()) return false;

        var occupant = counter.GetKitchenObject();
        var rends = occupant.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return false;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        // View-relative cutting: the cut plane slides along your flattened look
        // direction (look up/down = nearer/farther cut), and the cut line runs
        // left-right across your view. No mode switch — leaning in IS the focus.
        Vector3 axis = cameraTransform.forward; axis.y = 0f; axis.Normalize();
        Vector3 lineDir = cameraTransform.right; lineDir.y = 0f; lineDir.Normalize();
        if (axis.sqrMagnitude < 0.001f || lineDir.sqrMagnitude < 0.001f) return false;

        Vector3 center = b.center;
        float halfLen = Mathf.Abs(Vector3.Dot(b.extents, axis));
        float along = Mathf.Clamp(Vector3.Dot(hit.point - center, axis), -halfLen, halfLen);

        targetCounter = counter;
        cutT = halfLen > 0.0001f ? (along + halfLen) / (2f * halfLen) : 0.5f;
        cutPointWorld = center + axis * along + Vector3.up * (b.extents.y + 0.01f);

        // Blade lies along the cut line, blade down. The carry glue multiplies
        // in the tool's HELD offset; cancel it so the hover pose is exact.
        var toolComp = held.GetComponent<Tool>();
        hoverRot = Quaternion.LookRotation(lineDir, Vector3.up)
                   * toolComp.HoverRotationOffset
                   * Quaternion.Inverse(toolComp.HeldRotationOffset);

        // Guide line across the ingredient at the cut plane
        float halfDepth = Mathf.Abs(Vector3.Dot(b.extents, lineDir)) + 0.02f;
        Vector3 p = center + axis * along + Vector3.up * (b.extents.y + 0.005f);
        guide.SetPosition(0, p - lineDir * halfDepth);
        guide.SetPosition(1, p + lineDir * halfDepth);
        guide.enabled = true;
        return true;
    }

    private void EndHover()
    {
        if (!hovering) return;
        hovering = false;
        guide.enabled = false;
        carry.ClearHoldOverride();
        targetCounter = null;
    }

    private IEnumerator ChopRoutine()
    {
        chopping = true;
        var counter = targetCounter;
        float t = cutT;
        Vector3 up = cutPointWorld + Vector3.up * hoverHeight;
        Vector3 down = cutPointWorld + Vector3.up * 0.015f;

        for (float e = 0f; e < chopDipTime; e += Time.deltaTime)
        {
            carry.SetHoldOverride(Vector3.Lerp(up, down, e / chopDipTime), hoverRot);
            yield return null;
        }
        counter.ChopAt(t);
        for (float e = 0f; e < chopRaiseTime; e += Time.deltaTime)
        {
            carry.SetHoldOverride(Vector3.Lerp(down, up, e / chopRaiseTime), hoverRot);
            yield return null;
        }
        chopping = false;
    }
}
