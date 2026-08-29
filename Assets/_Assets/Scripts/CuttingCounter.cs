using UnityEngine;

/// Cutting board: only accepts items it has a cutting recipe for. Chopping
/// requires a held knife (hover-tool, PlayerToolUse): each chop slices the
/// ingredient's mesh for real on the aimed plane (Sliceable) and the pieces
/// rest loose on the board. Docs/MeshSlicing.md.
public class CuttingCounter : BaseCounter
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipes;

    public override bool CanAcceptKitchenObject(KitchenObject incoming) =>
        base.CanAcceptKitchenObject(incoming) && incoming != null && GetRecipe(incoming.GetKitchenObjectSO()) != null;

    /// True when this board has a cutting recipe for the object (whole or a cut piece of it).
    public bool CanCut(KitchenObject ko) => ko != null && GetRecipe(ko.GetKitchenObjectSO()) != null;

    /// True when the slotted occupant is something the knife can work on.
    public bool HasChoppableOccupant() => CanCut(GetKitchenObject()) && GetKitchenObject().GetComponent<Sliceable>() != null;

    /// Recipe-rule evaluation over the piece lineage (retag pieces as the
    /// output SO once cut enough) lands in step 4 — see Docs/MeshSlicing.md.

    private CuttingRecipeSO GetRecipe(KitchenObjectSO input)
    {
        foreach (var r in cuttingRecipes)
            if (r.input == input) return r;
        return null;
    }
}
