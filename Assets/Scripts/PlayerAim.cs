using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerAim : MonoBehaviour
{
    private const float MinimumAimMagnitude = .001f;
    private static readonly Vector2 DefaultIndicatorSize = new Vector2(.65f, .12f);

    [Header("Scene References")]
    [SerializeField] private Transform aimIndicator;
    [SerializeField] private Camera worldCamera;

    [Header("Indicator")]
    [SerializeField, Min(0f)] private float indicatorDistance = .85f;
    [SerializeField] private SpriteRenderer indicatorRenderer;
    [SerializeField] private Vector2 fallbackIndicatorSize = new Vector2(.65f, .12f);
    [SerializeField] private Color fallbackIndicatorColor = new Color(1f, .85f, .15f);
    [SerializeField] private int fallbackSortingOrder = 3;

    private PlayerController player;
    private static Sprite fallbackIndicatorSprite;

    public Vector2 AimDirection { get; private set; } = Vector2.up;

    private void Awake()
    {
        player = GetComponent<PlayerController>();

        if (aimIndicator == null)
        {
            aimIndicator = transform.Find("AimIndicator");
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        EnsureIndicatorVisual();
    }

    private void Update()
    {
        if (worldCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector3 screenPosition = Mouse.current.position.ReadValue();
        screenPosition.z = transform.position.z - worldCamera.transform.position.z;
        Vector2 rawDirection = worldCamera.ScreenToWorldPoint(screenPosition) - transform.position;

        if (rawDirection.sqrMagnitude < MinimumAimMagnitude)
        {
            return;
        }

        AimDirection = ClampToForwardHemisphere(rawDirection.normalized, player.FacingDirection);
        UpdateIndicator();
    }

    public void Initialize(Camera cameraReference)
    {
        if (cameraReference != null)
        {
            worldCamera = cameraReference;
        }
    }

    private void EnsureIndicatorVisual()
    {
        if (aimIndicator == null)
        {
            Debug.LogWarning("PlayerAim precisa de um AimIndicator configurado.", this);
            return;
        }

        indicatorRenderer ??= aimIndicator.GetComponent<SpriteRenderer>();
        indicatorRenderer ??= aimIndicator.gameObject.AddComponent<SpriteRenderer>();

        if (indicatorRenderer.sprite != null)
        {
            return;
        }

        fallbackIndicatorSize = new Vector2(
            fallbackIndicatorSize.x > 0f ? fallbackIndicatorSize.x : DefaultIndicatorSize.x,
            fallbackIndicatorSize.y > 0f ? fallbackIndicatorSize.y : DefaultIndicatorSize.y);

        indicatorRenderer.sprite = GetFallbackIndicatorSprite();
        indicatorRenderer.drawMode = SpriteDrawMode.Tiled;
        indicatorRenderer.size = fallbackIndicatorSize;
        indicatorRenderer.color = fallbackIndicatorColor;
        indicatorRenderer.sortingOrder = fallbackSortingOrder;
    }

    private static Sprite GetFallbackIndicatorSprite()
    {
        if (fallbackIndicatorSprite != null)
        {
            return fallbackIndicatorSprite;
        }

        Texture2D texture = new Texture2D(1, 1)
        {
            filterMode = FilterMode.Point,
            name = "Aim Indicator Fallback Texture"
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackIndicatorSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(.5f, .5f),
            1f);
        fallbackIndicatorSprite.name = "Aim Indicator Fallback Sprite";
        return fallbackIndicatorSprite;
    }

    private static Vector2 ClampToForwardHemisphere(Vector2 direction, Vector2 forward)
    {
        if (Vector2.Dot(forward, direction) >= 0f)
        {
            return direction;
        }

        Vector2 side = new Vector2(-forward.y, forward.x);
        return Vector2.Dot(direction, side) >= 0f ? side : -side;
    }

    private void UpdateIndicator()
    {
        if (aimIndicator == null)
        {
            return;
        }

        aimIndicator.localPosition = AimDirection * indicatorDistance;
        aimIndicator.right = AimDirection;
    }

    private void OnValidate()
    {
        indicatorDistance = Mathf.Max(0f, indicatorDistance);
        fallbackIndicatorSize = new Vector2(
            fallbackIndicatorSize.x > 0f ? fallbackIndicatorSize.x : DefaultIndicatorSize.x,
            fallbackIndicatorSize.y > 0f ? fallbackIndicatorSize.y : DefaultIndicatorSize.y);

        if (aimIndicator == null)
        {
            aimIndicator = transform.Find("AimIndicator");
        }

        if (indicatorRenderer == null && aimIndicator != null)
        {
            indicatorRenderer = aimIndicator.GetComponent<SpriteRenderer>();
        }
    }
}
