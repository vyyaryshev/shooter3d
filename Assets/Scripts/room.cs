using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] DoorController doorToNextRoom;
    public int enemyCount;
    public void ReduceEnemyCount()
    {
        enemyCount -= 1;
        if (enemyCount <= 0)
        {
            StartCoroutine(doorToNextRoom.OpenDoorCorutine());
        }
    }
}


