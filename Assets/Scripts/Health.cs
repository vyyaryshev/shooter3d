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

    public void Change(double x)
    {
        var oldHealth = health;
        var oldMaxHealth = maxHealth;

        health += x;
        ClampValues();

        NotifyHealthChanged(oldHealth, oldMaxHealth);
    }

    public void ChangeMaxHealth(double x)
    {
        var oldHealth = health;
        var oldMaxHealth = maxHealth;

        maxHealth += x;
        ClampValues();

        NotifyHealthChanged(oldHealth, oldMaxHealth);
    }

    public double SetHealth(double newHealth)
    {
        var oldHealth = health;
        var oldMaxHealth = maxHealth;

        health = newHealth;
        ClampValues();

        NotifyHealthChanged(oldHealth, oldMaxHealth);
        return health;
    }

    public double SetMaxHealth(double newMaxHealth)
    {
        var oldHealth = health;
        var oldMaxHealth = maxHealth;

        maxHealth = newMaxHealth;
        ClampValues();

        NotifyHealthChanged(oldHealth, oldMaxHealth);
        return maxHealth;
    }

    public double GetHealth() { return health; }
    public double GetMaxHealth() { return maxHealth; }

    void Start()
    {
        ClampValues();
        NotifyHealthChanged(health, maxHealth);
    }

    private void ClampValues()
    {
        if (maxHealth < 1.0) maxHealth = 1.0;
        if (health > maxHealth) health = maxHealth;
        if (health < 0) health = 0;
    }

    private void NotifyHealthChanged(double oldHealth, double oldMaxHealth)
    {
        var message = new HealthChangedMessage(health, maxHealth, oldHealth, oldMaxHealth);
        gameObject.SendMessage("HealthChanged", message, SendMessageOptions.DontRequireReceiver);
    }
}
