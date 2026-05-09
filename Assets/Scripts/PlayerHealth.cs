using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        // Проверяем каждый кадр
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        Debug.Log("HP: " + currentHP);
    }

    void Die()
    {
        Debug.Log("Игрок умер");

        // Удаляем объект игрока
        Destroy(gameObject);







        void Die()
        {
            Debug.Log("Die() вызван!");
            Destroy(gameObject);
        }














    }
}


