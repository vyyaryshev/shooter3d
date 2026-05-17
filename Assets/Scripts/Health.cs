using UnityEngine;

public class HealthChangedMessage
{
    public readonly double health;
    public readonly double maxHealth;
    public readonly double oldHealth;
    public readonly double oldMaxHealth;
    public readonly double healthChange;

    public HealthChangedMessage(double health, double maxHealth, double oldHealth, double oldMaxHealth)
    {
        this.health = health;
        this.maxHealth = maxHealth;
        this.oldHealth = oldHealth;
        this.oldMaxHealth = oldMaxHealth;
        healthChange = health - oldHealth;
    }
}

public class Health : MonoBehaviour
{
    [SerializeField] double health = 1000.0;
    [SerializeField] double maxHealth = 1000;

    public double GetHealth()
    {
        return health;
    }

    public double GetMaxHealth()
    {
        return maxHealth;
    }

    private void Start()
    {
        this.Change(0);
    }

    public void Change(double healthChange, bool increment = true, double maxHealthChange = 0)
    {
        var oldHealth = health;
        var oldMaxHealth = maxHealth;

        if (maxHealthChange != 0) {
            if (increment)
            {
                maxHealth += maxHealthChange;
            }
            else
            {
                maxHealth = maxHealthChange;
            }
            if (maxHealth < 1.0) maxHealth = 1.0;
        }

        if (increment)
        {
            health += healthChange;
        } else {
            health = healthChange;
        }

        if (health > maxHealth) health = maxHealth;
        if (health < 0) health = 0;

        gameObject.SendMessage("HealthChanged", new HealthChangedMessage(health, maxHealth, oldHealth, oldMaxHealth), SendMessageOptions.DontRequireReceiver);
        Debug.Log("Health: " + oldHealth + " -> " + health+ (maxHealthChange != 0 ? ", maxHealth: " + oldMaxHealth + " -> " + maxHealth:""));
    }
}
