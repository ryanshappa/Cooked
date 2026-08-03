using UnityEngine;

/// Stove: only accepts items it has a cooking recipe for (meat, not
/// vegetables or plates). Cooking is automatic over time; leaving food on
/// too long chains into the next recipe (Cooked -> Burned).
/// Bar version — pan physics/flipping arrives with the Phase 2 minigames.
public class StoveCounter : BaseCounter
{
    [SerializeField] private CookingRecipeSO[] cookingRecipes;

    private KitchenObject tracked;
    private CookingRecipeSO activeRecipe;
    private float timer;

    public override bool CanAcceptKitchenObject(KitchenObject incoming) =>
        base.CanAcceptKitchenObject(incoming) && incoming != null && GetRecipe(incoming.GetKitchenObjectSO()) != null;

    void Update()
    {
        var occupant = GetKitchenObject();
        if (occupant == null)
        {
            tracked = null;
            activeRecipe = null;
            return;
        }

        if (occupant != tracked)
        {
            tracked = occupant;
            activeRecipe = GetRecipe(occupant.GetKitchenObjectSO());
            timer = 0f;
        }

        if (activeRecipe == null) return;

        timer += Time.deltaTime;
        if (timer >= activeRecipe.seconds)
        {
            occupant.DestroySelf();
            KitchenObject.Spawn(activeRecipe.output, this);
            // next frame re-tracks the new occupant; a chained recipe
            // (Cooked -> Burned) starts automatically if one exists
        }
    }

    public float GetCookProgress() =>
        activeRecipe != null ? Mathf.Clamp01(timer / activeRecipe.seconds) : 0f;

    private CookingRecipeSO GetRecipe(KitchenObjectSO input)
    {
        foreach (var r in cookingRecipes)
            if (r.input == input) return r;
        return null;
    }
}
