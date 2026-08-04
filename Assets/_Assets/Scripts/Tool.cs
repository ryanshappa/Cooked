using UnityEngine;

/// Marks a kitchen object as a usable tool. Tools are ordinary carryable
/// items; holding one changes what LMB does (hover-tool work).
public class Tool : MonoBehaviour
{
    public enum ToolType { Knife }

    [SerializeField] private ToolType type = ToolType.Knife;

    // Authoring escape hatches for mesh-axis differences:
    // hover pose (blade down, along the cut line — we only ever slice
    // vertically) and held pose (angled across the FP view so the thin
    // blade is actually visible, not edge-on).
    [SerializeField] private Vector3 hoverRotationEuler;
    [SerializeField] private Vector3 heldRotationEuler;

    public ToolType Type => type;
    public Quaternion HoverRotationOffset => Quaternion.Euler(hoverRotationEuler);
    public Quaternion HeldRotationOffset => Quaternion.Euler(heldRotationEuler);
}
