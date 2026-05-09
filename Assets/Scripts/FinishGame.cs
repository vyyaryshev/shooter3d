using UnityEngine;

public class FinishGame : MonoBehaviour
{
    public int allEnemiesCount;
    public void ReduceEnemies()
    {
        allEnemiesCount -= 1;
        if (allEnemiesCount <= 0)
        {
            Finish();
        }
    }
    private void Finish() { }
}