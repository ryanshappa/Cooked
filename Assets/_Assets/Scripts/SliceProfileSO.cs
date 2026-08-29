using UnityEngine;

/// How a food behaves under the knife. Assigning one of these to a
/// KitchenObjectSO's `sliceProfile` is what makes that food sliceable —
/// no per-prefab components needed (KitchenObject adds the runtime
/// `Sliceable` itself). Docs/MeshSlicing.md.
[CreateAssetMenu(fileName = "SliceProfile_", menuName = "Scriptable Objects/SliceProfileSO")]
public class SliceProfileSO : ScriptableObject
{
    [Header("Look")]
    [Tooltip("Material for freshly cut faces (flesh/interior). Falls back to the food's outer material if null.")]
    public Material interiorMaterial;

    [Header("Rules")]
    [Tooltip("Used to turn sliced volume into grams (cheese ≈ 1.1, tomato ≈ 1.0, beef ≈ 1.05).")]
    public float densityGramsPerCm3 = 1f;
    [Tooltip("A cut that would leave either side lighter than this is refused (Cooking Simulator's min-mass gate).")]
    public float minPieceGrams = 2f;

    [Header("Separation feel")]
    [Tooltip("Gap opened between the two pieces along the cut, in metres (≈ blade thickness).")]
    public float cutGap = 0.004f;
    [Tooltip("Sideways speed the blade gives the TOP edge of the smaller piece (m/s). Thin pieces pivot on their base and flop over.")]
    public float sliceTopKickSpeed = 1.0f;
    [Tooltip("Sideways speed for chunky pieces that can't tip — they just scoot (m/s).")]
    public float chunkScootSpeed = 0.12f;
    [Tooltip("Seconds the two new pieces ignore each other's colliders so PhysX doesn't punt them apart.")]
    public float siblingIgnoreSeconds = 0.25f;
}
