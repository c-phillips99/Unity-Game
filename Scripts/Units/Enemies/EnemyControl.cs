using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyControl : MonoBehaviour
{
    public event Action<Collider2D> OnAttack;

    [SerializeField] protected EnemyData _data;
    [SerializeField] protected Rigidbody2D _rigidBody;
    [SerializeField] protected CapsuleCollider2D _collider;
    protected SpriteRenderer _spriteRenderer;
    protected States _currentState;
    protected Vector2 _moveDirection;
    protected Vector2 _speed;
    protected Vector2 _currentExternalVelocity;
    public bool IsTrackingPlayer { get { return _data.IsTrackingPlayer; } protected set { _data.IsTrackingPlayer = value; } }
    public bool HasControl { get { return _data.HasControl; } protected set { _data.HasControl = value; } }
    public Vector2 CurrentPosition { get { return _data.CurrentPosition; } protected set { _data.CurrentPosition = value; } }
    public float AttackCooldownTime => _data.AttackCooldownTime;

    protected enum States
    {
        Idle,
        Walking,
        Flying,
        Attacking,
        Falling,
        Jumping
    }

    #region External
    public virtual void TakeAwayControl(bool resetVelocity = true)
    {
        if (resetVelocity) _rigidBody.velocity = Vector2.zero;
        HasControl = false;
    }

    public virtual void ReturnControl()
    {
        _speed = Vector2.zero;
        HasControl = true;
    }

    public virtual void SetVelocity(Vector2 vel)
    {
        _speed += vel;
    }

    #endregion


    protected void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _moveDirection = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
        _currentState = States.Idle;
        HasControl = true;
    }

    protected void Update()
    {
        HandleAttackCooldown();
        HandleAttacking();
        HandleDisable();
    }

    protected void FixedUpdate()
    {
        CheckCapsuleCollisions();
        SearchForNearbyPlayer();

        HandleCollisions();

        HandleWalls();
        HandleJump();

        HandleHorizontal();
        HandleVertical();

        ApplyMovement();

        CurrentPosition = _rigidBody.position;
    }

    private void OnDrawGizmos()
    {
        /*
        var detectorSize = new Vector2(15, 5);
        var bounds = GetBounds(detectorSize);
        Gizmos.color = new Color(0.75f, 0.0f, 0.0f, 0.75f);
        //Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        */
    }

    #region Collisions
    protected bool _groundHit;
    protected bool _wallHit;
    protected bool _grounded;

    protected virtual void CheckCapsuleCollisions()
    {
        Physics2D.queriesHitTriggers = false;

        // Ground
        _groundHit = Physics2D.CapsuleCast(_collider.bounds.center, _collider.size, _collider.direction, 0, Vector2.down, _data.GrounderDistance, ~_data.EnemyLayer).collider != null ? true : false;

        // Walls
        Bounds bounds = GetBounds(_data.WallDetectorSize);
        _wallHit = Physics2D.OverlapBox(bounds.center, bounds.size, 0, _data.WallLayer) != null ? true : false;
    }

    protected virtual Bounds GetBounds(Vector2 detectorSize)
    {
        Vector2 colliderOrigin = _rigidBody.position + _collider.offset;
        return new Bounds(colliderOrigin, detectorSize);
    }


    protected virtual void HandleCollisions()
    {
        // Landed on the Ground
        if (!_grounded && _groundHit) _grounded = true;
        // Left the Ground
        else if (_grounded && !_groundHit) _grounded = false;
    }
    #endregion

    #region Nearby Player
    protected Collider2D _playerCollider;
    protected Vector2 _playerPosition;

    protected virtual void SearchForNearbyPlayer()
    {
        Bounds bounds = GetBounds(_data.PlayerSearchDetectionSize);
        LayerMask playerLayerMask = LayerMask.GetMask("Player");
        _playerCollider = Physics2D.OverlapBox(bounds.center, bounds.size, 0, playerLayerMask);

        if (_playerCollider != null)
        {
            IsTrackingPlayer = true;
            _playerPosition = (Vector2)_playerCollider.transform.position;
        }
        else
        {
            IsTrackingPlayer = false;
        }
    }
    #endregion

    #region Disables
    private float _disableTimer = 0;
    private float _defaultTimeDisabled = .75f;
    protected void HandleDisable()
    {
        if (HasControl) return;

        if (_disableTimer >= _defaultTimeDisabled)
        {
            ReturnControl();
            _disableTimer = 0;
        }
        else
        {
            _disableTimer += Time.deltaTime;
        }
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        TakeAwayControl();
        SetVelocity(knockback);
    }
    #endregion

    #region Walls
    protected virtual void HandleWalls()
    {
        // May need to prioritize the nearest wall here... But who is going to make a climbable wall that tight?
        if (_wallHit && !IsTrackingPlayer)
        {
            // _wallDirection = (int)Mathf.Sign(_wallContacts[0].point.x - transform.position.x);
            _moveDirection = _spriteRenderer.flipX ? new Vector2(1, _moveDirection.y) : new Vector2(-1, _moveDirection.y);
            HandleSpriteFlipping();
        }
    }

    #endregion

    #region Jumping
    protected void HandleJump()
    {
        if (!_data.IsJumpingEnemy) return;
    }
    #endregion

    #region Attacking
    protected float _attackCooldownTimer = 0;
    protected bool _canAttack = true;
    protected void HandleAttacking()
    {
        if (!_canAttack || !IsTrackingPlayer) return;

        Vector2 attackRange = _data.PlayerAttackDetectionSize;
        Vector2 distance = _playerPosition - CurrentPosition;

        if (Math.Abs(distance.x) <= attackRange.x && Math.Abs(distance.y) <= attackRange.y)
        {
            OnAttack?.Invoke(_playerCollider);
            _canAttack = false;
        }
    }
    protected void HandleAttackCooldown()
    {
        if (_canAttack) return;

        if (_attackCooldownTimer > AttackCooldownTime)
        {
            _canAttack = true;
            _attackCooldownTimer = 0;
        }
        else
        {
            _attackCooldownTimer += Time.deltaTime;
        }
    }
    #endregion

    #region Horizontal

    protected void HandleHorizontal()
    {
        if (IsTrackingPlayer)
        {
            _moveDirection = (_playerPosition.x - _data.CurrentPosition.x) > 0 ? Vector2.right : Vector2.left;
            HandleSpriteFlipping();
        }

        if (!HasControl)
        {
            var deceleration = _data.DecelerationRate;
            _speed.x = Mathf.MoveTowards(_speed.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            _speed.x = _moveDirection.x * _data.WalkSpeed * Time.fixedDeltaTime;
        }
    }

    #endregion

    #region Vertical
    protected void HandleVertical()
    {
        _speed.y = _rigidBody.velocity.y;
        if (!_data.IsFlyingEnemy) return;
    }
    #endregion

    #region SpriteFlip

    protected void HandleSpriteFlipping() => _spriteRenderer.flipX = _moveDirection == Vector2.right ? false : true;

    #endregion

    #region Movement

    protected void ApplyMovement() => _rigidBody.velocity = _speed;    

    #endregion
}
