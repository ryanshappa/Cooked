using UnityEngine;

/// One cooking transition: input sits on a hot surface for `seconds` and
/// becomes output. Chains express progressions (Meat -> CookedMeat, then
/// CookedMeat -> BurnedMeat via a second recipe).
[CreateAssetMenu(fileName = "CookingRecipeSO", menuName = "Scriptable Objects/CookingRecipeSO")]
public class CookingRecipeSO : ScriptableObject
{
    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float seconds = 8f;
}
