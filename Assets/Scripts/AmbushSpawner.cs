using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AmbushSpawner : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

    [Header("Spawn")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform spawnedEnemiesParent;
    [SerializeField] private float firstSpawnDelay;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private bool randomizeSpawnInterval;
    [SerializeField] private float minSpawnInterval = 0.25f;
    [SerializeField] private float maxSpawnInterval = 1f;
    [SerializeField] private float spawnPositionRadius = 0.5f;
    [SerializeField] private bool randomizeEnemies;
    [SerializeField] private bool randomizeSpawnPoints;

    [Header("Enemy Setup")]
    [SerializeField] private bool assignPlayerTarget = true;
    [SerializeField] private bool startLandingOnSpawn;

    private Transform player;
    private Coroutine spawnRoutine;
    private bool triggered;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        player = other.transform;
        StartAmbush();
    }

    public void StartAmbush()
    {
        if (triggerOnce && triggered)
            return;

        triggered = true;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning(name + ": AmbushSpawner has no enemy prefabs.", this);
            yield break;
        }

        if (firstSpawnDelay > 0f)
            yield return new WaitForSeconds(firstSpawnDelay);

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject prefab = GetEnemyPrefab(i);
            if (prefab == null)
                continue;

            Transform spawnPoint = GetSpawnPoint(i);
            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
            position += GetPositionOffset();

            GameObject enemy = Instantiate(prefab, position, rotation, spawnedEnemiesParent);
            SetupSpawnedEnemy(enemy);

            float delay = GetSpawnDelay();
            if (delay > 0f && i < enemyPrefabs.Length - 1)
                yield return new WaitForSeconds(delay);
        }

        spawnRoutine = null;
    }

    private GameObject GetEnemyPrefab(int index)
    {
        if (randomizeEnemies)
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        return enemyPrefabs[index];
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        if (randomizeSpawnPoints)
            return spawnPoints[Random.Range(0, spawnPoints.Length)];

        return spawnPoints[index % spawnPoints.Length];
    }

    private Vector3 GetPositionOffset()
    {
        if (spawnPositionRadius <= 0f)
            return Vector3.zero;

        Vector2 offset = Random.insideUnitCircle * spawnPositionRadius;
        return new Vector3(offset.x, 0f, offset.y);
    }

    private float GetSpawnDelay()
    {
        if (!randomizeSpawnInterval)
            return spawnInterval;

        return Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void SetupSpawnedEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        if (assignPlayerTarget && player != null)
            AssignPlayerTarget(enemy);

        if (startLandingOnSpawn)
        {
            NavMeshActivationOnLanding landing = enemy.GetComponentInChildren<NavMeshActivationOnLanding>(true);
            if (landing != null)
                landing.BeginLanding();
        }
    }

    private void AssignPlayerTarget(GameObject enemy)
    {
        MutantAI[] mutants = enemy.GetComponentsInChildren<MutantAI>(true);
        for (int i = 0; i < mutants.Length; i++)
            mutants[i].SetPlayer(player);

        SoldierRangedAI[] soldiers = enemy.GetComponentsInChildren<SoldierRangedAI>(true);
        for (int i = 0; i < soldiers.Length; i++)
            soldiers[i].SetPlayer(player);

        RoboDroneAI[] drones = enemy.GetComponentsInChildren<RoboDroneAI>(true);
        for (int i = 0; i < drones.Length; i++)
            drones[i].SetPlayer(player);
    }

    private void OnValidate()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        firstSpawnDelay = Mathf.Max(0f, firstSpawnDelay);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        minSpawnInterval = Mathf.Max(0f, minSpawnInterval);
        maxSpawnInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
        spawnPositionRadius = Mathf.Max(0f, spawnPositionRadius);
    }
}
