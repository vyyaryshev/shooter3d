using UnityEngine;

public class HealthScript : MonoBehaviour
{
    [SerializeField] private int health;

    public void TakeDamage(int damage)
    {
        health -= damage;
        print("Damaged! Health = " + health);
    }
}