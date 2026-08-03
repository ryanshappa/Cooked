using UnityEngine;

/// A station the player can perform work on with the Attack input (LMB) —
/// chopping on the cutting board today, the physics minigames tomorrow.
public interface IWorkStation
{
    void Work(Transform worker);
}
