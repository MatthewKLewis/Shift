using System;
using UnityEngine;

public class sHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public bool destroyOnDeath = true;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsFull => Current >= maxHealth;

    public event Action<int> OnHealthChanged; //remaining health, after damage or healing
    public event Action OnDied;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void Damage(int amount)
    {
        if (IsDead) return;

        Current = Mathf.Max(0, Current - amount);
        OnHealthChanged?.Invoke(Current);

        if (IsDead)
        {
            OnDied?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || IsFull) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        OnHealthChanged?.Invoke(Current);
    }
}
