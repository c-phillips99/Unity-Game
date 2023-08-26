using System.Collections;
using System.Collections.Generic;
using TarodevController;
using System;
using UnityEngine;
using TMPro;

public abstract class CombatBase : MonoBehaviour
{
    public event Action TookHit;
    public event Action<Vector2> TookKnockback;

    #region Data Fields

    protected float _timeSinceHit = 0;
    protected SpriteRenderer _spriteRenderer;
    public bool SpriteFlipped => _spriteRenderer.flipX;
    public virtual bool IsAlive { get; protected set; }
    public virtual bool IsInvincible { get; protected set; }
    public virtual int Health { get; protected set; }
    public virtual int MaxHealth { get; }
    public virtual float InvincibilityTime { get; }
    public virtual bool IsAttacking { get; }
    public virtual int BaseDamage { get; }
    public virtual Vector2 BaseKnockback { get; }
    public virtual Vector2 CurrentPosition { get; }
    public virtual Vector2 ColliderOffset { get; }

    #endregion

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        ResetCombatData();
    }

    protected virtual void Update()
    {
        HandleInvincibility();
    }

    protected void ResetCombatData()
    {
        Health = MaxHealth;
        IsAlive = true;
        IsInvincible = false;
    }

    #region Invinvibility

    protected void HandleInvincibility()
    {
        if (!IsInvincible) return;

        if (_timeSinceHit > InvincibilityTime)
        {
            IsInvincible = false;
            _timeSinceHit = 0;
        }
        else _timeSinceHit += Time.deltaTime;
    }

    #endregion

    #region Attack Colliders
    protected virtual Bounds GetColliderBounds()
    {
        var colliderOrigin = CurrentPosition + ColliderOffset;
        return new Bounds(colliderOrigin, Vector3.one);
    }

    #endregion

    #region Attack
    protected virtual void Attack(Collider2D colliderToAttack)
    {
        CombatManager.Instance.DealAttack(BaseDamage, BaseKnockback, colliderToAttack, gameObject);
    }

    public void TakeAttack(int damage, Vector2 knockback, GameObject attackSource)
    {
        TakeDamage(damage, attackSource);
        TakeKnockback(knockback, attackSource);
        IsInvincible = true;
    }
    #endregion

    #region Damage

    public void TakeDamage(int damage, GameObject damageSource)
    {
        if (!IsAlive || IsInvincible) return;

        Health -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage and has {Health} health remaining.");

        if (Health <= 0)
        {
            IsAlive = false;
            return;
        }
        else
        {
            TookHit?.Invoke();
        }
    }

    #endregion

    #region Knockback

    public void TakeKnockback(Vector2 knockback, GameObject knockbackSource)
    {
        if (!IsAlive || IsInvincible) return;

        Vector2 sourcePosition = knockbackSource.transform.position;

        // knockback direction x and y will be 1 (right/up) or -1 (left/down)
        Vector2 knockbackDirection = sourcePosition - CurrentPosition;
        knockbackDirection.x = knockbackDirection.x >= 0 ? -1 : 1;
        knockbackDirection.y = 1;
        //knockbackDirection.y = knockbackDirection.y >= 0 ? 1 : -1;

        TookKnockback?.Invoke(knockback * knockbackDirection);
    }

    #endregion
}
