using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class BallController : MonoBehaviour
{
    private const float MinimumDirectionMagnitude = .001f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float baseSpeed = 5f;
    [SerializeField, Min(0f)] private float maxSpeed = 12f;
    [SerializeField] private Vector2 initialDirection = new Vector2(.8f, .6f);
    [SerializeField, Min(0f)] private float redirectCooldown = .1f;

    [Header("Damage")]
    [SerializeField, Min(1)] private int damage = 1;
    [FormerlySerializedAs("enemyLayers")]
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField, Min(0f)] private float damageCooldown = .35f;

    [Header("Collision Response")]
    [Tooltip("Physical contacts with these layers make the ball ricochet. Player is intentionally excluded.")]
    [SerializeField] private LayerMask reboundLayers;
    [SerializeField] private LayerMask playerLayers;
    [SerializeField, Min(0f)] private float playerPassThroughDuration = .3f;

    [Header("Debug")]
    [SerializeField] private Color colliderGizmoColor = new Color(1f, .25f, .2f, .9f);

    private Rigidbody2D body;
    private CircleCollider2D ballCollider;
    private Vector2 travelDirection;
    private float currentSpeed;
    private float nextDamageAt;
    private float nextRedirectAt;

    public float CurrentSpeed => currentSpeed;
    public Vector2 TravelDirection => travelDirection;

    private void Awake()
    {
        CacheComponents();
        ConfigurePhysics();
        ConfigureDefaults();

        currentSpeed = Mathf.Min(baseSpeed, maxSpeed);
        travelDirection = NormalizeOrFallback(initialDirection, Vector2.up);
    }

    private void Start()
    {
        Redirect(travelDirection);
    }

    private void FixedUpdate()
    {
        // travelDirection is authoritative. This prevents Unity's contact solver
        // from turning a normal Player collision into an automatic hit.
        body.linearVelocity = travelDirection * currentSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamage(collision.collider);

        if (Collision2DUtility.Contains(playerLayers, collision.gameObject.layer))
        {
            StartCoroutine(PassThroughPlayerTemporarily(collision.collider));
            return;
        }

        if (collision.contactCount > 0
            && Collision2DUtility.Contains(reboundLayers, collision.gameObject.layer))
        {
            Reflect(collision.GetContact(0).normal);
        }
    }

    private IEnumerator PassThroughPlayerTemporarily(Collider2D playerCollider)
    {
        if (playerCollider == null)
        {
            yield break;
        }

        Physics2D.IgnoreCollision(ballCollider, playerCollider, true);
        yield return new WaitForSeconds(playerPassThroughDuration);

        if (ballCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(ballCollider, playerCollider, false);
        }
    }

    public void Redirect(Vector2 direction)
    {
        if (direction.sqrMagnitude < MinimumDirectionMagnitude)
        {
            return;
        }

        travelDirection = direction.normalized;
        body.linearVelocity = travelDirection * currentSpeed;
    }

    public void Reflect(Vector2 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude < MinimumDirectionMagnitude)
        {
            return;
        }

        // A direção armazenada representa o movimento anterior à resolução do solver.
        // Isso evita refletir uma velocidade já achatada contra a parede.
        Redirect(Vector2.Reflect(travelDirection, surfaceNormal.normalized));
    }

    public void RedirectAndAccelerate(Vector2 direction, float multiplier)
    {
        if (Time.time < nextRedirectAt || multiplier <= 0f)
        {
            return;
        }

        nextRedirectAt = Time.time + redirectCooldown;
        currentSpeed = Mathf.Min(currentSpeed * Mathf.Max(1f, multiplier), maxSpeed);
        Redirect(direction);
    }

    public void ResetBall(Vector2 position, Vector2 direction)
    {
        body.position = position;
        currentSpeed = Mathf.Min(baseSpeed, maxSpeed);
        Redirect(direction);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (Time.time < nextDamageAt || !Collision2DUtility.Contains(damageableLayers, other.gameObject.layer))
        {
            return;
        }

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null)
        {
            return;
        }

        target.TakeDamage(damage);
        nextDamageAt = Time.time + damageCooldown;
    }

    private void CacheComponents()
    {
        body = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<CircleCollider2D>();
    }

    private void ConfigurePhysics()
    {
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void ConfigureDefaults()
    {
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.BallLayerName);

        if (damageableLayers.value == 0)
        {
            damageableLayers = Collision2DUtility.MaskFor(
                Collision2DUtility.EnemyLayerName,
                Collision2DUtility.PlayerLayerName);
        }

        if (reboundLayers.value == 0)
        {
            reboundLayers = Collision2DUtility.MaskFor(
                Collision2DUtility.WallLayerName,
                Collision2DUtility.EnemyLayerName);
        }

        if (playerLayers.value == 0)
        {
            playerLayers = Collision2DUtility.MaskFor(Collision2DUtility.PlayerLayerName);
        }
    }

    private static Vector2 NormalizeOrFallback(Vector2 value, Vector2 fallback)
    {
        return value.sqrMagnitude >= MinimumDirectionMagnitude ? value.normalized : fallback;
    }

    private void OnDrawGizmosSelected()
    {
        if (ballCollider == null)
        {
            ballCollider = GetComponent<CircleCollider2D>();
        }

        Collision2DUtility.DrawCollider(ballCollider, colliderGizmoColor);
    }

    private void OnValidate()
    {
        baseSpeed = Mathf.Max(0f, baseSpeed);
        maxSpeed = Mathf.Max(baseSpeed, maxSpeed);
        damage = Mathf.Max(1, damage);
        redirectCooldown = Mathf.Max(0f, redirectCooldown);
        damageCooldown = Mathf.Max(0f, damageCooldown);
        playerPassThroughDuration = Mathf.Max(0f, playerPassThroughDuration);
        ConfigureDefaults();
    }
}
