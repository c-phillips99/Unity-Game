using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyCombat : CombatBase
{
    [SerializeField] protected EnemyData _data;
    [SerializeField] protected CapsuleCollider2D _enemyCollider;
    [SerializeField] protected EnemyControl _enemyController;

    #region Data Fields
    public bool IsTrackingPlayer { get { return _data.IsTrackingPlayer; } protected set { _data.IsTrackingPlayer = value; } }
    public override bool IsAlive { get { return _data.IsAlive; } protected set { _data.IsAlive = value; } }
    public override bool IsInvincible { get { return _data.IsInvincible; } protected set { _data.IsInvincible = value; } }
    public override int Health { get { return _data.Health; } protected set { _data.Health = value; } }
    public override int MaxHealth => _data.MaxHealth;
    public override float InvincibilityTime => _data.InvincibilityTime;
    public override bool IsAttacking => false; // NEED TO IMPLEMENT
    public override int BaseDamage => _data.BaseDamage;
    public override Vector2 BaseKnockback => _data.BaseKnockback;
    public override Vector2 CurrentPosition => _data.CurrentPosition;
    public override Vector2 ColliderOffset => _enemyCollider.offset;

    #endregion

    protected void OnEnable()
    {
        _enemyController.OnAttack += Attack;
    }
    protected void OnDisable()
    {
        _enemyController.OnAttack -= Attack;
    }
}
