using UnityEngine;

/// Cutting board: only accepts items it has a cutting recipe for (that IS
/// the vegetables-only rule — no recipe, no placement). LMB chops; after
/// enough cuts the item is replaced by the recipe output.
/// Bar version — the physics chopping minigame replaces the LMB mash in Phase 2.
public class CuttingCounter : BaseCounter, IWorkStation
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipes;

    private int cutsDone;
    private KitchenObject tracked;   // reset progress when the item changes

    public override bool CanAcceptKitchenObject(KitchenObject incoming) =>
        base.CanAcceptKitchenObject(incoming) && incoming != null && GetRecipe(incoming.GetKitchenObjectSO()) != null;

    public void Work(Transform worker)
    {
        var occupant = GetKitchenObject();
        if (occupant == null) return;

        var recipe = GetRecipe(occupant.GetKitchenObjectSO());
        if (recipe == null) return;

        if (occupant != tracked) { tracked = occupant; cutsDone = 0; }
        cutsDone++;

        // placeholder feedback: squash a little more with each cut
        var visual = occupant.transform;
        visual.localScale = new Vector3(1f + 0.06f * cutsDone, Mathf.Max(0.5f, 1f - 0.08f * cutsDone), 1f + 0.06f * cutsDone);

        if (cutsDone >= recipe.cutsRequired)
        {
            occupant.DestroySelf();
            KitchenObject.Spawn(recipe.output, this);
            tracked = null;
            cutsDone = 0;
        }
    }

    private CuttingRecipeSO GetRecipe(KitchenObjectSO input)
    {
        foreach (var r in cuttingRecipes)
            if (r.input == input) return r;
        return null;
    }
}
