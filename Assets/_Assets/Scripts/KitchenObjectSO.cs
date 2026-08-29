using UnityEngine;

[CreateAssetMenu(fileName = "KitchenObjectSO", menuName = "Scriptable Objects/KitchenObjectSO")]
public class KitchenObjectSO : ScriptableObject
{
    public Transform prefab;
    public Sprite sprite;
    public string objectName;

    [Header("Knife")]
    [Tooltip("Assign a profile to make this food sliceable with the knife; leave empty for things that can't be cut (plates, tools, cooked steak…).")]
    public SliceProfileSO sliceProfile;

    public bool IsSliceable => sliceProfile != null;
}
