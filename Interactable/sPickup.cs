using UnityEngine;

//Repair kit: drive over it in overhead mode to restore health.
[RequireComponent(typeof(Collider))]

public class sPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private float bobHeight = 0.15f;

    private Vector3 basePosition;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        basePosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * bobHeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<sPlayer>();
        if (player == null || player.IsDead) return;

        var health = player.GetComponent<sHealth>();
        if (health.IsFull) return; //leave it for later

        health.Heal(healAmount);
        Actions.OnConsoleLog?.Invoke($"repair kit applied. hull integrity {health.Current}/{health.maxHealth}");
        Destroy(gameObject);
    }
}
