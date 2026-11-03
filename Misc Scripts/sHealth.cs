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

    [Space(4)]
    [Header("Audio")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;  //played from a temporary object, so it survives Destroy
    [SerializeField] private AudioClip healSound;
    [SerializeField] private bool spatialSound = true; //turn off for the player (heard "inside the robot")

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
            sUtil.PlaySoundAt(deathSound, transform.position, spatial: spatialSound);
            OnDied?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
        else
        {
            sUtil.PlaySoundAt(hurtSound, transform.position, spatial: spatialSound);
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || IsFull) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        OnHealthChanged?.Invoke(Current);
        sUtil.PlaySoundAt(healSound, transform.position, spatial: spatialSound);
    }
}
