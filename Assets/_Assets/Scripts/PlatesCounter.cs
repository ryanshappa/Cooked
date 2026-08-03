using UnityEngine;

/// Plate dispenser station: the plate stack on top is an IngredientGrabPoint
/// that hands out Plate kitchen objects. Not a placement surface.
public class PlatesCounter : BaseCounter
{
    public override bool CanAcceptKitchenObject() => false;
}
