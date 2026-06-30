using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArenaEncounter : MonoBehaviour
{
    [Header("Entry")]
    [SerializeField] private Collider entryTrigger;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool startOnlyOnce = true;

    [Header("Enemies")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform spawnedEnemiesParent;

    [Header("Completion Movement")]
    [SerializeField] private Transform objectToMove;
    [SerializeField] private Transform moveTargetPoint;
    [SerializeField] private float moveDuration = 1.5f;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private Transform player;
    private Coroutine moveRoutine;
    private bool started;
    private bool completed;

    private void Awake()
    {
        if (entryTrigger == null)
            entryTrigger = GetComponent<Collider>();

        if (entryTrigger != null)
        {
            entryTrigger.isTrigger = true;

            ArenaEncounterEntryTrigger trigger = entryTrigger.GetComponent<ArenaEncounterEntryTrigger>();
            if (trigger == null)
                trigger = entryTrigger.gameObject.AddComponent<ArenaEncounterEntryTrigger>();

            trigger.Initialize(this);
        }
    }

    public void PlayerEntered(Collider other)
    {
        if (completed || (startOnlyOnce && started))
            return;

        if (!other.CompareTag(playerTag))
            return;

        player = other.transform;
        StartEncounter();
    }

    private void StartEncounter()
    {
        started = true;
        SpawnEnemies();
        CheckCompletion();
    }

    private void SpawnEnemies()
    {
        aliveEnemies.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: ArenaEncounter has no enemy prefabs or spawn points.", this);
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            GameObject prefab = enemyPrefabs[i];
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];

            if (prefab == null || spawnPoint == null)
                continue;

            GameObject enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnedEnemiesParent);
            AssignPlayerTarget(enemy);
            TrackEnemy(enemy);
        }
    }

    private void AssignPlayerTarget(GameObject enemy)
    {
        if (enemy == null || player == null)
            return;

        MutantAI[] mutants = enemy.GetComponentsInChildren<MutantAI>(true);
        for (int i = 0; i < mutants.Length; i++)
            mutants[i].SetPlayer(player);

        RoboDroneAI[] drones = enemy.GetComponentsInChildren<RoboDroneAI>(true);
        for (int i = 0; i < drones.Length; i++)
            drones[i].SetPlayer(player);
    }

    private void TrackEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        Health health = enemy.GetComponentInChildren<Health>();
        if (health == null)
        {
            Debug.LogWarning($"{name}: spawned enemy '{enemy.name}' has no Health component.", enemy);
            return;
        }

        ArenaEncounterEnemyTracker tracker = health.GetComponent<ArenaEncounterEnemyTracker>();
        if (tracker == null)
            tracker = health.gameObject.AddComponent<ArenaEncounterEnemyTracker>();

        tracker.Initialize(this, enemy);
        aliveEnemies.Add(enemy);
    }

    public void EnemyDied(GameObject enemy)
    {
        if (enemy != null)
            aliveEnemies.Remove(enemy);

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        RemoveDestroyedEnemies();

        if (!started || completed || aliveEnemies.Count > 0)
            return;

        completed = true;
        MoveCompletionObject();
    }

    private void RemoveDestroyedEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
                aliveEnemies.RemoveAt(i);
        }
    }

    private void MoveCompletionObject()
    {
        if (objectToMove == null || moveTargetPoint == null)
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveObjectToTarget());
    }

    private IEnumerator MoveObjectToTarget()
    {
        Vector3 startPosition = objectToMove.position;
        Vector3 targetPosition = moveTargetPoint.position;
        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            objectToMove.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        objectToMove.position = targetPosition;
    }
}

[DisallowMultipleComponent]
public class ArenaEncounterEntryTrigger : MonoBehaviour
{
    private ArenaEncounter arenaEncounter;

    public void Initialize(ArenaEncounter encounter)
    {
        arenaEncounter = encounter;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (arenaEncounter != null)
            arenaEncounter.PlayerEntered(other);
    }
}

[DisallowMultipleComponent]
public class ArenaEncounterEnemyTracker : MonoBehaviour
{
    private ArenaEncounter arenaEncounter;
    private GameObject enemyRoot;
    private bool reported;

    public void Initialize(ArenaEncounter encounter, GameObject enemy)
    {
        arenaEncounter = encounter;
        enemyRoot = enemy;
        reported = false;
    }

    public void HealthChanged(HealthChangedMessage message)
    {
        if (reported || message.health > 0)
            return;

        reported = true;

        if (arenaEncounter != null)
            arenaEncounter.EnemyDied(enemyRoot);
    }
}
