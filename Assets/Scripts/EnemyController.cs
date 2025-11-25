using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [SerializeField] Room enemyRoom;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform targetPlayer;

    void Awake()
    {
    //    enemyRoom.enemyCount += 1;
    }

    private void Update()
    {
        if (Vector3.Distance(targetPlayer.position, transform.position) < 10)
        {
            agent.destination = targetPlayer.position;
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
