using UnityEngine;

/// Delivery window: accepts ONLY plates (the inverse of PlateHolder's
/// no-plates rule). A short beat after a plate is set down it is
/// "delivered" (despawned). Phase 3 adds order validation + payment here.
public class DeliveryCounter : BaseCounter
{
    [SerializeField] private float deliverDelay = 0.6f;

    private float timer;

    public override bool CanAcceptKitchenObject(KitchenObject incoming) =>
        base.CanAcceptKitchenObject(incoming) && incoming != null && incoming.GetComponent<PlateHolder>() != null;

    void Update()
    {
        var plate = GetKitchenObject();
        if (plate == null) { timer = 0f; return; }

        timer += Time.deltaTime;
        if (timer >= deliverDelay)
        {
            plate.DestroySelf();   // takes any food on the plate with it
            timer = 0f;
        }
    }
}
