using UnityEngine;

/// Marks a kitchen object as a usable tool. Tools are ordinary carryable
/// items; holding one changes what LMB does (hover-tool work).
public class Tool : MonoBehaviour
{
    public enum ToolType { Knife }

    [SerializeField] private ToolType type = ToolType.Knife;

    // Authoring escape hatch: extra rotation applied in the hover pose so the
    // model's blade points down/along the cut line regardless of mesh axes.
    [SerializeField] private Vector3 hoverRotationEuler;

    public ToolType Type => type;
    public Quaternion HoverRotationOffset => Quaternion.Euler(hoverRotationEuler);
}
