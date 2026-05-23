using UnityEngine;
using UnityEngine.InputSystem;

public class FpsWeaponController : MonoBehaviour
{
    private static readonly string[] MuzzleNames = { "MuzzlePoint", "Muzzle", "ShootPoint", "FirePoint" };

    [Header("Fire")]
    [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Automatic;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 200f;
    [SerializeField] private float roundsPerMinute = 650f;
    [SerializeField] private float spreadAngle = 0.5f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject tracerPrefab;
    [SerializeField] private float tracerLifetime = 0.08f;
    [SerializeField] private WeaponImpactSystem impactSystem;
    [SerializeField] private ShellEjector shellEjector;

    [Header("Feedback")]
    [SerializeField] private WeaponRecoil recoil;
    [SerializeField] private FpsCameraShake cameraShake;
    [SerializeField] private float cameraShakeAmount = 0.08f;
    [SerializeField] private float cameraShakeStrength = 0.03f;

    private float nextFireTime;

    private void Awake()
    {
        if (aimCamera == null)
            aimCamera = GetComponentInParent<Camera>();

        if (aimCamera == null)
            aimCamera = Camera.main;

        if (muzzlePoint == null)
            muzzlePoint = FindChildByName(MuzzleNames);

        if (impactSystem == null)
            impactSystem = GetComponent<WeaponImpactSystem>();

        if (recoil == null)
            recoil = GetComponent<WeaponRecoil>();

        if (shellEjector == null)
            shellEjector = GetComponent<ShellEjector>();
    }

    private Transform FindChildByName(string[] names)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < names.Length; i++)
        {
            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                if (children[childIndex].name == names[i])
                    return children[childIndex];
            }
        }

        return null;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        bool wantsToFire = fireMode == WeaponFireMode.Automatic
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (wantsToFire)
            TryFire();
    }

    public void TryFire()
    {
        if (Time.time < nextFireTime || aimCamera == null || muzzlePoint == null)
            return;

        nextFireTime = Time.time + 60f / Mathf.Max(1f, roundsPerMinute);
        FireShot();
    }

    private void FireShot()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play(true);

        Vector3 origin = aimCamera.transform.position;
        Vector3 direction = ApplySpread(aimCamera.transform.forward);
        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            ApplyDamage(hit);

            if (impactSystem != null)
                impactSystem.SpawnImpact(hit);
        }

        SpawnTracer(muzzlePoint.position, endPoint);

        if (recoil != null)
            recoil.AddRecoil();

        if (shellEjector != null)
            shellEjector.Eject();

        if (cameraShake != null)
            cameraShake.Shake(cameraShakeAmount, cameraShakeStrength);
    }

    private Vector3 ApplySpread(Vector3 forward)
    {
        float yaw = Random.Range(-spreadAngle, spreadAngle);
        float pitch = Random.Range(-spreadAngle, spreadAngle);
        return Quaternion.Euler(pitch, yaw, 0f) * forward;
    }

    private void ApplyDamage(RaycastHit hit)
    {
        Health health = hit.collider.GetComponentInParent<Health>();
        if (health != null)
            health.Change(-damage);
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (tracerPrefab == null)
            return;

        GameObject tracer = FpsObjectPool.Spawn(tracerPrefab, start, Quaternion.identity, tracerLifetime);
        if (tracer == null)
            return;

        FpsTracer fpsTracer = tracer.GetComponent<FpsTracer>();
        if (fpsTracer != null)
            fpsTracer.Play(start, end);
    }
}
