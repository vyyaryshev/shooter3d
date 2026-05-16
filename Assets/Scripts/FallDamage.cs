using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FallDamage : MonoBehaviour
{
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Health playerHealth;
    [SerializeField] private GroundCheck groundCheck;
    [SerializeField] private float safeFallSpeed = 10f;
    [SerializeField] private float damageMultiplier = 5f;

    private bool wasGrounded = true;
    private float maxFallSpeed;

    private void Awake()
    {
        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody>();

        if (playerHealth == null)
            playerHealth = GetComponent<Health>();

        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck>();
    }

    private void Update()
    {
        if (playerRigidbody == null || playerHealth == null || groundCheck == null)
            return;

        bool isGrounded = groundCheck.isGrounded;

        if (!isGrounded)
        {
            float fallingSpeed = -playerRigidbody.linearVelocity.y;

            if (fallingSpeed > maxFallSpeed)
                maxFallSpeed = fallingSpeed;
        }

        if (isGrounded && !wasGrounded)
        {
            ApplyFallDamage();
            maxFallSpeed = 0f;
        }

        wasGrounded = isGrounded;
    }

    private void ApplyFallDamage()
    {
        if (maxFallSpeed <= safeFallSpeed)
            return;

        int damage = Mathf.RoundToInt((maxFallSpeed - safeFallSpeed) * damageMultiplier);

        if (damage <= 0)
            return;

        Debug.Log("Урон от падения: " + damage);
        playerHealth.Change(-damage);
    }
}
