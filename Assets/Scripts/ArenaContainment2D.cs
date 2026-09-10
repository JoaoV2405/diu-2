using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(BallController))]
public sealed class ArenaContainment2D : MonoBehaviour
{
    [SerializeField] private ArenaBounds2D arena;

    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private BallController ball;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        ball = GetComponent<BallController>();
    }

    private void FixedUpdate()
    {
        if (arena == null)
        {
            return;
        }

        Rect bounds = arena.GetInnerWorldBounds(bodyCollider.bounds.extents);
        Vector2 position = body.position;
        Vector2 colliderCenterOffset = (Vector2)bodyCollider.bounds.center - position;
        Vector2 colliderCenter = position + colliderCenterOffset;
        Vector2 direction = ball.TravelDirection;
        bool wasCorrected = false;

        if (colliderCenter.x < bounds.xMin)
        {
            position.x = bounds.xMin - colliderCenterOffset.x;
            direction.x = Mathf.Abs(direction.x);
            wasCorrected = true;
        }
        else if (colliderCenter.x > bounds.xMax)
        {
            position.x = bounds.xMax - colliderCenterOffset.x;
            direction.x = -Mathf.Abs(direction.x);
            wasCorrected = true;
        }

        if (colliderCenter.y < bounds.yMin)
        {
            position.y = bounds.yMin - colliderCenterOffset.y;
            direction.y = Mathf.Abs(direction.y);
            wasCorrected = true;
        }
        else if (colliderCenter.y > bounds.yMax)
        {
            position.y = bounds.yMax - colliderCenterOffset.y;
            direction.y = -Mathf.Abs(direction.y);
            wasCorrected = true;
        }

        if (!wasCorrected)
        {
            return;
        }

        body.position = position;
        ball.Redirect(direction);
    }

    public void Initialize(ArenaBounds2D arenaBounds)
    {
        if (arenaBounds == null)
        {
            Debug.LogError("ArenaContainment2D requer uma ArenaBounds2D válida.", this);
            return;
        }

        arena = arenaBounds;
    }
}
