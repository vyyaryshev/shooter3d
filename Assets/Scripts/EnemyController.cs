using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    private Animator anim;
    private NavMeshAgent agent;
    private bool isDead;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
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

        DisableBehaviour<MutantAI>();
        DisableBehaviourByName("RoboDroneAI");

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (anim != null)
            anim.Play("Death");

        Destroy(gameObject, 3f);
    }

    public bool IsDead()
    {
        return isDead;
    }

    private void DisableBehaviour<T>() where T : Behaviour
    {
        T behaviour = GetComponentInChildren<T>();
        if (behaviour != null)
            behaviour.enabled = false;
    }

    private void DisableBehaviourByName(string behaviourTypeName)
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i].GetType().Name == behaviourTypeName)
                behaviours[i].enabled = false;
        }
    }
}
