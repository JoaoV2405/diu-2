using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GameManager))]
public sealed class DebugGameHud : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera gameplayCamera;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    public void Initialize(GameManager manager, PlayerController controlledPlayer, Camera cameraReference)
    {
        gameManager = manager;
        player = controlledPlayer;
        gameplayCamera = cameraReference;
    }

    private void OnGUI()
    {
        if (gameManager == null)
        {
            return;
        }

        GUI.Label(
            new Rect(16f, 12f, 360f, 28f),
            $"Nível {gameManager.CurrentLevel}   XP {gameManager.CurrentExperience}/{gameManager.ExperienceToNextLevel}");

        DrawPlayerHealth();

        if (!gameManager.IsGameOver)
        {
            return;
        }

        GUI.Box(new Rect(Screen.width * .5f - 150f, Screen.height * .5f - 60f, 300f, 120f), "GAME OVER");
        if (GUI.Button(new Rect(Screen.width * .5f - 90f, Screen.height * .5f, 180f, 32f), "Reiniciar"))
        {
            gameManager.RestartGame();
        }
    }

    private void DrawPlayerHealth()
    {
        if (player == null || gameplayCamera == null)
        {
            return;
        }

        Vector3 screenPosition = gameplayCamera.WorldToScreenPoint(player.transform.position);
        GUI.Label(
            new Rect(screenPosition.x - 40f, Screen.height - screenPosition.y - 42f, 120f, 24f),
            $"HP {player.CurrentHealth}/{player.MaxHealth}");
    }
}
