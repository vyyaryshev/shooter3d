using UnityEngine;
using UnityEngine.UI;

public class HealthIndicator : MonoBehaviour
{
    [SerializeField] private Slider healthBar;

    void Start()
    {
        Health health = GetComponent<Health>();

        if (health != null)
            UpdateHealthBar(health.GetHealth(), health.GetMaxHealth());
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        UpdateHealthBar(message.health, message.maxHealth);
    }

    private void UpdateHealthBar(double health, double maxHealth)
    {
        if (healthBar == null)
            return;

        if (maxHealth <= 0)
        {
            healthBar.value = 0f;
            return;
        }

        healthBar.value = Mathf.Clamp01((float)(health / maxHealth));
    }
}
