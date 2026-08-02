using UnityEngine;

/// Fridge body: a non-placeable surface. The doors are FridgeDoor children
/// (each toggles independently); shelf stock is IngredientGrabPoint props.
/// Clicking the body itself does nothing.
public class FridgeCounter : BaseCounter
{
    public override bool CanAcceptKitchenObject() => false;
}
