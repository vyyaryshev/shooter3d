using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AcidPool : MonoBehaviour
{
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float tickInterval = 1f;

    private readonly List<OldHealth> playerTargets = new List<OldHealth>();
    private readonly List<EnemyHealth> enemyTargets = new List<EnemyHealth>();
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
        OldHealth health = other.GetComponentInParent<OldHealth>();
        if (health != null && !playerTargets.Contains(health))
            playerTargets.Add(health);

        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null && !enemyTargets.Contains(enemyHealth))
            enemyTargets.Add(enemyHealth);
    }

    private void OnTriggerExit(Collider other)
    {
        OldHealth health = other.GetComponentInParent<OldHealth>();
        if (health != null)
            playerTargets.Remove(health);

        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
            enemyTargets.Remove(enemyHealth);
    }

    private void ApplyDamage()
    {
        for (int i = playerTargets.Count - 1; i >= 0; i--)
        {
            OldHealth health = playerTargets[i];

            if (health == null)
            {
                playerTargets.RemoveAt(i);
                continue;
            }

            health.Change(-damagePerTick);
        }

        for (int i = enemyTargets.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemyHealth = enemyTargets[i];

            if (enemyHealth == null || enemyHealth.IsDead())
            {
                enemyTargets.RemoveAt(i);
                continue;
            }

            enemyHealth.TakeDamage(damagePerTick);
        }
    }
}
