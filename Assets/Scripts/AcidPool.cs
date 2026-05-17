using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AcidPool : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float tickInterval = 1f;

    private readonly List<Health> targets = new List<Health>();
    private float nextDamageTime;

    private void Reset()
    {
        Collider poolCollider = GetComponent<Collider>();

        if (poolCollider != null)
            poolCollider.isTrigger = true;
    }

    private void Update()
    {
        if (Time.time < nextDamageTime)
            return;

        nextDamageTime = Time.time + tickInterval;
        ApplyDamage();
    }

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null && !targets.Contains(health))
            targets.Add(health);
    }

    private void OnTriggerExit(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null)
            targets.Remove(health);
    }

    private void ApplyDamage()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Health health = targets[i];

            if (health == null || health.GetHealth() <= 0)
            {
                targets.RemoveAt(i);
                continue;
            }

            health.Change(-damagePerTick);
        }
    }
}
