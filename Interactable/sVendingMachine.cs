using UnityEngine;

//Console: "dispense vend_1" (or just "dispense" for the nearest one) drops a repair kit
//in front of the machine. You still have to shift to overhead mode and drive over it.
public class sVendingMachine : sDevice
{
    [SerializeField] private sPickup pickupPrefab;   //leave empty for a placeholder cube
    [SerializeField] private Transform dispensePoint; //leave empty for 1.5m in front
    [SerializeField] private int stock = 3;

    public override string Kind => "VENDOR";
    public override string Status => stock > 0 ? $"stock {stock}" : "SOLD OUT";
    public override string[] Verbs => new[] { "dispense" };

    public override string Execute(string verb)
    {
        if (verb != "dispense") return null;
        if (stock <= 0) return $"{id}: SOLD OUT";

        stock--;
        Vector3 position = dispensePoint != null
            ? dispensePoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;

        if (pickupPrefab != null) Instantiate(pickupPrefab, position, Quaternion.identity);
        else SpawnPlaceholder(position);

        return $"{id}: repair kit dispensed ({stock} left)";
    }

    void SpawnPlaceholder(Vector3 position)
    {
        var kit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kit.name = "RepairKit";
        kit.transform.position = position;
        kit.transform.localScale = Vector3.one * 0.5f;
        kit.GetComponent<Collider>().isTrigger = true;
        kit.GetComponent<Renderer>().material.color = new Color(0.3f, 0.95f, 0.45f);
        kit.AddComponent<sPickup>();
    }
}
