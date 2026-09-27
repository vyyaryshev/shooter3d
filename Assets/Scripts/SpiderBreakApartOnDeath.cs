using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class SpiderBreakApartOnDeath : MonoBehaviour
{
    [Header("Visual Source")]
    [SerializeField] private SkinnedMeshRenderer spiderRenderer;
    [SerializeField] private Transform[] breakPoints;
    [SerializeField] private string[] autoBreakPointNames =
    {
        "Body",
        "Foot",
        "Foot_end",
        "Foot.001",
        "Foot.001_end",
        "Foot.002",
        "Foot.002_end",
        "Foot.003",
        "Foot.003_end"
    };

    [Header("Fragments")]
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private Material fragmentMaterial;
    [SerializeField] private PrimitiveType fallbackPrimitive = PrimitiveType.Capsule;
    [SerializeField] private Vector3 bodyFragmentScale = new Vector3(0.35f, 0.18f, 0.35f);
    [SerializeField] private Vector3 legFragmentScale = new Vector3(0.08f, 0.22f, 0.08f);
    [SerializeField] private float fragmentLifetime = 8f;

    [Header("Force")]
    [SerializeField] private float explosionForce = 2.5f;
    [SerializeField] private float explosionRadius = 1.2f;
    [SerializeField] private float upwardModifier = 0.25f;
    [SerializeField] private float randomTorque = 8f;

    [Header("Cleanup")]
    [SerializeField] private bool disableOriginalRenderers = true;
    [SerializeField] private bool disableOriginalColliders = true;
    [SerializeField] private bool destroyOriginalAfterBreak = true;
    [SerializeField] private float destroyOriginalDelay = 0.1f;

    private bool broken;

    private void Awake()
    {
        ResolveReferences();
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (broken || message.health > 0)
            return;

        BreakApart();
    }

    public void BreakApart()
    {
        if (broken)
            return;

        broken = true;
        ResolveReferences();
        StopSpiderBehaviour();
        SpawnFragments();
        HideOriginal();

        if (destroyOriginalAfterBreak)
            Destroy(gameObject, Mathf.Max(0f, destroyOriginalDelay));
    }

    private void SpawnFragments()
    {
        Transform[] points = GetBreakPoints();
        Vector3 explosionCenter = transform.position;

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            if (point == null)
                continue;

            GameObject fragment = CreateFragment(point);
            if (fragment == null)
                continue;

            Rigidbody fragmentRigidbody = EnsureRigidbody(fragment);
            EnsureCollider(fragment);

            if (fragmentRigidbody != null)
            {
                fragmentRigidbody.AddExplosionForce(explosionForce, explosionCenter, explosionRadius, upwardModifier, ForceMode.Impulse);
                fragmentRigidbody.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
            }

            if (fragmentLifetime > 0f)
                Destroy(fragment, fragmentLifetime);
        }
    }

    private GameObject CreateFragment(Transform point)
    {
        GameObject fragment;
        if (fragmentPrefab != null)
        {
            fragment = Instantiate(fragmentPrefab, point.position, point.rotation);
        }
        else
        {
            fragment = GameObject.CreatePrimitive(fallbackPrimitive);
            fragment.name = point.name + "_Fragment";
            fragment.transform.SetPositionAndRotation(point.position, point.rotation);
            fragment.transform.localScale = IsBodyPoint(point) ? bodyFragmentScale : legFragmentScale;

            Renderer renderer = fragment.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetFragmentMaterial();
        }

        fragment.transform.SetParent(null, true);
        return fragment;
    }

    private Rigidbody EnsureRigidbody(GameObject fragment)
    {
        if (fragment.TryGetComponent(out Rigidbody fragmentRigidbody))
            return fragmentRigidbody;

        return fragment.AddComponent<Rigidbody>();
    }

    private void EnsureCollider(GameObject fragment)
    {
        Collider collider = fragment.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = false;
            return;
        }

        fragment.AddComponent<BoxCollider>();
    }

    private void StopSpiderBehaviour()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.enabled = false;

        DisableBehaviour<MutantAI>();
        DisableBehaviour<SpiderProceduralWalker>();
        DisableBehaviourByName("RoboDroneAI");
        DisableBehaviourByName("SoldierRangedAI");
    }

    private void HideOriginal()
    {
        if (disableOriginalRenderers)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }
        }

        if (disableOriginalColliders)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }
    }

    private Transform[] GetBreakPoints()
    {
        if (breakPoints != null && breakPoints.Length > 0)
            return breakPoints;

        Transform[] foundPoints = new Transform[autoBreakPointNames.Length];
        int count = 0;

        for (int i = 0; i < autoBreakPointNames.Length; i++)
        {
            Transform point = FindChildByName(transform, autoBreakPointNames[i]);
            if (point == null)
                continue;

            foundPoints[count] = point;
            count++;
        }

        Transform[] result = new Transform[count];
        for (int i = 0; i < count; i++)
            result[i] = foundPoints[i];

        return result;
    }

    private Material GetFragmentMaterial()
    {
        if (fragmentMaterial != null)
            return fragmentMaterial;

        if (spiderRenderer != null)
            return spiderRenderer.sharedMaterial;

        return null;
    }

    private bool IsBodyPoint(Transform point)
    {
        return point != null && point.name == "Body";
    }

    private void ResolveReferences()
    {
        if (spiderRenderer == null)
            spiderRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    private void DisableBehaviour<T>() where T : Behaviour
    {
        T behaviour = GetComponentInChildren<T>();
        if (behaviour != null)
            behaviour.enabled = false;
    }

    private void DisableBehaviourByName(string behaviourTypeName)
    {
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null && behaviours[i] != this && behaviours[i].GetType().Name == behaviourTypeName)
                behaviours[i].enabled = false;
        }
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
        ResolveReferences();
        fragmentLifetime = Mathf.Max(0f, fragmentLifetime);
        explosionForce = Mathf.Max(0f, explosionForce);
        explosionRadius = Mathf.Max(0.01f, explosionRadius);
        upwardModifier = Mathf.Max(0f, upwardModifier);
        randomTorque = Mathf.Max(0f, randomTorque);
        destroyOriginalDelay = Mathf.Max(0f, destroyOriginalDelay);
    }
}
