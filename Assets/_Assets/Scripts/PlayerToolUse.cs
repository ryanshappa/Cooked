using System.Collections;
using UnityEngine;

/// The hover-tool: while holding a knife and aiming at a cutting board with
/// a choppable item, the knife glides out to hover over the aim point with a
/// projected cut-guide line; LMB chops at that position (CookingSim-style —
/// your look direction is the cursor).
/// Runs in LateUpdate before PlayerCarry and the KitchenObject glue
/// (execution order) so the whole aim→hold→item chain updates in one frame —
/// out-of-order updates made the hovering knife jitter while strafing.
[DefaultExecutionOrder(-50)]
public class PlayerToolUse : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameInput input;
    [SerializeField] private Transform cameraTransform;   // falls back to Camera.main

    [Header("Targeting")]
    [SerializeField] private float maxDistance = 1.4f;   // hover only engages up close — leaning in IS the focus mode
    [SerializeField] private LayerMask interactMask;

    [Header("Hover pose")]
    [SerializeField] private float hoverHeight = 0.14f;
    [SerializeField] private float chopDipTime = 0.07f;
    [SerializeField] private float chopRaiseTime = 0.09f;

    private PlayerCarry carry;
    private LineRenderer guide;
    private bool hovering;
    private bool chopping;

    private Sliceable targetSliceable;
    private Vector3 cutPointWorld;    // a point on the cut plane (top of the item, under the guide)
    private Vector3 cutNormal;        // cut-plane normal (horizontal, perpendicular to the blade)
    private Quaternion hoverRot;

    void Awake()
    {
        carry = GetComponent<PlayerCarry>();

        var guideGo = new GameObject("CutGuide");
        guide = guideGo.AddComponent<LineRenderer>();
        guide.positionCount = 2;   // single red line across the item at the cut plane
        guide.startWidth = guide.endWidth = 0.008f;
        guide.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        guide.material.color = new Color(1f, 0.25f, 0.2f, 0.9f);
        guide.enabled = false;
    }

    void LateUpdate()
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

    /// Valid hover = holding a knife + aiming at a Sliceable resting on a cutting
    /// board (either the board's slotted occupant or a loose cut piece lying on
    /// it). Computes the cut plane from the aim point.
    private bool TryResolveHover()
    {
        var held = carry.GetKitchenObject();
        var tool = held ? held.GetComponent<Tool>() : null;
        if (tool == null || tool.Type != Tool.ToolType.Knife) return false;

        if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward,
                out RaycastHit hit, maxDistance, interactMask, QueryTriggerInteraction.Collide))
            return false;

        var sliceable = ResolveSliceable(hit.collider, hit.point);
        if (sliceable == null) return false;

        var rends = sliceable.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return false;
        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);

        // Real chopping posture (see Docs/Videos/Knife + dev's reference shot):
        // the knife points FORWARD (away from you), blade vertical, so the cut
        // plane contains your view direction. Looking left/right slides the
        // slice along the item; the guide line runs front-to-back.
        Vector3 axis = cameraTransform.right; axis.y = 0f; axis.Normalize();      // slide direction (aim left/right) = plane normal
        Vector3 lineDir = cameraTransform.forward; lineDir.y = 0f; lineDir.Normalize();  // knife + guide direction
        if (axis.sqrMagnitude < 0.001f || lineDir.sqrMagnitude < 0.001f) return false;

        Vector3 center = b.center;
        float halfLen = Mathf.Abs(Vector3.Dot(b.extents, axis));

        // Don't trust the raw collider hit for the cut position — intersect the
        // view ray with the horizontal plane at the ITEM'S TOP and clamp to the
        // item's bounds. Pitch sweeps the cut smoothly from the item's front
        // edge to its back edge and can never leave the item.
        float planeY = b.max.y;
        Vector3 camPos = cameraTransform.position;
        Vector3 camDir = cameraTransform.forward;
        Vector3 aimPoint;
        if (camDir.y < -0.0001f)
        {
            float tPlane = (planeY - camPos.y) / camDir.y;
            aimPoint = camPos + camDir * tPlane;
        }
        else
        {
            aimPoint = center + axis * halfLen;   // looking level/up: back edge
        }
        float along = Mathf.Clamp(Vector3.Dot(aimPoint - center, axis), -halfLen, halfLen);

        targetSliceable = sliceable;
        cutNormal = axis;
        cutPointWorld = center + axis * along + Vector3.up * (b.extents.y + 0.01f);

        // Blade lies along the cut line, blade down. Hover rotation is applied
        // verbatim by the hold point (no compensation needed — the glue follows
        // the hold point directly).
        hoverRot = Quaternion.LookRotation(lineDir, Vector3.up) * tool.HoverRotationOffset;

        // Guide: single red line matching the KNIFE's footprint (its blade
        // track), so line and blade visually agree instead of the line only
        // spanning the item's depth.
        var heldRend = held.GetComponentInChildren<Renderer>();
        float halfKnife = heldRend != null
            ? Mathf.Max(0.1f, Mathf.Abs(Vector3.Dot(heldRend.bounds.extents, lineDir)))
            : 0.18f;
        Vector3 pTop = center + axis * along; pTop.y = b.max.y + 0.006f;
        guide.SetPosition(0, pTop - lineDir * halfKnife);
        guide.SetPosition(1, pTop + lineDir * halfKnife);
        guide.enabled = true;
        return true;
    }

    /// The Sliceable the aim ray means: a board's slotted occupant, or a loose
    /// piece that is physically resting on a cutting board with a recipe for it.
    private Sliceable ResolveSliceable(Collider hitCollider, Vector3 hitPoint)
    {
        var direct = hitCollider.GetComponentInParent<Sliceable>();
        if (direct != null)
        {
            var ko = direct.GetComponent<KitchenObject>();
            if (ko.GetParent() is CuttingCounter slotted) return slotted.CanCut(ko) ? direct : null;
            if (ko.GetParent() != null) return null;   // on a plate / other counter / in a hand
            var board = FindBoardUnder(direct);
            return board != null && board.CanCut(ko) ? direct : null;
        }

        var counter = hitCollider.GetComponentInParent<CuttingCounter>();
        if (counter == null) return null;
        if (counter.HasChoppableOccupant()) return counter.GetKitchenObject().GetComponent<Sliceable>();

        // Aiming at the board right next to a loose piece (easy to do with round
        // things like a tomato, whose top edge overhangs empty board): be
        // forgiving and take the nearest cuttable piece within reach of the hit.
        return FindLoosePieceNear(counter, hitPoint, nearPieceRadius);
    }

    [SerializeField] private float nearPieceRadius = 0.05f;   // board-hit tolerance around loose pieces
    private static readonly Collider[] nearHits = new Collider[16];
    private static Sliceable FindLoosePieceNear(CuttingCounter board, Vector3 point, float radius)
    {
        int n = Physics.OverlapSphereNonAlloc(point, radius, nearHits, ~0, QueryTriggerInteraction.Ignore);
        Sliceable best = null; float bestD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var s = nearHits[i].GetComponentInParent<Sliceable>();
            if (s == null) continue;
            var ko = s.GetComponent<KitchenObject>();
            if (ko.GetParent() != null || !board.CanCut(ko)) continue;
            float d = (nearHits[i].ClosestPoint(point) - point).sqrMagnitude;
            if (d < bestD) { bestD = d; best = s; }
        }
        return best;
    }

    private static readonly RaycastHit[] downHits = new RaycastHit[8];
    private static CuttingCounter FindBoardUnder(Sliceable piece)
    {
        var rend = piece.GetComponentInChildren<Renderer>();
        Vector3 from = rend != null ? rend.bounds.center : piece.transform.position;
        int n = Physics.RaycastNonAlloc(from, Vector3.down, downHits, 0.6f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            if (downHits[i].collider.transform.IsChildOf(piece.transform)) continue;
            var c = downHits[i].collider.GetComponentInParent<CuttingCounter>();
            if (c != null) return c;
        }
        return null;
    }

    private void EndHover()
    {
        if (!hovering) return;
        hovering = false;
        guide.enabled = false;
        carry.ClearHoldOverride();
        targetSliceable = null;
    }

    private IEnumerator ChopRoutine()
    {
        chopping = true;
        var target = targetSliceable;
        Vector3 point = cutPointWorld, normal = cutNormal;
        Vector3 up = cutPointWorld + Vector3.up * hoverHeight;
        Vector3 down = cutPointWorld + Vector3.up * 0.015f;

        for (float e = 0f; e < chopDipTime; e += Time.deltaTime)
        {
            carry.SetHoldOverride(Vector3.Lerp(up, down, e / chopDipTime), hoverRot);
            yield return null;
        }
        // The cut fires at the bottom of the dip, on the plane the guide showed
        // (the promise to the player), not on the lagging knife transform.
        if (target != null) target.TrySlice(point, normal, out _, out _);
        for (float e = 0f; e < chopRaiseTime; e += Time.deltaTime)
        {
            carry.SetHoldOverride(Vector3.Lerp(down, up, e / chopRaiseTime), hoverRot);
            yield return null;
        }
        chopping = false;
    }
}
