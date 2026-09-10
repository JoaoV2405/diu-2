using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0)] private int currentExperience;
    [SerializeField, Min(1)] private int experienceToNextLevel = 3;

    [Header("Dependencies")]
    [SerializeField] private PlayerController player;

    public event Action ProgressChanged;
    public event Action GameOverStarted;

    public bool IsGameOver { get; private set; }
    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => experienceToNextLevel;

    public void Initialize(PlayerController controlledPlayer)
    {
        player = controlledPlayer;
    }

    public void AddExperience(int amount)
    {
        if (IsGameOver || amount <= 0)
        {
            return;
        }

        currentExperience += amount;

        while (currentExperience >= experienceToNextLevel)
        {
            currentExperience -= experienceToNextLevel;
            currentLevel++;
            experienceToNextLevel = currentLevel * 3;
            ApplyAutomaticUpgrade();
        }

        ProgressChanged?.Invoke();
    }

    public void GameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        Time.timeScale = 0f;
        GameOverStarted?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ApplyAutomaticUpgrade()
    {
        if (player == null)
        {
            Debug.LogWarning("Não foi possível aplicar o upgrade: Player não configurado.", this);
            return;
        }

        switch ((currentLevel - 2) % 3)
        {
            case 0:
                player.IncreaseMaxHealth(1);
                break;
            case 1:
                player.ImproveDash();
                break;
            default:
                player.ImproveHit();
                break;
        }
    }

    private void OnValidate()
    {
        currentLevel = Mathf.Max(1, currentLevel);
        currentExperience = Mathf.Max(0, currentExperience);
        experienceToNextLevel = Mathf.Max(1, experienceToNextLevel);
    }
}
