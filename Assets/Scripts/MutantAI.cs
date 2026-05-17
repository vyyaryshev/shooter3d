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
    private EnemyController enemyController;

    [Header("Attack Settings")]
    public int damage = 20;
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        enemyController = GetComponent<EnemyController>();

        if (IsAgentReady())
            GoToNextPoint();
    }

    void Update()
    {
        if (enemyController != null && enemyController.IsDead()) return;
        if (!IsAgentReady()) return;

        if (player == null)
        {
            Patrol();
            return;
        }

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

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPoint();
        }

        agent.isStopped = false;

        if (anim != null)
            anim.Play("Run");
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPoint].position);
        currentPoint = (currentPoint + 1) % patrolPoints.Length;
    }

    void Chase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);

        if (anim != null)
            anim.Play("Run");
    }

    void Attack()
    {
        agent.isStopped = true;
        transform.LookAt(player);

        if (Time.time >= nextAttackTime)
        {
            if (anim != null)
                anim.SetTrigger("Attack");

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private bool IsAgentReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
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
