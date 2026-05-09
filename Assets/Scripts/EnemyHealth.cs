using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;

    private Animator anim;
    private NavMeshAgent agent;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        UpdateHealthBar();
    }

    // 🔴 НАНЕСЕНИЕ УРОНА
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log("Enemy HP: " + currentHealth);

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 🟢 ЛЕЧЕНИЕ (если понадобится)
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        UpdateHealthBar();
    }

    // 📊 ОБНОВЛЕНИЕ UI
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }

    // 💀 СМЕРТЬ
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " умер 💀");

        // Останавливаем движение
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Отключаем AI
        MutantAI ai = GetComponent<MutantAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        // Выключаем коллайдеры
        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = false;
        }

        // Анимация смерти
        if (anim != null)
        {
            anim.Play("Death");
        }

        // Удаление через время
        Destroy(gameObject, 3f);
    }

    // ❗ Проверка для других скриптов
    public bool IsDead()
    {
        return isDead;
    }
}