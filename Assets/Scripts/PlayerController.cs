using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController : MonoBehaviour, IDamageable
{
    private const float MinimumInputMagnitude = .001f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4.5f;

    [Header("State")]
    [SerializeField] private bool facingLeft = false;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float dashSpeed = 11f;
    [SerializeField, Min(0f)] private float dashDuration = .18f;
    [SerializeField, Min(0f)] private float dashCooldown = 1f;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(0)] private int currentHealth = 5;

    [Header("Dependencies")]
    [SerializeField] private GameManager gameManager;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private Rigidbody2D body;
    private PlayerHitController hitController;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndsAt;
    private float dashReadyAt;

    public event Action<int, int> HealthChanged;

    public bool IsDashing => Time.time < dashEndsAt;
    public Vector2 FacingDirection => Vector2.up;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.PlayerLayerName);
        body = GetComponent<Rigidbody2D>();
        hitController = GetComponent<PlayerHitController>();
        spriteRenderer = animator.GetComponent<SpriteRenderer>();


        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
    }

    private void Update()
    {
        if (gameManager != null && gameManager.IsGameOver)
        {
            moveInput = Vector2.zero;
            return;
        }

        ReadMovementInput();
        TryStartDash();
        HandleFlip();
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = IsDashing
            ? dashDirection * dashSpeed
            : moveInput * moveSpeed;
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDashing || (gameManager != null && gameManager.IsGameOver))
        {
            return;
        }
        ShowDamageFeedback();
        currentHealth = Mathf.Max(0, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            gameManager?.GameOver();
        }
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        maxHealth += amount;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ImproveDash()
    {
        dashCooldown = Mathf.Max(.25f, dashCooldown - .15f);
    }

    public void ImproveHit()
    {
        if (hitController == null)
        {
            hitController = GetComponent<PlayerHitController>();
        }

        hitController?.ImproveHit();
    }

    private void ReadMovementInput()
    {
        moveInput = Vector2.zero;
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveInput.x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveInput.x += 1f;
        }

        moveInput.x = Mathf.Clamp(moveInput.x, -1f, 1f);
        if (moveInput.sqrMagnitude > MinimumInputMagnitude)
        {
            lastMoveDirection = moveInput;
        }
    }
        private void HandleAnimation()
        {
            animator.SetFloat("xVelocity", body.linearVelocity.x);
            animator.SetFloat("yVelocity", body.linearVelocity.y);
            animator.SetBool("isDashing", IsDashing);

        }
    private void HandleFlip()
        {
            if (body.linearVelocity.x > 0 && facingLeft)
            {
                Flip();
            }
            else if (body.linearVelocity.x < 0 && !facingLeft)
            {
                Flip();
            }
        }

    private void Flip()
    {
        facingLeft = !facingLeft;
        spriteRenderer.flipX = facingLeft;
    }
    private void TryStartDash()
    {
        if (Keyboard.current == null || Time.time < dashReadyAt)
        {
            return;
        }

        bool dashPressed = Keyboard.current.leftShiftKey.wasPressedThisFrame
            || Keyboard.current.spaceKey.wasPressedThisFrame;

        if (!dashPressed)
        {
            return;
        }
        dashDirection = moveInput.sqrMagnitude > MinimumInputMagnitude ? moveInput : lastMoveDirection;
        dashEndsAt = Time.time + dashDuration;
        dashReadyAt = dashEndsAt + dashCooldown;
    }
    
     private void Attack()
    {
       animator.SetTrigger("attack");
    }

    private void OnDrawGizmosSelected()
    {
        Collision2DUtility.DrawCollider(GetComponent<Collider2D>(), new Color(.2f, .75f, 1f, .9f));
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.PlayerLayerName);
    }
    private void ShowDamageFeedback()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = Color.red;
        CancelInvoke(nameof(RestoreColor));
        Invoke(nameof(RestoreColor), 0.2f);
    }

    private void RestoreColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }
}
