using UnityEngine;

[DisallowMultipleComponent]
public sealed class WallMarker : MonoBehaviour
{
    [SerializeField] private Color colliderGizmoColor = new Color(.3f, .55f, 1f, .8f);

    private void Awake()
    {
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.WallLayerName);
    }

    private void OnDrawGizmosSelected()
    {
        Collision2DUtility.DrawCollider(GetComponent<Collider2D>(), colliderGizmoColor);
    }

    private void OnValidate()
    {
        Collision2DUtility.SetLayer(gameObject, Collision2DUtility.WallLayerName);
    }
}
