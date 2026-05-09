using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] DoorController doorToNextRoom;
    [SerializeField] FinishGame finishScript;

    public int enemyCount;

    private void Start()
    {
        finishScript.allEnemiesCount += enemyCount;
    }

    public void ReduceEnemyCount()
    {
        enemyCount -= 1;
        finishScript.ReduceEnemies();
        if (enemyCount <= 0)
        {
            StartCoroutine(doorToNextRoom.OpenDoorCorutine());
        }
    }
}