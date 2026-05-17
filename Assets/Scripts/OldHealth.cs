using UnityEngine;
using UnityEngine.UI;

public class OldHealth : MonoBehaviour
{
    [SerializeField] int health = 1000;
    [SerializeField] int maxHealth = 1000;
    [SerializeField] public Slider healthBar;

    public void Change(int x)
    {
        health += x;

        if (health > maxHealth) health = maxHealth;
        if (health < 0) health = 0;

        Debug.Log("Health: " + health);

        UpdateBar();
    }

    public int GetHealth() { return health; }
    public int GetMaxHealth() { return maxHealth; }

    void Start()
    {
        UpdateBar();
    }

    void Update()
    {
        UpdateBar();
    }

    void UpdateBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)health / maxHealth;
        }
    }
}