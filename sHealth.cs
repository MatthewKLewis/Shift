using System;
using UnityEngine;

public class sHealth : MonoBehaviour
{
    public float maxHealth = 3f;
    public bool destroyOnDeath = true;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public event Action<float> OnDamaged; //remaining health
    public event Action OnDied;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void Damage(float amount)
    {
        if (IsDead) return;

        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(Current);

        if (IsDead)
        {
            OnDied?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}
