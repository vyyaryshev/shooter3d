using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArenaRoom : MonoBehaviour
{
    [Header("Activation")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool activateOnlyOnce = true;

    [Header("Spawning")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform spawnedEnemiesParent;
    [SerializeField] private bool spawnAllEnemies = true;
    [SerializeField] private int enemyCountToSpawn = 1;

    [Header("Exit")]
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private Transform exitSpawnPoint;
    [SerializeField] private Vector3 exitMoveDirection = Vector3.right;
    [SerializeField] private float exitMoveDistance = 4f;
    [SerializeField] private float exitMoveDuration = 1.5f;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private GameObject exitInstance;
    private Coroutine exitMoveRoutine;
    private Transform activatingPlayer;
    private bool activated;
    private bool completed;

    private void Awake()
    {
        if (exitPrefab != null)
        {
            Vector3 position = exitSpawnPoint != null ? exitSpawnPoint.position : exitPrefab.transform.position;
            Quaternion rotation = exitSpawnPoint != null ? exitSpawnPoint.rotation : exitPrefab.transform.rotation;
            exitInstance = Instantiate(exitPrefab, position, rotation, transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed)
            return;

        if (activateOnlyOnce && activated)
            return;

        if (!other.CompareTag(playerTag))
            return;

        activatingPlayer = other.transform;
        ActivateArena();
    }

    private void ActivateArena()
    {
        activated = true;
        SpawnEnemies();
        CheckCompletion();
    }

    private void SpawnEnemies()
    {
        aliveEnemies.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        int count = spawnAllEnemies ? enemyPrefabs.Length : Mathf.Min(enemyCountToSpawn, enemyPrefabs.Length, spawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = enemyPrefabs[i];
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];

            if (prefab == null || spawnPoint == null)
                continue;

            GameObject enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnedEnemiesParent);
            AssignTarget(enemy);
            TrackEnemy(enemy);
        }
    }

    private void AssignTarget(GameObject enemy)
    {
        if (enemy == null || activatingPlayer == null)
            return;

        MutantAI[] mutants = enemy.GetComponentsInChildren<MutantAI>(true);
        for (int i = 0; i < mutants.Length; i++)
            mutants[i].SetPlayer(activatingPlayer);

        RoboDroneAI[] drones = enemy.GetComponentsInChildren<RoboDroneAI>(true);
        for (int i = 0; i < drones.Length; i++)
            drones[i].SetPlayer(activatingPlayer);
    }

    private void TrackEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        Health health = enemy.GetComponentInChildren<Health>();
        if (health == null)
        {
            Debug.LogWarning($"{name}: enemy '{enemy.name}' has no Health component.", enemy);
            return;
        }

        GameObject healthOwner = health.gameObject;
        ArenaEnemyDeathReporter reporter = healthOwner.GetComponent<ArenaEnemyDeathReporter>();
        if (reporter == null)
            reporter = healthOwner.AddComponent<ArenaEnemyDeathReporter>();

        reporter.Initialize(this, enemy);
        aliveEnemies.Add(enemy);
    }

    public void NotifyEnemyDied(GameObject enemy)
    {
        if (enemy != null)
            aliveEnemies.Remove(enemy);

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        RemoveDestroyedEnemies();

        if (!activated || completed || aliveEnemies.Count > 0)
            return;

        completed = true;
        OpenExit();
    }

    private void RemoveDestroyedEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
                aliveEnemies.RemoveAt(i);
        }
    }

    private void OpenExit()
    {
        if (exitInstance == null)
            return;

        if (exitMoveRoutine != null)
            StopCoroutine(exitMoveRoutine);

        exitMoveRoutine = StartCoroutine(MoveExitSideways());
    }

    private IEnumerator MoveExitSideways()
    {
        Transform exitTransform = exitInstance.transform;
        Vector3 startPosition = exitTransform.position;
        Vector3 direction = exitMoveDirection.sqrMagnitude > 0.01f ? exitMoveDirection.normalized : Vector3.right;
        Vector3 targetPosition = startPosition + direction * exitMoveDistance;
        float duration = Mathf.Max(0.01f, exitMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            exitTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        exitTransform.position = targetPosition;
    }
}

[DisallowMultipleComponent]
public class ArenaEnemyDeathReporter : MonoBehaviour
{
    private ArenaRoom arenaRoom;
    private GameObject enemyRoot;
    private bool reported;

    public void Initialize(ArenaRoom room, GameObject enemy)
    {
        arenaRoom = room;
        enemyRoot = enemy;
        reported = false;
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (reported || message.health > 0)
            return;

        reported = true;

        if (arenaRoom != null)
            arenaRoom.NotifyEnemyDied(enemyRoot);
    }
}
