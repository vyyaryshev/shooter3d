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

    [Header("Ground Check")]
    public float groundCheckDistance = 1.2f;
    public float maxGroundAngle = 45f;
    public LayerMask groundMask = ~0;

    private Rigidbody rigidbody;
    private bool isGrounded;

    public List<Func<float>> speedOverrides = new List<Func<float>>();

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (Keyboard.current == null)
            return;

        CheckGround();

        // Бег
        IsRunning = canRun && Keyboard.current[runningKey].isPressed;

        float targetMovingSpeed = IsRunning ? runSpeed : speed;

        if (speedOverrides.Count > 0)
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

        // Ввод движения
        float moveX = 0f;
        float moveY = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveY = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveY = -1f;

        Vector2 targetVelocity = new Vector2(moveX, moveY) * targetMovingSpeed;

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