using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArenaBounds2D : MonoBehaviour
{
    private const string TopWallName = "Wall_Top";
    private const string BottomWallName = "Wall_Bottom";
    private const string LeftWallName = "Wall_Left";
    private const string RightWallName = "Wall_Right";

    private static readonly Vector2 DefaultSize = new Vector2(5f, 16f);
    private const float DefaultWallThickness = .4f;

    [Header("Fallback Dimensions")]
    [Tooltip("Used only when the four wall colliders are not assigned or found.")]
    [SerializeField] private Vector2 size = new Vector2(5f, 16f);
    [Tooltip("Used only with the fallback dimensions.")]
    [SerializeField, Min(0f)] private float wallThickness = .4f;

    [Header("Scene Walls")]
    [Tooltip("When all four walls are available, their Collider2D bounds define the arena.")]
    [SerializeField] private Collider2D topWall;
    [SerializeField] private Collider2D bottomWall;
    [SerializeField] private Collider2D leftWall;
    [SerializeField] private Collider2D rightWall;

    [Header("Debug")]
    [SerializeField] private Color gizmoColor = new Color(.3f, .55f, 1f, .8f);

    public Vector2 Size => size;
    public float WallThickness => wallThickness;

    private void Awake()
    {
        ApplyFallbackDefaults();
        ResolveWallReferences();
    }

    public Rect GetInnerWorldBounds(Vector2 bodyExtents)
    {
        if (TryGetBoundsFromSceneWalls(bodyExtents, out Rect sceneBounds))
        {
            return sceneBounds;
        }

        Vector2 worldScale = Abs(transform.lossyScale);
        Vector2 worldSize = Vector2.Scale(size, worldScale);
        Vector2 wallInset = worldScale * (wallThickness * .5f);
        Vector2 inset = wallInset + bodyExtents;
        Vector2 halfSize = Vector2.Max(worldSize * .5f - inset, Vector2.zero);
        Vector2 center = transform.position;

        return Rect.MinMaxRect(
            center.x - halfSize.x,
            center.y - halfSize.y,
            center.x + halfSize.x,
            center.y + halfSize.y);
    }

    private bool TryGetBoundsFromSceneWalls(Vector2 bodyExtents, out Rect bounds)
    {
        ResolveWallReferences();

        if (topWall == null || bottomWall == null || leftWall == null || rightWall == null)
        {
            bounds = default;
            return false;
        }

        float xMin = leftWall.bounds.max.x + bodyExtents.x;
        float xMax = rightWall.bounds.min.x - bodyExtents.x;
        float yMin = bottomWall.bounds.max.y + bodyExtents.y;
        float yMax = topWall.bounds.min.y - bodyExtents.y;

        if (xMin > xMax || yMin > yMax)
        {
            bounds = default;
            return false;
        }

        bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return true;
    }

    private void ResolveWallReferences()
    {
        topWall ??= FindWallCollider(TopWallName);
        bottomWall ??= FindWallCollider(BottomWallName);
        leftWall ??= FindWallCollider(LeftWallName);
        rightWall ??= FindWallCollider(RightWallName);
    }

    private Collider2D FindWallCollider(string wallName)
    {
        Transform wall = transform.Find(wallName);
        return wall != null ? wall.GetComponent<Collider2D>() : null;
    }

    private void ApplyFallbackDefaults()
    {
        size = new Vector2(
            size.x > 0f ? size.x : DefaultSize.x,
            size.y > 0f ? size.y : DefaultSize.y);

        if (wallThickness < 0f)
        {
            wallThickness = DefaultWallThickness;
        }

        size = new Vector2(Mathf.Max(.1f, size.x), Mathf.Max(.1f, size.y));
        wallThickness = Mathf.Clamp(wallThickness, 0f, Mathf.Min(size.x, size.y));
    }

    private static Vector2 Abs(Vector3 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }

    private void OnDrawGizmosSelected()
    {
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.color = gizmoColor;

        if (TryGetBoundsFromSceneWalls(Vector2.zero, out Rect sceneBounds))
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(sceneBounds.center, sceneBounds.size);
        }
        else
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private void OnValidate()
    {
        ApplyFallbackDefaults();
        ResolveWallReferences();
    }
}
