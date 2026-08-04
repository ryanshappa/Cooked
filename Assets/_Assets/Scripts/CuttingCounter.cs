using System.Collections.Generic;
using UnityEngine;

/// Cutting board: only accepts items it has a cutting recipe for. Chopping
/// requires a held knife (hover-tool) — each chop lands at an aimed position
/// along the ingredient, and cut spacing is measured for the future
/// PrepScore's `evenness` metric.
public class CuttingCounter : BaseCounter
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipes;

    private readonly List<float> cutPositions = new List<float>();   // normalized 0..1 along the cut axis
    private KitchenObject tracked;   // reset progress when the item changes

    public override bool CanAcceptKitchenObject(KitchenObject incoming) =>
        base.CanAcceptKitchenObject(incoming) && incoming != null && GetRecipe(incoming.GetKitchenObjectSO()) != null;

    /// True when there is an occupant the knife can work on.
    public bool HasChoppableOccupant()
    {
        var occupant = GetKitchenObject();
        return occupant != null && GetRecipe(occupant.GetKitchenObjectSO()) != null;
    }

    /// One knife chop at normalized position t (0..1 along the ingredient).
    public void ChopAt(float t)
    {
        var occupant = GetKitchenObject();
        if (occupant == null) return;
        var recipe = GetRecipe(occupant.GetKitchenObjectSO());
        if (recipe == null) return;

        if (occupant != tracked) { tracked = occupant; cutPositions.Clear(); }
        cutPositions.Add(Mathf.Clamp01(t));

        // placeholder feedback: squash a little more with each cut
        float n = cutPositions.Count;
        occupant.transform.localScale = new Vector3(1f + 0.05f * n, Mathf.Max(0.55f, 1f - 0.07f * n), 1f + 0.05f * n);

        if (cutPositions.Count >= recipe.cutsRequired)
        {
            float evenness = ComputeEvenness();
            Debug.Log($"[PrepScore preview] {occupant.name} chopped: evenness {evenness:F2} " +
                      $"({cutPositions.Count} cuts)");
            occupant.DestroySelf();
            KitchenObject.Spawn(recipe.output, this);
            tracked = null;
            cutPositions.Clear();
        }
    }

    /// 1 = perfectly even slice widths, 0 = terrible. Widths are the gaps
    /// between sorted cut positions including the two ends.
    private float ComputeEvenness()
    {
        var cuts = new List<float>(cutPositions);
        cuts.Sort();
        var widths = new List<float>();
        float prev = 0f;
        foreach (var c in cuts) { widths.Add(c - prev); prev = c; }
        widths.Add(1f - prev);

        float mean = 1f / widths.Count;
        float variance = 0f;
        foreach (var w in widths) variance += (w - mean) * (w - mean);
        variance /= widths.Count;

        // normalize: worst case (all cuts stacked at one end) has variance ~mean*(1-mean)
        float worst = mean * (1f - mean);
        return worst > 0f ? Mathf.Clamp01(1f - variance / worst) : 1f;
    }

    private CuttingRecipeSO GetRecipe(KitchenObjectSO input)
    {
        foreach (var r in cuttingRecipes)
            if (r.input == input) return r;
        return null;
    }
}
