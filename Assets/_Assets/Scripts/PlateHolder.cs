using UnityEngine;

/// Sits on a Plate kitchen object and makes it a holder itself: while the
/// plate is on a counter, food can be placed on top of it (the start of
/// dish assembly). One item for now; the multi-ingredient contents model
/// arrives with recipes/assembly (Phase 2/3).
public class PlateHolder : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private Transform plateTopPoint;

    private KitchenObject held;

    public Transform GetKitchenObjectFollowTransform() => plateTopPoint;
    public void SetKitchenObject(KitchenObject obj) => held = obj;
    public KitchenObject GetKitchenObject() => held;
    public void ClearKitchenObject() => held = null;
    public bool HasKitchenObject() => held != null;
    // Food only — never another plate.
    public bool CanAcceptKitchenObject(KitchenObject incoming) =>
        plateTopPoint != null && (incoming == null || incoming.GetComponent<PlateHolder>() == null);
}
