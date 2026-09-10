using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public sealed class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.6f;

    [Header("Combat")]
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0)] private int experienceReward = 1;
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0f)] private float contactDamageCooldown = 1f;
    [SerializeField] private LayerMask playerLayers;

    [Header("Ranged Attack")]
    [Tooltip("Vertical distance from the Player at which the enemy stops and starts shooting.")]
    [SerializeField, Min(0f)] private float verticalAttackDistance = 3f;
    [SerializeField, Min(.01f)] private float projectileDelay = 1f;
    [SerializeField, Min(0f)] private float projectileSpeed = 5f;
    [SerializeField, Min(1)] private int projectileDamage = 1;
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Dependencies")]
    [SerializeField] private PlayerController target;
    [SerializeField] private GameManager gameManager;

    [Header("Feedback")]
    [SerializeField, Min(0f)] private float damageFlashDuration = .1f;
    [SerializeField] private Color damageFlashColor = Color.blue;

    [Header("Debug")]
    [SerializeField] private Color colliderGizmoColor = new Color(1f, .2f, .75f, .9f);
    [SerializeField] private Color attackDistanceGizmoColor = new Color(1f, .65f, .1f, .9f);

    private int currentHealth;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float nextContactDamageAt;
    private float nextProjectileAt;
    private bool isInAttackRange;
    private Animator animator;


    public int CurrentHealth => currentHealth;
    public bool IsInAttackRange => isInAttackRange;

    private void Awake()
    {
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.EnemyLayerName);
        body = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = animator.GetComponent<SpriteRenderer>();


        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        currentHealth = maxHealth;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (playerLayers.value == 0)
        {
            playerLayers = Collision2DUtility.MaskFor(Collision2DUtility.PlayerLayerName);
        }
    }

    private void FixedUpdate()
    {
        if (target == null || (gameManager != null && gameManager.IsGameOver))
        {
            body.linearVelocity = Vector2.zero;
            isInAttackRange = false;
            return;
        }

        float verticalDistance = Mathf.Abs(target.transform.position.y - transform.position.y);
        isInAttackRange = verticalDistance <= verticalAttackDistance;

        if (isInAttackRange)
        {
            body.linearVelocity = Vector2.zero;
            TryShootProjectile();
            return;
        }

        // Leaving the range allows an immediate shot when the enemy reaches it again.
        nextProjectileAt = 0f;

        float verticalDirection = Mathf.Sign(target.transform.position.y - transform.position.y);
        float horizontalDirection = Mathf.Sign(target.transform.position.x - transform.position.x);
        body.linearVelocity = new Vector2(horizontalDirection * moveSpeed, verticalDirection * moveSpeed);
    }

    private void TryShootProjectile()
    {
        if (Time.time < nextProjectileAt)
        {
            return;
        }

        Vector2 spawnPosition = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
        Vector2 direction = (Vector2)target.transform.position - spawnPosition;

        EnemyProjectile projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPosition, Quaternion.identity)
            : EnemyProjectile.CreateRuntime(spawnPosition);

        projectile.gameObject.SetActive(true);
        animator.SetTrigger("Attack");
        projectile.Initialize(direction, projectileSpeed, projectileDamage, gameObject);
        nextProjectileAt = Time.time + projectileDelay;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextContactDamageAt
            || !Collision2DUtility.Contains(playerLayers, collision.gameObject.layer))
        {
            return;
        }

        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        damageable.TakeDamage(contactDamage);
        nextContactDamageAt = Time.time + contactDamageCooldown;
    }

    public void Initialize(PlayerController playerTarget, GameManager manager)
    {
        target = playerTarget;
        gameManager = manager;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        ShowDamageFeedback();

        if (currentHealth > 0)
        {
            return;
        }

        gameManager?.AddExperience(experienceReward);
        Destroy(gameObject);
    }

    private void ShowDamageFeedback()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = damageFlashColor;
        CancelInvoke(nameof(RestoreColor));
        Invoke(nameof(RestoreColor), damageFlashDuration);
    }

    private void RestoreColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D enemyCollider = GetComponent<Collider2D>();
        Collision2DUtility.DrawCollider(enemyCollider, colliderGizmoColor);

        Color previousColor = Gizmos.color;
        Gizmos.color = attackDistanceGizmoColor;

        Vector3 center = transform.position;
        float halfWidth = enemyCollider != null
            ? Mathf.Max(.5f, enemyCollider.bounds.extents.x)
            : .5f;
        Vector3 upperLimit = center + Vector3.up * verticalAttackDistance;
        Vector3 lowerLimit = center + Vector3.down * verticalAttackDistance;
        Vector3 horizontalOffset = Vector3.right * halfWidth;

        Gizmos.DrawLine(lowerLimit, upperLimit);
        Gizmos.DrawLine(upperLimit - horizontalOffset, upperLimit + horizontalOffset);
        Gizmos.DrawLine(lowerLimit - horizontalOffset, lowerLimit + horizontalOffset);
        Gizmos.color = previousColor;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        maxHealth = Mathf.Max(1, maxHealth);
        experienceReward = Mathf.Max(0, experienceReward);
        contactDamage = Mathf.Max(1, contactDamage);
        contactDamageCooldown = Mathf.Max(0f, contactDamageCooldown);
        verticalAttackDistance = Mathf.Max(0f, verticalAttackDistance);
        projectileDelay = Mathf.Max(.01f, projectileDelay);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileDamage = Mathf.Max(1, projectileDamage);
        damageFlashDuration = Mathf.Max(0f, damageFlashDuration);
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.EnemyLayerName);

        if (playerLayers.value == 0)
        {
            playerLayers = Collision2DUtility.MaskFor(Collision2DUtility.PlayerLayerName);
        }
    }
}
