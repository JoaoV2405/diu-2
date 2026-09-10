using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class EnemyProjectile : MonoBehaviour
{
    private const float MinimumDirectionMagnitude = .001f;
    private const int RuntimeSpriteResolution = 16;

    [Header("Projectile")]
    [SerializeField, Min(0f)] private float maximumLifetime = 10f;
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private LayerMask destroyOnContactLayers;

    private static Sprite runtimeSprite;

    private Rigidbody2D body;
    private CircleCollider2D projectileCollider;
    private int damage;
    private float destroyAt;

    private void Awake()
    {
        CacheComponents();
        ConfigurePhysics();
        ConfigureDefaults();
    }

    private void Update()
    {
        if (Time.time >= destroyAt)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Collision2DUtility.Contains(playerLayers, other.gameObject.layer))
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (Collision2DUtility.Contains(destroyOnContactLayers, other.gameObject.layer))
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(Vector2 direction, float speed, int damageAmount, GameObject owner)
    {
        CacheComponents();
        ConfigurePhysics();
        ConfigureDefaults();

        damage = Mathf.Max(1, damageAmount);
        destroyAt = Time.time + maximumLifetime;

        Vector2 travelDirection = direction.sqrMagnitude >= MinimumDirectionMagnitude
            ? direction.normalized
            : Vector2.down;
        body.linearVelocity = travelDirection * Mathf.Max(0f, speed);

        if (owner == null)
        {
            return;
        }

        Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D ownerCollider in ownerColliders)
        {
            Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
        }
    }

    public static EnemyProjectile CreateRuntime(Vector2 position)
    {
        GameObject projectileObject = new GameObject("EnemyProjectile");
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * .3f;

        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetOrCreateRuntimeSprite();
        renderer.color = new Color(1f, .25f, .1f, 1f);
        renderer.sortingOrder = 1;

        CircleCollider2D circle = projectileObject.AddComponent<CircleCollider2D>();
        circle.radius = .5f;
        projectileObject.AddComponent<Rigidbody2D>();
        return projectileObject.AddComponent<EnemyProjectile>();
    }

    private void CacheComponents()
    {
        body ??= GetComponent<Rigidbody2D>();
        projectileCollider ??= GetComponent<CircleCollider2D>();
    }

    private void ConfigurePhysics()
    {
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        projectileCollider.isTrigger = true;
    }

    private void ConfigureDefaults()
    {
        if (playerLayers.value == 0)
        {
            playerLayers = Collision2DUtility.MaskFor(Collision2DUtility.PlayerLayerName);
        }

        if (destroyOnContactLayers.value == 0)
        {
            destroyOnContactLayers = Collision2DUtility.MaskFor(
                Collision2DUtility.BallLayerName,
                Collision2DUtility.WallLayerName);
        }
    }

    private static Sprite GetOrCreateRuntimeSprite()
    {
        if (runtimeSprite != null)
        {
            return runtimeSprite;
        }

        Texture2D texture = new Texture2D(
            RuntimeSpriteResolution,
            RuntimeSpriteResolution,
            TextureFormat.RGBA32,
            false);
        texture.name = "RuntimeEnemyProjectile";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[RuntimeSpriteResolution * RuntimeSpriteResolution];
        Vector2 center = Vector2.one * (RuntimeSpriteResolution - 1) * .5f;
        float radiusSquared = RuntimeSpriteResolution * RuntimeSpriteResolution * .25f;

        for (int y = 0; y < RuntimeSpriteResolution; y++)
        {
            for (int x = 0; x < RuntimeSpriteResolution; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                pixels[y * RuntimeSpriteResolution + x] = offset.sqrMagnitude <= radiusSquared
                    ? Color.white
                    : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, RuntimeSpriteResolution, RuntimeSpriteResolution),
            Vector2.one * .5f,
            RuntimeSpriteResolution);
        runtimeSprite.name = "RuntimeEnemyProjectile";
        runtimeSprite.hideFlags = HideFlags.HideAndDontSave;
        return runtimeSprite;
    }

    private void OnValidate()
    {
        maximumLifetime = Mathf.Max(0f, maximumLifetime);
        ConfigureDefaults();
    }
}
