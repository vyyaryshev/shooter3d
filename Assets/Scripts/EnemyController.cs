using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;
    private bool isDead;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (message.health <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " умер");

        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
        }

        MutantAI ai = GetComponent<MutantAI>();
        if (ai != null)
            ai.enabled = false;

        foreach (Collider col in GetComponents<Collider>())
            col.enabled = false;

        if (anim != null)
            anim.Play("Death");

        Destroy(gameObject, 3f);
    }

    public bool IsDead()
    {
        return isDead;
    }
}
