using UnityEngine;
using UnityEngine.AI;

public class OldEnemyController : MonoBehaviour
{
    [SerializeField] Room enemyRoom;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform targetPlayer;

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

    //    enemyRoom.enemyCount += 1;
    }

    private void Update()
    {
        if (agent == null || targetPlayer == null || !agent.enabled || !agent.isOnNavMesh)
            return;

       // if (Vector3.Distance(targetPlayer.position, transform.position) < 10)
        {
            agent.SetDestination(targetPlayer.position);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        print(collision.gameObject.tag);
        if (collision.gameObject.tag == "Bullet")
        {
            print("1");
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
    //    enemyRoom.ReduceEnemyCount();
    }
}
