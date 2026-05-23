using UnityEngine;

public class PooledEffect : MonoBehaviour
{
    private GameObject prefab;
    private float returnTime;

    public void Play(GameObject sourcePrefab, float lifetime)
    {
        prefab = sourcePrefab;
        returnTime = Time.time + Mathf.Max(0.05f, lifetime);

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Clear(true);
            particles[i].Play(true);
        }
    }

    private void Update()
    {
        if (Time.time >= returnTime)
            FpsObjectPool.Return(prefab, gameObject);
    }
}
