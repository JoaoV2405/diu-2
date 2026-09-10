using UnityEngine;

public static class Collision2DUtility
{
    public const string BallLayerName = "Ball";
    public const string EnemyLayerName = "Enemy";
    public const string PlayerLayerName = "Player";
    public const string WallLayerName = "Wall";
    public const string HitAreaLayerName = "HitArea";

    public static LayerMask MaskFor(params string[] layerNames)
    {
        int mask = 0;

        foreach (string layerName in layerNames)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) mask |= 1 << layer;
        }

        return mask;
    }

    public static bool Contains(LayerMask mask, int layer)
    {
        return layer >= 0 && (mask.value & (1 << layer)) != 0;
    }

    public static void SetLayer(GameObject target, string layerName)
    {
        if (target == null)
        {
            return;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"A layer '{layerName}' não existe. Verifique Project Settings > Tags and Layers.", target);
            return;
        }

        if (target.layer != layer)
        {
            target.layer = layer;
        }
    }

    public static void DrawCollider(Collider2D collider, Color color)
    {
        if (collider == null || !collider.enabled) return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.color = color;
        Gizmos.matrix = collider.transform.localToWorldMatrix;

        if (collider is CircleCollider2D circle)
        {
            Gizmos.DrawWireSphere(circle.offset, circle.radius);
        }
        else if (collider is BoxCollider2D box)
        {
            Gizmos.DrawWireCube(box.offset, box.size);
        }
        else if (collider is CapsuleCollider2D capsule)
        {
            DrawCapsule(capsule);
        }
        else
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private static void DrawCapsule(CapsuleCollider2D capsule)
    {
        Vector2 size = capsule.size;
        Vector2 center = capsule.offset;
        bool vertical = capsule.direction == CapsuleDirection2D.Vertical;
        float radius = (vertical ? size.x : size.y) * .5f;
        float halfStraight = Mathf.Max(0f, (vertical ? size.y : size.x) * .5f - radius);
        Vector2 axis = vertical ? Vector2.up : Vector2.right;
        Vector2 side = vertical ? Vector2.right : Vector2.up;
        Vector2 endA = center + axis * halfStraight;
        Vector2 endB = center - axis * halfStraight;

        Gizmos.DrawWireSphere(endA, radius);
        Gizmos.DrawWireSphere(endB, radius);
        Gizmos.DrawLine(endA + side * radius, endB + side * radius);
        Gizmos.DrawLine(endA - side * radius, endB - side * radius);
    }
}
