using UnityEngine;
using UnityEngine.SceneManagement;

public class RocketExhaustEffect : MonoBehaviour
{
    private Transform rocket;
    private Transform anchor;
    private Vector3 localPosition;
    private Vector3 noseAxis;
    private Material material;
    private Texture2D texture;
    private bool stopped;

    public static RocketExhaustEffect Create(Transform rocket, Transform exhaustPoint,
        Vector3 localTail, Vector3 modelAxis, float length, float width)
    {
        GameObject root = new GameObject("Rocket Burning Tail");
        SceneManager.MoveGameObjectToScene(root, rocket.gameObject.scene);
        RocketExhaustEffect effect = root.AddComponent<RocketExhaustEffect>();
        effect.rocket = rocket;
        effect.anchor = exhaustPoint != null ? exhaustPoint : rocket;
        effect.localPosition = exhaustPoint != null ? Vector3.zero : localTail;
        effect.noseAxis = modelAxis;
        effect.FollowRocket();
        effect.material = RocketExplosionEffect.CreateParticleMaterial(out effect.texture);
        effect.AddEmitter("Flame", new Color(1f, 0.65f, 0.08f), 100f, 0.18f, length / 0.3f, width, false);
        effect.AddEmitter("Smoke", new Color(0.2f, 0.2f, 0.2f, 0.45f), 30f, 1.2f, 0.8f, width * 1.8f, true);
        return effect;
    }

    private void AddEmitter(string name, Color color, float rate, float lifetime, float speed, float width, bool smoke)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = true;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(width * 0.5f, width);
        main.startColor = color;
        // Old particles stay in the world when the rocket turns or is destroyed.
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;
        var emission = particles.emission;
        emission.rateOverTime = rate;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = smoke ? 15f : 6f;
        shape.radius = width * 0.15f;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.Linear(0f, smoke ? 0.5f : 1f, 1f, smoke ? 2f : 0.1f));
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f),
            new GradientColorKey(smoke ? Color.white : new Color(1f, 0.25f, 0.02f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        var colors = particles.colorOverLifetime;
        colors.enabled = true;
        colors.color = fade;
        if (material != null)
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        particles.Play();
    }

    private void LateUpdate()
    {
        if (stopped)
            return;
        if (rocket == null || anchor == null)
            Stop();
        else
            FollowRocket();
    }

    private void FollowRocket()
    {
        transform.position = anchor.TransformPoint(localPosition);
        Vector3 direction = -rocket.TransformDirection(noseAxis).normalized;
        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        transform.rotation = Quaternion.LookRotation(direction, up);
    }

    public void Stop()
    {
        if (stopped)
            return;
        stopped = true;
        foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        Destroy(gameObject, 1.5f);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (texture != null)
            Destroy(texture);
    }
}
