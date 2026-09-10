using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpawnManager : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Prefer a prefab. If empty, the first existing child EnemyController is used as a template.")]
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private PlayerController target;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ArenaBounds2D arena;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Timing")]
    [SerializeField, Min(0f)] private float initialDelay = 3f;
    [SerializeField, Min(.1f)] private float initialSpawnInterval = 5f;
    [SerializeField, Min(.1f)] private float minimumSpawnInterval = 1.5f;

    [Header("Difficulty Over Time")]
    [SerializeField, Min(1f)] private float difficultyStepSeconds = 20f;
    [SerializeField, Min(0f)] private float intervalReductionPerStep = .5f;
    [SerializeField, Min(1)] private int startingMaxAlive = 2;
    [SerializeField, Min(0)] private int additionalMaxAlivePerStep = 1;
    [SerializeField, Min(1)] private int maximumAlive = 10;

    [Header("Fallback Spawn Area")]
    [SerializeField, Min(0f)] private float spawnEdgePadding = .6f;
    [SerializeField] private Vector2 fallbackHorizontalRange = new Vector2(-6.5f, 6.5f);
    [SerializeField] private float fallbackSpawnY = 6.8f;

    private readonly List<EnemyController> activeEnemies = new List<EnemyController>();
    private EnemyController spawnTemplate;
    private float startedAt;
    private float nextSpawnAt;

    public int ActiveEnemyCount => activeEnemies.Count;

    private void Awake()
    {
        ResolveReferences();
        PrepareSpawnTemplate();
        RegisterExistingEnemies();
    }

    private void Start()
    {
        if (spawnTemplate == null)
        {
            Debug.LogError(
                "EnemySpawnManager precisa de um prefab ou de um EnemyController filho para usar como modelo.",
                this);
            enabled = false;
            return;
        }

        startedAt = Time.time;
        nextSpawnAt = Time.time + initialDelay;
    }

    private void Update()
    {
        if (gameManager != null && gameManager.IsGameOver)
        {
            return;
        }

        RemoveDestroyedEnemies();

        float elapsed = Time.time - startedAt;
        int difficultyStep = Mathf.FloorToInt(elapsed / difficultyStepSeconds);
        int currentLimit = Mathf.Min(
            maximumAlive,
            startingMaxAlive + difficultyStep * additionalMaxAlivePerStep);

        if (activeEnemies.Count >= currentLimit || Time.time < nextSpawnAt)
        {
            return;
        }

        SpawnEnemy();

        float currentInterval = Mathf.Max(
            minimumSpawnInterval,
            initialSpawnInterval - difficultyStep * intervalReductionPerStep);
        nextSpawnAt = Time.time + currentInterval;
    }

    public void Initialize(PlayerController playerTarget, GameManager manager, ArenaBounds2D arenaBounds)
    {
        target = playerTarget;
        gameManager = manager;
        arena = arenaBounds;
        InitializeExistingEnemies();
    }

    private void ResolveReferences()
    {
        enemyContainer ??= transform;

        Transform sceneRoot = transform.root;
        target ??= sceneRoot.GetComponentInChildren<PlayerController>(true);
        gameManager ??= sceneRoot.GetComponentInChildren<GameManager>(true);
        arena ??= sceneRoot.GetComponentInChildren<ArenaBounds2D>(true);
        enemyPrefab ??= GetComponentInChildren<EnemyController>(true);
    }

    private void PrepareSpawnTemplate()
    {
        if (enemyPrefab == null)
        {
            return;
        }

        if (!enemyPrefab.gameObject.scene.IsValid())
        {
            spawnTemplate = enemyPrefab;
            return;
        }

        // Preserve a runtime template even if the original scene enemy is defeated.
        spawnTemplate = Instantiate(enemyPrefab, enemyContainer);
        spawnTemplate.name = $"{enemyPrefab.name}_SpawnTemplate";
        spawnTemplate.gameObject.SetActive(false);
    }

    private void RegisterExistingEnemies()
    {
        activeEnemies.Clear();
        EnemyController[] existingEnemies = enemyContainer.GetComponentsInChildren<EnemyController>(true);

        foreach (EnemyController existingEnemy in existingEnemies)
        {
            if (existingEnemy == null
                || existingEnemy == spawnTemplate
                || !existingEnemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            existingEnemy.Initialize(target, gameManager);
            activeEnemies.Add(existingEnemy);
        }
    }

    private void InitializeExistingEnemies()
    {
        foreach (EnemyController existingEnemy in activeEnemies)
        {
            if (existingEnemy != null)
            {
                existingEnemy.Initialize(target, gameManager);
            }
        }
    }

    private void RemoveDestroyedEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        EnemyController instance = Instantiate(
            spawnTemplate,
            spawnPosition,
            spawnTemplate.transform.rotation,
            enemyContainer);

        instance.name = enemyPrefab != null ? enemyPrefab.name : "Enemy";
        instance.Initialize(target, gameManager);
        instance.gameObject.SetActive(true);
        activeEnemies.Add(instance);
    }

    private Vector3 GetSpawnPosition()
    {
        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }

        if (arena != null)
        {
            Rect bounds = arena.GetInnerWorldBounds(Vector2.one * spawnEdgePadding);
            return new Vector3(Random.Range(bounds.xMin, bounds.xMax), bounds.yMax, transform.position.z);
        }

        float minimumX = Mathf.Min(fallbackHorizontalRange.x, fallbackHorizontalRange.y);
        float maximumX = Mathf.Max(fallbackHorizontalRange.x, fallbackHorizontalRange.y);
        return enemyContainer.TransformPoint(
            new Vector3(Random.Range(minimumX, maximumX), fallbackSpawnY, 0f));
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        int startIndex = Random.Range(0, spawnPoints.Length);
        for (int offset = 0; offset < spawnPoints.Length; offset++)
        {
            Transform spawnPoint = spawnPoints[(startIndex + offset) % spawnPoints.Length];
            if (spawnPoint != null)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        initialDelay = Mathf.Max(0f, initialDelay);
        initialSpawnInterval = Mathf.Max(.1f, initialSpawnInterval);
        minimumSpawnInterval = Mathf.Clamp(minimumSpawnInterval, .1f, initialSpawnInterval);
        difficultyStepSeconds = Mathf.Max(1f, difficultyStepSeconds);
        intervalReductionPerStep = Mathf.Max(0f, intervalReductionPerStep);
        startingMaxAlive = Mathf.Max(1, startingMaxAlive);
        additionalMaxAlivePerStep = Mathf.Max(0, additionalMaxAlivePerStep);
        maximumAlive = Mathf.Max(startingMaxAlive, maximumAlive);
        spawnEdgePadding = Mathf.Max(0f, spawnEdgePadding);
    }
}
