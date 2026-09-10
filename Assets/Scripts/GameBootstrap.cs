using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameBootstrap : MonoBehaviour
{
    private const string ArenaPath = "Arena";
    private const string PlayerPath = "Player";
    private const string BallPath = "Ball";
    private const string EnemyPath = "Enemies/Enemy_Type1";

    [Header("Scene References")]
    [Tooltip("Optional. Uses Camera.main when it is not assigned.")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ArenaBounds2D arena;
    [SerializeField] private PlayerController player;
    [SerializeField] private BallController ball;
    [SerializeField] private EnemyController enemy;

    [Header("Optional Services")]
    [SerializeField] private PlayerAim playerAim;
    [SerializeField] private ArenaContainment2D ballContainment;
    [SerializeField] private EnemySpawnManager enemySpawnManager;
    [SerializeField] private DebugGameHud debugGameHud;

    private void Awake()
    {
        ResolveSceneReferences();

        if (!ValidateRequiredReferences())
        {
            enabled = false;
            return;
        }

        // The scene is the source of truth for transforms, colliders, physics and
        // visual values. The bootstrap only connects the existing components.
        player.Initialize(gameManager);
        gameManager.Initialize(player);

        if (playerAim != null)
        {
            playerAim.Initialize(gameplayCamera);
        }

        if (ballContainment != null)
        {
            ballContainment.Initialize(arena);
        }

        if (enemy != null)
        {
            enemy.Initialize(player, gameManager);
        }

        if (enemySpawnManager != null)
        {
            enemySpawnManager.Initialize(player, gameManager, arena);
        }

        if (debugGameHud != null)
        {
            debugGameHud.Initialize(gameManager, player, gameplayCamera);
        }
    }

    private void ResolveSceneReferences()
    {
        gameplayCamera ??= Camera.main;
        gameManager ??= GetComponentInChildren<GameManager>(true);

        arena ??= FindExistingComponent<ArenaBounds2D>(ArenaPath);
        player ??= FindExistingComponent<PlayerController>(PlayerPath);
        ball ??= FindExistingComponent<BallController>(BallPath);
        enemy ??= FindExistingComponent<EnemyController>(EnemyPath);
        enemySpawnManager ??= GetComponentInChildren<EnemySpawnManager>(true);

        if (player != null)
        {
            playerAim ??= player.GetComponent<PlayerAim>();
        }

        if (ball != null)
        {
            ballContainment ??= ball.GetComponent<ArenaContainment2D>();
        }

        if (gameManager != null)
        {
            debugGameHud ??= gameManager.GetComponent<DebugGameHud>();
        }
    }

    private bool ValidateRequiredReferences()
    {
        List<string> missingReferences = new List<string>();

        AddIfMissing(gameManager, nameof(gameManager), missingReferences);
        AddIfMissing(arena, nameof(arena), missingReferences);
        AddIfMissing(player, nameof(player), missingReferences);
        AddIfMissing(ball, nameof(ball), missingReferences);

        if (missingReferences.Count == 0)
        {
            return true;
        }

        Debug.LogError(
            $"GameBootstrap encontrou referências obrigatórias ausentes: {string.Join(", ", missingReferences)}. " +
            "Atribua os componentes existentes pelo Inspector. Nenhum objeto será criado automaticamente.",
            this);
        return false;
    }

    private T FindExistingComponent<T>(string fallbackPath) where T : Component
    {
        Transform target = transform.Find(fallbackPath);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static void AddIfMissing(
        Object reference,
        string referenceName,
        ICollection<string> missingReferences)
    {
        if (reference == null)
        {
            missingReferences.Add(referenceName);
        }
    }
}
