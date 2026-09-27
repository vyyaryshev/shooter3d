using UnityEngine;

[DisallowMultipleComponent]
public class SpiderProceduralWalker : MonoBehaviour
{
    [System.Serializable]
    private class SpiderLeg
    {
        public string footName;
        public Transform foot;
        public Transform footEnd;
        public float phaseOffset;

        [HideInInspector] public Quaternion startLocalRotation;
        [HideInInspector] public Vector3 homeLocalPosition;
        [HideInInspector] public Vector3 plantedPosition;
        [HideInInspector] public Vector3 stepStart;
        [HideInInspector] public Vector3 stepTarget;
        [HideInInspector] public float stepProgress;
        [HideInInspector] public bool stepping;
    }

    [Header("Legs")]
    [SerializeField] private SpiderLeg[] legs =
    {
        new SpiderLeg { footName = "Foot", phaseOffset = 0f },
        new SpiderLeg { footName = "Foot.001", phaseOffset = 0.5f },
        new SpiderLeg { footName = "Foot.002", phaseOffset = 0.5f },
        new SpiderLeg { footName = "Foot.003", phaseOffset = 0f }
    };

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayHeight = 0.4f;
    [SerializeField] private float groundRayDistance = 1.2f;
    [SerializeField] private float footGroundOffset = 0.02f;

    [Header("Step")]
    [SerializeField] private float stepDistance = 0.18f;
    [SerializeField] private float stepDuration = 0.16f;
    [SerializeField] private float stepHeight = 0.08f;
    [SerializeField] private float movePrediction = 0.12f;
    [SerializeField] private int maxMovingLegs = 2;

    [Header("Body")]
    [SerializeField] private Transform body;
    [SerializeField] private bool animateBody = true;
    [SerializeField] private float bodyBobHeight = 0.025f;
    [SerializeField] private float bodyBobSpeed = 8f;

    private Vector3 lastPosition;
    private Vector3 velocity;
    private Vector3 startBodyLocalPosition;

    private void Awake()
    {
        if (body == null)
            body = FindChildByName(transform, "Body");

        AutoFindLegs();
        InitializeLegs();
        lastPosition = transform.position;
        if (body != null)
            startBodyLocalPosition = body.localPosition;
    }

    private void Update()
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        velocity = (transform.position - lastPosition) / deltaTime;
        lastPosition = transform.position;

