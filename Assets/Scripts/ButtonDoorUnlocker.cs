using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private bool clearStaticFlagsInEditor = true;

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

        WarnIfDoorLooksStatic();
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

        ClearStaticFlags();
    }

    private void WarnIfDoorLooksStatic()
    {
        if (door == null)
            return;

        Transform[] doorParts = door.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < doorParts.Length; i++)
        {
            if (doorParts[i] != null && doorParts[i].gameObject.isStatic)
            {
                Debug.LogWarning(
                    $"{nameof(ButtonDoorUnlocker)} on {name}: moving door part '{doorParts[i].name}' is Static. " +
                    "Static objects can keep their rendered mesh in place while colliders move. Disable Static on the door and its children.",
                    doorParts[i]);
                return;
            }
        }
    }

    private void ClearStaticFlags()
    {
#if UNITY_EDITOR
        if (!clearStaticFlagsInEditor || door == null)
            return;

        Transform[] doorParts = door.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < doorParts.Length; i++)
        {
            if (doorParts[i] == null)
                continue;

            GameObject part = doorParts[i].gameObject;
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(part);
            if (staticFlags == 0)
                continue;

            GameObjectUtility.SetStaticEditorFlags(part, 0);
            EditorUtility.SetDirty(part);
        }
#endif
    }
}
