using System;
using UnityEngine;

public class sHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public bool destroyOnDeath = true;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;

    public event Action<int> OnDamaged; //remaining health
    public event Action OnDied;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void Damage(int amount)
    {
        if (IsDead) return;

        Current = Mathf.Max(0, Current - amount);
        OnDamaged?.Invoke(Current);

        if (IsDead)
        {
            OnDied?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}
