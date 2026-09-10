using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAim))]
public sealed class PlayerHitController : MonoBehaviour
{
    private const string AttackTrigger = "Attack";

    [Header("Hit")]
    [SerializeField] private Collider2D hitArea;
    [SerializeField] private Animator attackAnimator;
    private Transform hitAreaTransform;
    [SerializeField, Min(0f)] private float hitAreaDistance = .8f;
    [SerializeField, Min(0f)] private float hitWindow = .18f;
    [SerializeField, Min(1f)] private float hitSpeedMultiplier = 1.2f;
    [SerializeField] private LayerMask ballLayers;

    [Header("Debug")]
    [SerializeField] private Color hitAreaGizmoColor = new Color(1f, .85f, .15f, .9f);

    private PlayerAim aim;

    private readonly HashSet<BallController> ballsHitThisClick = new HashSet<BallController>();
    private float attackEndsAt;

    public bool IsAttacking => Time.time < attackEndsAt;

    private void Awake()
    {
        aim = GetComponent<PlayerAim>();

        ResolveHitArea();
        ResolveAttackAnimator();
        ConfigureDefaults();
    }

    private void Update()
    {
        UpdateHitAreaPosition();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (attackAnimator != null)
            {
                attackAnimator.SetTrigger("AttackTrigger");
            }

            ballsHitThisClick.Clear();
            attackEndsAt = Time.time + hitWindow;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsAttacking || !Collision2DUtility.Contains(ballLayers, other.gameObject.layer))
        {
            return;
        }

        BallController ball = other.GetComponentInParent<BallController>();
        if (ball != null && ballsHitThisClick.Add(ball))
        {
            ball.RedirectAndAccelerate(aim.AimDirection, hitSpeedMultiplier);
        }
    }

    public void ImproveHit()
    {
        hitSpeedMultiplier += .1f;

        if (hitArea is CircleCollider2D circle)
        {
            circle.radius += .08f;
        }
    }

    private void ResolveHitArea()
    {
        if (hitArea == null)
        {
            Transform area = transform.Find("HitArea");

            if (area != null)
            {
                hitArea = area.GetComponent<Collider2D>();
            }
        }

        if (hitArea != null)
        {
            hitAreaTransform = hitArea.transform;
        }
    }

    private void ResolveAttackAnimator()
    {
        if (attackAnimator == null)
        {
            attackAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void ConfigureDefaults()
    {
        if (hitArea != null)
        {
            hitArea.isTrigger = true;
            Collision2DUtility.SetLayer(hitArea.gameObject, Collision2DUtility.HitAreaLayerName);
        }

        if (ballLayers.value == 0)
        {
            ballLayers = Collision2DUtility.MaskFor(Collision2DUtility.BallLayerName);
        }
    }

    private void UpdateHitAreaPosition()
    {
        if (hitAreaTransform != null)
        {
            hitAreaTransform.localPosition = aim.AimDirection * hitAreaDistance;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitArea == null)
        {
            ResolveHitArea();
        }

        Collision2DUtility.DrawCollider(hitArea, hitAreaGizmoColor);
    }

    private void OnValidate()
    {
        hitAreaDistance = Mathf.Max(0f, hitAreaDistance);
        hitWindow = Mathf.Max(0f, hitWindow);
        hitSpeedMultiplier = Mathf.Max(1f, hitSpeedMultiplier);
        ResolveHitArea();
        ResolveAttackAnimator();
        ConfigureDefaults();
    }
}
