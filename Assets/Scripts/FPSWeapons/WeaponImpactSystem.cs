using UnityEngine;

public class WeaponImpactSystem : MonoBehaviour
{
    [SerializeField] private SurfaceEffectProfile surfaceProfile;

    public void SpawnImpact(RaycastHit hit)
    {
        if (surfaceProfile == null)
            return;

        SurfaceEffectDefinition effect = surfaceProfile.GetEffect(hit);
        if (effect == null)
            return;

        Quaternion rotation = Quaternion.LookRotation(hit.normal);
        Vector3 position = hit.point + hit.normal * effect.decalOffset;

        if (effect.impactParticles != null)
            FpsObjectPool.Spawn(effect.impactParticles, position, rotation, effect.lifetime);

        if (effect.decalPrefab != null)
            FpsObjectPool.Spawn(effect.decalPrefab, position, rotation, effect.lifetime);
    }
}
