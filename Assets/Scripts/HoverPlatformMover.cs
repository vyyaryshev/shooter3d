using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class HoverPlatformMover : MonoBehaviour
{
    [Header("Platform")]
    [SerializeField] private Transform platformRoot;
    [SerializeField] private bool clearStaticFlagsInEditor = true;

    [Header("Points")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private bool startAtPointA = true;

    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waitAtPoint = 0.4f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useRigidbodyMovement = true;

    [Header("Passenger")]
    [SerializeField] private bool parentPlayerWhileOnPlatform = true;
    [SerializeField] private string playerTag = "Player";

    private Rigidbody platformRigidbody;
    private Coroutine moveRoutine;
    private Transform currentPassenger;
    private Transform originalPassengerParent;

    private void Awake()
    {
        if (platformRoot == null)
            platformRoot = transform;

        platformRigidbody = platformRoot.GetComponent<Rigidbody>();

        if (platformRigidbody != null && useRigidbodyMovement)
        {
            platformRigidbody.isKinematic = true;
            platformRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        WarnIfPlatformLooksStatic();
    }

    private void OnEnable()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"{nameof(HoverPlatformMover)} on {name}: assign Point A and Point B.", this);
            return;
        }

        SetPlatformPosition(startAtPointA ? pointA.position : pointB.position);
        moveRoutine = StartCoroutine(MoveBetweenPoints());
    }

    private void OnDisable()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        ReleasePassenger();
    }

    private IEnumerator MoveBetweenPoints()
    {
        Transform fromPoint = startAtPointA ? pointA : pointB;
        Transform toPoint = startAtPointA ? pointB : pointA;

        while (enabled)
        {
            yield return MoveToPoint(fromPoint.position, toPoint.position);

            if (waitAtPoint > 0f)
                yield return new WaitForSeconds(waitAtPoint);

            Transform previousFrom = fromPoint;
            fromPoint = toPoint;
            toPoint = previousFrom;
        }
    }

    private IEnumerator MoveToPoint(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(from, to);
        float duration = speed > 0f ? distance / speed : 0f;

        if (duration <= 0f)
        {
            SetPlatformPosition(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = movementCurve.Evaluate(t);
            SetPlatformPosition(Vector3.LerpUnclamped(from, to, curvedT));
            yield return null;
        }

        SetPlatformPosition(to);
    }

    private void SetPlatformPosition(Vector3 position)
    {
        if (platformRigidbody != null && useRigidbodyMovement)
            platformRigidbody.MovePosition(position);
        else
            platformRoot.position = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!parentPlayerWhileOnPlatform || currentPassenger != null || !other.CompareTag(playerTag))
            return;

        currentPassenger = other.transform;
        originalPassengerParent = currentPassenger.parent;
        currentPassenger.SetParent(platformRoot, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!parentPlayerWhileOnPlatform || currentPassenger == null || other.transform != currentPassenger)
            return;

        ReleasePassenger();
    }

    private void ReleasePassenger()
    {
        if (currentPassenger == null)
            return;

        currentPassenger.SetParent(originalPassengerParent, true);
        currentPassenger = null;
        originalPassengerParent = null;
    }

    private void OnValidate()
    {
        if (platformRoot == null)
            platformRoot = transform;

        if (speed < 0f)
            speed = 0f;

        if (waitAtPoint < 0f)
            waitAtPoint = 0f;

        ClearStaticFlags();
    }

    private void WarnIfPlatformLooksStatic()
    {
        if (platformRoot == null)
            return;

        Transform[] parts = platformRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null && parts[i].gameObject.isStatic)
            {
                Debug.LogWarning(
                    $"{nameof(HoverPlatformMover)} on {name}: platform part '{parts[i].name}' is Static. " +
                    "Moving LOD platforms and their child renderers must not be Static.",
                    parts[i]);
                return;
            }
        }
    }

    private void ClearStaticFlags()
    {
#if UNITY_EDITOR
        if (!clearStaticFlagsInEditor || platformRoot == null)
            return;

        Transform[] parts = platformRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null)
                continue;

            GameObject part = parts[i].gameObject;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(part);
            if (flags == 0)
                continue;

            GameObjectUtility.SetStaticEditorFlags(part, 0);
            EditorUtility.SetDirty(part);
        }
#endif
    }
}
