using UnityEngine;
using UnityEngine.AI;

public class MutantAI : MonoBehaviour
{
    public Transform player;

    public float viewDistance = 15f;
    public float attackDistance = 2f;

    public Transform[] patrolPoints;
    private int currentPoint;

    private NavMeshAgent agent;
    private Animator anim;
    private EnemyHealth health;

    [Header("Attack Settings")]
    public int damage = 20;
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        GoToNextPoint();
    }

    void Update()
    {
        if (health == null || health.IsDead()) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackDistance)
        {
            Attack();
        }
        else if (distance <= viewDistance)
        {
            Chase();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        if (agent.remainingDistance < 0.5f)
        {
            GoToNextPoint();
        }

        agent.isStopped = false;
        anim.Play("Run");
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.destination = patrolPoints[currentPoint].position;
        currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    void Chase()
    {
        agent.isStopped = false;
        agent.destination = player.position;

        anim.Play("Run");
    }

    void Attack()
    {
        agent.isStopped = true;
        transform.LookAt(player);

        if (Time.time >= nextAttackTime)
        {
            anim.SetTrigger("Attack");
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // 🔥 ВЫЗЫВАЕТСЯ ИЗ ANIMATION EVENT
    public void DealDamage()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackDistance + 0.5f)
        {
            Health playerHealth = player.GetComponent<Health>();

            if (playerHealth != null)
            {
                playerHealth.Change(-damage); // ❗ урон = отрицательное значение
            }
        }
    }
}