        UpdateStepping(deltaTime);
        UpdateBodyBob();
    }

    private void LateUpdate()
    {
        ApplyLegRotations();
    }

    private void AutoFindLegs()
    {
        if (legs == null)
            return;

        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] == null)
                continue;

            if (legs[i].foot == null && !string.IsNullOrWhiteSpace(legs[i].footName))
                legs[i].foot = FindChildByName(transform, legs[i].footName);

            if (legs[i].footEnd == null && legs[i].foot != null)
                legs[i].footEnd = FindChildByName(legs[i].foot, legs[i].foot.name + "_end");
        }
    }

    private void InitializeLegs()
    {
        if (legs == null)
            return;

        for (int i = 0; i < legs.Length; i++)
        {
            SpiderLeg leg = legs[i];
            if (!IsValidLeg(leg))
                continue;

            leg.startLocalRotation = leg.foot.localRotation;
            leg.homeLocalPosition = transform.InverseTransformPoint(leg.footEnd.position);
            leg.plantedPosition = ProjectToGround(transform.TransformPoint(leg.homeLocalPosition), leg.footEnd.position);
            leg.stepStart = leg.plantedPosition;
            leg.stepTarget = leg.plantedPosition;
            leg.stepProgress = 1f;
            leg.stepping = false;
        }
    }

    private void UpdateStepping(float deltaTime)
    {
        if (legs == null)
            return;

        int movingLegs = CountMovingLegs();

        for (int i = 0; i < legs.Length; i++)
        {
            SpiderLeg leg = legs[i];
            if (!IsValidLeg(leg))
                continue;

            if (leg.stepping)
            {
                UpdateLegStep(leg, deltaTime);
                continue;
            }

            if (movingLegs >= maxMovingLegs)
                continue;

            Vector3 desiredPosition = GetDesiredFootPosition(leg);
            float distance = Vector3.Distance(Flatten(leg.plantedPosition), Flatten(desiredPosition));
            if (distance < stepDistance)
                continue;

            leg.stepStart = leg.plantedPosition;
            leg.stepTarget = desiredPosition;
            leg.stepProgress = 0f;
            leg.stepping = true;
            movingLegs++;
        }
    }

    private void UpdateLegStep(SpiderLeg leg, float deltaTime)
    {
        float duration = Mathf.Max(0.01f, stepDuration);
        leg.stepProgress = Mathf.Clamp01(leg.stepProgress + deltaTime / duration);

        float t = SmoothStep(leg.stepProgress);
        Vector3 position = Vector3.Lerp(leg.stepStart, leg.stepTarget, t);
        position += Vector3.up * Mathf.Sin(t * Mathf.PI) * stepHeight;

        leg.plantedPosition = position;

        if (leg.stepProgress >= 1f)
        {
            leg.plantedPosition = leg.stepTarget;
            leg.stepping = false;
        }
    }

    private Vector3 GetDesiredFootPosition(SpiderLeg leg)
    {
        Vector3 homeWorld = transform.TransformPoint(leg.homeLocalPosition);
        Vector3 predicted = homeWorld + Flatten(velocity) * movePrediction;
        return ProjectToGround(predicted, homeWorld);
    }

    private Vector3 ProjectToGround(Vector3 originPosition, Vector3 fallback)
    {
        Vector3 rayOrigin = originPosition + Vector3.up * groundRayHeight;
        float rayDistance = groundRayHeight + groundRayDistance;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point + hit.normal * footGroundOffset;

        return fallback;
    }

    private void ApplyLegRotations()
    {
        if (legs == null)
            return;

        for (int i = 0; i < legs.Length; i++)
        {
            SpiderLeg leg = legs[i];
            if (!IsValidLeg(leg))
                continue;

            leg.foot.localRotation = leg.startLocalRotation;

            Vector3 currentDirection = leg.footEnd.position - leg.foot.position;
            Vector3 targetDirection = leg.plantedPosition - leg.foot.position;

            if (currentDirection.sqrMagnitude < 0.000001f || targetDirection.sqrMagnitude < 0.000001f)
                continue;

            Quaternion correction = Quaternion.FromToRotation(currentDirection, targetDirection);
            leg.foot.rotation = correction * leg.foot.rotation;
        }
    }

    private void UpdateBodyBob()
    {
        if (!animateBody || body == null)
            return;

        float horizontalSpeed = Flatten(velocity).magnitude;
        float bob = Mathf.Sin(Time.time * bodyBobSpeed) * bodyBobHeight * Mathf.Clamp01(horizontalSpeed);
        body.localPosition = startBodyLocalPosition + Vector3.up * bob;
    }

    private int CountMovingLegs()
    {
        int count = 0;

        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] != null && legs[i].stepping)
                count++;
        }

        return count;
    }

    private bool IsValidLeg(SpiderLeg leg)
    {
        return leg != null && leg.foot != null && leg.footEnd != null;
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }

    private void OnValidate()
    {
        groundRayHeight = Mathf.Max(0f, groundRayHeight);
        groundRayDistance = Mathf.Max(0.01f, groundRayDistance);
        footGroundOffset = Mathf.Max(0f, footGroundOffset);
        stepDistance = Mathf.Max(0.01f, stepDistance);
        stepDuration = Mathf.Max(0.01f, stepDuration);
        stepHeight = Mathf.Max(0f, stepHeight);
        movePrediction = Mathf.Max(0f, movePrediction);
        maxMovingLegs = Mathf.Clamp(maxMovingLegs, 1, 4);
        bodyBobHeight = Mathf.Max(0f, bodyBobHeight);
        bodyBobSpeed = Mathf.Max(0f, bodyBobSpeed);
    }
}
