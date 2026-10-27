using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5f;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9f;
    public Key runningKey = Key.LeftShift;

    [Header("Jump")]
    public float jumpForce = 7f;
    public Key jumpKey = Key.Space;

    [Header("Dodge")]
    [SerializeField] private Key dodgeKey = Key.LeftCtrl;
    [SerializeField, Min(0.1f)] private float dodgeDistance = 3f;
    [SerializeField, Min(0.05f)] private float dodgeDuration = 0.2f;
    [SerializeField, Min(0f)] private float dodgeCooldown = 1f;
    [SerializeField] private bool dodgeOnlyOnGround = true;
    public bool IsDodging => dodgeTimeRemaining > 0f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.2f;
    public float maxGroundAngle = 45f;
    public LayerMask groundMask = ~0;

    private Rigidbody rigidbody;
    private bool isGrounded;
    private bool dodgeRequested;
    private Vector3 dodgeDirection;
    private float dodgeTimeRemaining;
    private float nextDodgeTime;

    public List<Func<float>> speedOverrides = new List<Func<float>>();

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Keyboard.current == null || Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
            return;
        if (dodgeRequested || IsDodging || Time.time < nextDodgeTime)
            return;
        if (Keyboard.current[dodgeKey].wasPressedThisFrame
            || (dodgeKey == Key.LeftCtrl && Keyboard.current.rightCtrlKey.wasPressedThisFrame))
        {
            Vector2 input = ReadMoveInput();
            Vector3 direction = transform.TransformDirection(new Vector3(input.x, 0f, input.y));
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            dodgeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized
                : Vector3.ProjectOnPlane(-transform.forward, Vector3.up).normalized;
            dodgeRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (Keyboard.current == null)
            return;

        CheckGround();

        if (dodgeRequested)
        {
            dodgeRequested = false;
            if (!IsDodging && Time.time >= nextDodgeTime && (!dodgeOnlyOnGround || isGrounded))
            {
                dodgeTimeRemaining = dodgeDuration;
                nextDodgeTime = Time.time + dodgeCooldown;
            }
        }
        if (IsDodging)
        {
            IsRunning = false;
            float stepTime = Mathf.Min(dodgeTimeRemaining, Time.fixedDeltaTime);
            float distance = dodgeDistance / dodgeDuration * stepTime;
            // Check the Rigidbody's shape before each step to avoid dashing through a wall.
            if (rigidbody.SweepTest(dodgeDirection, out RaycastHit obstacle, distance + 0.05f, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(0f, obstacle.distance - 0.05f);
                dodgeTimeRemaining = 0f;
            }
            else
                dodgeTimeRemaining = Mathf.Max(0f, dodgeTimeRemaining - Time.fixedDeltaTime);
            Vector3 velocity = dodgeDirection * (distance / Time.fixedDeltaTime);
            velocity.y = rigidbody.linearVelocity.y;
            rigidbody.linearVelocity = velocity;
            return;
        }

        // Бег
        IsRunning = canRun && Keyboard.current[runningKey].isPressed;

        float targetMovingSpeed = IsRunning ? runSpeed : speed;

        if (speedOverrides.Count > 0)
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

        // Ввод движения
        Vector2 targetVelocity = ReadMoveInput() * targetMovingSpeed;

        Vector3 worldVelocity = transform.rotation * new Vector3(
            targetVelocity.x,
            rigidbody.linearVelocity.y,
            targetVelocity.y
        );

        rigidbody.linearVelocity = worldVelocity;

        // Прыжок
        if (Keyboard.current[jumpKey].wasPressedThisFrame && isGrounded)
        {
            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private Vector2 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
        return new Vector2(x, y);
    }

    private void OnDisable()
    {
        dodgeRequested = false;
        if (IsDodging && rigidbody != null)
            rigidbody.linearVelocity = Vector3.up * rigidbody.linearVelocity.y;
        dodgeTimeRemaining = 0f;
    }

    private void CheckGround()
    {
        isGrounded = false;

        Vector3 rayStart = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            if (angle <= maxGroundAngle)
            {
                isGrounded = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;

        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundCheckDistance);
    }
}
