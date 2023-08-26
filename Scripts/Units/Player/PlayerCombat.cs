using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : CombatBase
{
    [SerializeField] private PlayerData _data;
    [SerializeField] private CapsuleCollider2D _standingCollider;
    private TarodevController.PlayerController _playerController;
    private TarodevController.PlayerAnimator _playerAnimator;

    #region Data Fields

    public override bool IsAlive { get { return _data.IsAlive; } protected set { _data.IsAlive = value; } }
    public override bool IsInvincible { get { return _data.IsInvincible; } protected set { _data.IsInvincible = value; } }
    public override int Health { get { return _data.Health; } protected set { _data.Health = value; } }
    public override int MaxHealth => _data.MaxHealth;
    public override float InvincibilityTime => _data.InvincibilityTime;
    public override bool IsAttacking => _playerAnimator.CurrentState == PlayerAnimator.Attack;
    public override int BaseDamage => _data.BaseDamage;
    public override Vector2 BaseKnockback => _data.BaseKnockback;
    public override Vector2 CurrentPosition => _data.CurrentPosition;
    public override Vector2 ColliderOffset => _standingCollider.offset;

    #endregion

    protected override void Awake()
    {
        _playerController = GetComponent<TarodevController.PlayerController>();
        _playerAnimator = GetComponentInChildren<TarodevController.PlayerAnimator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ResetCombatData();
    }

    protected void OnEnable()
    {
        _playerController.Attacked += HandleAttackCollisions;
    }

    protected void OnDisable()
    {
        _playerController.Attacked -= HandleAttackCollisions;
    }

    #region Attack Collisions

    protected void HandleAttackCollisions()
    {
        Vector2 attackSize = new Vector2(1, 1);

        HashSet<Collider2D> enemiesToAttack = DetectEnemyColliders(
            bounds: GetColliderBounds(), 
            boxSize: attackSize, 
            angle: 0f, 
            direction: SpriteFlipped ? Vector2.left : Vector2.right,
            distance: 2.5f
        );

        if (enemiesToAttack.Count == 0) return;

        foreach (Collider2D enemyToAttack in enemiesToAttack)
        {
            Attack(enemyToAttack);
        }
    }

    protected HashSet<Collider2D> DetectEnemyColliders(Bounds bounds, Vector2 boxSize, float angle, Vector2 direction, float distance)
    {
        HashSet<Collider2D> enemyColliders = new HashSet<Collider2D>();

        LayerMask enemyLayerMask = LayerMask.GetMask("Enemy");
        Collider2D[] enemiesHitOverlap = Physics2D.OverlapBoxAll(bounds.center, boxSize, angle, enemyLayerMask);
        RaycastHit2D[] enemiesHitBoxCast = Physics2D.BoxCastAll(bounds.center, boxSize, angle, direction, distance, enemyLayerMask);

        foreach (RaycastHit2D enemyHit in enemiesHitBoxCast)
        {
            enemyColliders.Add(enemyHit.collider);
        }
        foreach (Collider2D enemyHit in enemiesHitOverlap)
        {
            enemyColliders.Add(enemyHit);
        }

        return enemyColliders;
    }
    #endregion
}
