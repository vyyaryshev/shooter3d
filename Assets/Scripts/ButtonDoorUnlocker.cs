using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonDoorUnlocker : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private List<PressableButton> requiredButtons = new List<PressableButton>();

    [Header("Door Movement")]
    [SerializeField] private Transform door;
    [SerializeField] private Transform openedTarget;
    [SerializeField] private Vector3 localOpenOffset = new Vector3(3f, 0f, 0f);
    [SerializeField] private float openDuration = 1.5f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent unlocked;

    private Vector3 closedPosition;
    private Vector3 openedPosition;
    private Coroutine openRoutine;
    private bool isUnlocked;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        if (door == null)
            door = transform;

        closedPosition = door.position;
        openedPosition = openedTarget != null ? openedTarget.position : door.TransformPoint(localOpenOffset);
    }

    private void OnEnable()
    {
        for (int i = 0; i < requiredButtons.Count; i++)
        {
            if (requiredButtons[i] == null)
                continue;

            requiredButtons[i].Pressed += HandleButtonChanged;
            requiredButtons[i].Released += HandleButtonChanged;
        }

        CheckUnlockState();
    }

    private void OnDisable()
    {
        for (int i = 0; i < requiredButtons.Count; i++)
        {
            if (requiredButtons[i] == null)
                continue;

            requiredButtons[i].Pressed -= HandleButtonChanged;
            requiredButtons[i].Released -= HandleButtonChanged;
        }
    }

    public void CheckUnlockState()
    {
        if (isUnlocked || requiredButtons.Count == 0)
            return;

        for (int i = 0; i < requiredButtons.Count; i++)
        {
            if (requiredButtons[i] == null || !requiredButtons[i].IsPressed)
                return;
        }

        Unlock();
    }

    public void Unlock()
    {
        if (isUnlocked)
            return;

        isUnlocked = true;
        unlocked.Invoke();

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenDoor());
    }

    private void HandleButtonChanged(PressableButton button)
    {
        CheckUnlockState();
    }

    private IEnumerator OpenDoor()
    {
        Vector3 startPosition = door.position;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            door.position = Vector3.LerpUnclamped(startPosition, openedPosition, openCurve.Evaluate(t));
            yield return null;
        }

        door.position = openedPosition;
        openRoutine = null;
    }

    private void OnValidate()
    {
        if (door == null)
            door = transform;
    }
}
