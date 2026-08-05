using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Collider))]
public class PressableButton : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private string playerTag = "Player";

    [Header("Button Visual")]
    [SerializeField] private Transform movingPart;
    [SerializeField] private Vector3 localPressOffset = new Vector3(0f, -0.08f, 0f);
    [SerializeField] private float pressDuration = 0.15f;
    [SerializeField] private AnimationCurve pressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Materials")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material idleMaterial;
    [SerializeField] private Material pressedMaterial;

    [Header("Behaviour")]
    [SerializeField] private bool pressOnce = true;
    [SerializeField] private bool startPressed;

    [Header("Events")]
    [SerializeField] private UnityEvent pressed;
    [SerializeField] private UnityEvent released;

    public event Action<PressableButton> Pressed;
    public event Action<PressableButton> Released;

    private Vector3 releasedLocalPosition;
    private Vector3 pressedLocalPosition;
    private Coroutine animationRoutine;
    private bool playerInside;
    private bool isPressed;

    public bool IsPressed => isPressed;

    private void Awake()
    {
        if (movingPart == null)
            movingPart = transform;

        releasedLocalPosition = movingPart.localPosition;
        pressedLocalPosition = releasedLocalPosition + localPressOffset;
        SetPressedState(startPressed, true);
    }

    private void Update()
    {
        if (!playerInside || Keyboard.current == null)
            return;

        KeyControl keyControl = Keyboard.current[interactKey];
        if (keyControl == null || !keyControl.wasPressedThisFrame)
            return;

        if (pressOnce)
            Press();
        else
            SetPressedState(!isPressed, false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInside = false;
    }

    public void Press()
    {
        SetPressedState(true, false);
    }

    public void Release()
    {
        if (pressOnce)
            return;

        SetPressedState(false, false);
    }

    private void SetPressedState(bool pressedState, bool instant)
    {
        if (isPressed == pressedState && !instant)
            return;

        isPressed = pressedState;
        ApplyMaterial();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        Vector3 targetPosition = isPressed ? pressedLocalPosition : releasedLocalPosition;
        if (instant || pressDuration <= 0f)
            movingPart.localPosition = targetPosition;
        else
            animationRoutine = StartCoroutine(AnimateButton(targetPosition));

        if (isPressed)
        {
            pressed.Invoke();
            Pressed?.Invoke(this);
        }
        else
        {
            released.Invoke();
            Released?.Invoke(this);
        }
    }

    private IEnumerator AnimateButton(Vector3 targetPosition)
    {
        Vector3 startPosition = movingPart.localPosition;
        float elapsed = 0f;

        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pressDuration);
            movingPart.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, pressCurve.Evaluate(t));
            yield return null;
        }

        movingPart.localPosition = targetPosition;
        animationRoutine = null;
    }

    private void ApplyMaterial()
    {
        if (targetRenderer == null)
            return;

        Material material = isPressed ? pressedMaterial : idleMaterial;
        if (material != null)
            targetRenderer.material = material;
    }

    private void OnValidate()
    {
        if (movingPart == null)
            movingPart = transform;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        Collider buttonCollider = GetComponent<Collider>();
        if (buttonCollider != null)
            buttonCollider.isTrigger = true;
    }
}
