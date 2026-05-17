using UnityEngine;
using UnityEngine.UI;

public class HealthIndicator : MonoBehaviour
{
    [SerializeField] private Slider healthBar;

    public void HealthChanged(HealthChangedMessage message)
    {
        if (healthBar == null)
            return;

        if (message.maxHealth <= 0)
        {
            healthBar.value = 0f;
            return;
        }

        healthBar.value = Mathf.Clamp01((float)(message.health / message.maxHealth));
    }
}
