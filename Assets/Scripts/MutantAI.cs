using UnityEngine;
using UnityEngine.AI;

public class MutantAI : MonoBehaviour
{
    public string playerTag = "Player";
    public Transform player;

    public float viewDistance = 15f;
    public float attackDistance = 2f;
    public float navMeshSampleRadius = 3f;

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
        ResolvePlayer();

        if (EnsureAgentOnNavMesh() && HasPatrolPoints())
            GoToNextPoint();
    }

    public void SetPlayer(Transform target)
    {
        player = target;
    }

    void Update()
    {
        if (enemyController != null && enemyController.IsDead()) return;
        if (!EnsureAgentOnNavMesh()) return;

        ResolvePlayer();

        if (player == null)
        {
            Patrol();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (!HasPatrolPoints())
        {
            if (distance <= attackDistance)
                Attack();
            else
                Chase();

            return;
        }

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
        if (!HasPatrolPoints()) return;

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
        if (!HasPatrolPoints()) return;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform point = patrolPoints[currentPoint];
            currentPoint = (currentPoint + 1) % patrolPoints.Length;

            if (point == null)
                continue;

            agent.SetDestination(point.position);
            return;
        }
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

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, navMeshSampleRadius, agent.areaMask))
        {
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    private bool HasPatrolPoints()
    {
        if (patrolPoints == null)
            return false;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] != null)
                return true;
        }

        return false;
    }

    private void ResolvePlayer()
    {
        if (player != null || string.IsNullOrWhiteSpace(playerTag))
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.transform;
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
