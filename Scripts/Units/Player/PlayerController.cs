// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TarodevController {
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController {
        [SerializeField] private PlayerData _data;

        #region Internal

        [HideInInspector] private Rigidbody2D _rb; // Hide is for serialization to avoid errors in gizmo calls
        [SerializeField] private CapsuleCollider2D _standingCollider;
        [SerializeField] private CapsuleCollider2D _crouchingCollider;
        private CapsuleCollider2D _col; // current active collider
        // private PlayerInput _input;
        private PlayerAnimator _animator;
        private PlayerCombat _combat;
        private bool _cachedTriggerSetting;

        private FrameInput FrameInput;
        private Vector2 _speed;
        private Vector2 _currentExternalVelocity;
        private int _fixedFrame;
        private bool _hasControl = true;

        #endregion

        #region External

        public event Action<bool, float> GroundedChanged;
        public event Action<bool, Vector2> DashingChanged;
        public event Action<bool> WallGrabChanged;
        public event Action<bool> LedgeClimbChanged;
        public event Action<bool> Jumped;
        public event Action AirJumped;
        public event Action Attacked;
        public PlayerData PlayerStats => _data;
        public Vector2 Input => FrameInput.Move;
        public Vector2 Velocity => _rb.velocity;
        public Vector2 Speed => _speed; // + _currentExternalVelocity; // we should add this, right?
        public Vector2 GroundNormal { get; private set; }
        public int WallDirection { get; private set; }
        public bool Crouching { get; private set; }
        public bool ClimbingLadder { get; private set; }
        public bool GrabbingLedge { get; private set; }
        public bool ClimbingLedge { get; private set; }

        public virtual void ApplyVelocity(Vector2 vel, UnitForce forceType) {
            if (forceType == UnitForce.Burst) _speed += vel;
            else _currentExternalVelocity += vel;
        }

        public virtual void SetVelocity(Vector2 vel, UnitForce velocityType) {
            if (velocityType == UnitForce.Burst) _speed = vel;
            else _currentExternalVelocity = vel;
        }

        public virtual void TakeAwayControl(bool resetVelocity = true) {
            if (resetVelocity) _rb.velocity = Vector2.zero;
            _hasControl = false;
        }

        public virtual void ReturnControl() {
            _speed = Vector2.zero;
            _hasControl = true;
        }

        #endregion

        protected virtual void Awake() {
            _rb = GetComponent<Rigidbody2D>();
            //_input = GetComponent<PlayerInput>();
            _animator = GetComponentInChildren<PlayerAnimator>();
            _combat = GetComponent<PlayerCombat>();
            _cachedTriggerSetting = Physics2D.queriesHitTriggers;
            Physics2D.queriesStartInColliders = false;

            ToggleColliders(isStanding: true);
        }

        protected virtual void OnEnable()
        {
            _combat.TookKnockback += ApplyKnockback;
        }

        protected virtual void OnDisable()
        {
            _combat.TookKnockback -= ApplyKnockback;
        }

        protected virtual void Update() {
            GatherInput();
            HandleDisable();
        }

        protected virtual void GatherInput() {
            FrameInput = PlayerInput.FrameInput;

            if (_data.SnapInput)
            {
                FrameInput.Move.x = Mathf.Abs(FrameInput.Move.x) < _data.HorizontalDeadzoneThreshold ? 0 : Mathf.Sign(FrameInput.Move.x);
                FrameInput.Move.y = Mathf.Abs(FrameInput.Move.y) < _data.VerticalDeadzoneThreshold ? 0 : Mathf.Sign(FrameInput.Move.y);
            }

            if (FrameInput.JumpDown) {
                _jumpToConsume = true;
                _frameJumpWasPressed = _fixedFrame;
            }

            if (FrameInput.Move.x != 0) _stickyFeet = false;

            if (FrameInput.DashDown && _data.AllowDash) _dashToConsume = true;
            if (FrameInput.AttackDown && _data.AllowAttacks) _attackToConsume = true;
        }

        protected virtual void FixedUpdate() {
            _fixedFrame++;

            CheckCollisions();
            HandleCollisions();
            
            HandleWalls();
            HandleLedges();
            HandleLadders();

            HandleCrouching();
            HandleJump();
            HandleDash();
            HandleAttacking();

            HandleHorizontal();
            HandleVertical();
            ApplyMovement();

            HandleInteractable();

            _data.CurrentPosition = _rb.position;
        }

        #region Collisions

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[2];
        private readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[2];
        private readonly Collider2D[] _wallHits = new Collider2D[2];
        private readonly Collider2D[] _ladderHits = new Collider2D[2];
        private RaycastHit2D _hittingWall;
        private int _groundHitCount;
        private int _ceilingHitCount;
        private int _wallHitCount;
        private int _ladderHitCount;
        private int _frameLeftGrounded = int.MinValue;
        private bool _grounded;
        private Vector2 _skinWidth = new(0.02f, 0.02f); // Expose this?

        protected virtual void CheckCollisions() {
            Physics2D.queriesHitTriggers = false;

            // Ground and Ceiling
            _groundHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _groundHits, _data.GrounderDistance, ~_data.PlayerLayer);
            _ceilingHitCount = Physics2D.CapsuleCastNonAlloc(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _ceilingHits, _data.GrounderDistance, ~_data.PlayerLayer);

            // Walls and Ladders
            var bounds = GetBounds(_data.WallDetectorSize);
            _wallHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _wallHits, _data.ClimbableLayer);

            _hittingWall = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, new Vector2(PlayerInput.FrameInput.Move.x, 0), _data.GrounderDistance, ~_data.PlayerLayer);

            Physics2D.queriesHitTriggers = true; // Ladders are set to Trigger
            _ladderHitCount = Physics2D.OverlapBoxNonAlloc(bounds.center, bounds.size, 0, _ladderHits, _data.LadderLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
        }

        protected virtual bool TryGetGroundNormal(out Vector2 groundNormal) {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.Raycast(_rb.position, Vector2.down, _data.GrounderDistance * 2, ~_data.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            groundNormal = hit.normal; // defaults to Vector2.zero if nothing was hit
            return hit.collider;
        }

        protected virtual Bounds GetBounds(Vector2 size) {
            var colliderOrigin = _rb.position + _standingCollider.offset;
            return new Bounds(colliderOrigin, size);
        }

        protected virtual void HandleCollisions() {
            // Hit a Ceiling
            if (_ceilingHitCount > 0) {
                // prevent sticking to ceiling if we did an InAir jump after receiving external velocity w/ PlayerForce.Decay
                _currentExternalVelocity.y = Mathf.Min(0f, _currentExternalVelocity.y);
                _speed.y = Mathf.Min(0, _speed.y);
            }

            // Landed on the Ground
            if (!_grounded && _groundHitCount > 0) {
                _grounded = true;
                ResetDash();
                ResetJump();
                GroundedChanged?.Invoke(true, Mathf.Abs(_speed.y));
                if (FrameInput.Move.x == 0) _stickyFeet = true;
            }
            // Left the Ground
            else if (_grounded && _groundHitCount == 0) {
                _grounded = false;
                _frameLeftGrounded = _fixedFrame;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        protected virtual bool IsStandingPosClear(Vector2 pos) => CheckPos(pos, _standingCollider);
        protected virtual bool IsCrouchingPosClear(Vector2 pos) => CheckPos(pos, _crouchingCollider);

        protected virtual bool CheckPos(Vector2 pos, CapsuleCollider2D col) {
            Physics2D.queriesHitTriggers = false;
            var hit = Physics2D.OverlapCapsule(pos + col.offset, col.size - _skinWidth, col.direction, 0, ~_data.PlayerLayer);
            Physics2D.queriesHitTriggers = _cachedTriggerSetting;
            return !hit;
        }

        #endregion

        #region Walls

        private readonly ContactPoint2D[] _wallContacts = new ContactPoint2D[2];
        private float _currentWallJumpMoveMultiplier = 1f; // aka "Horizontal input influence"
        private int _lastWallDirection; // for coyote wall jumps
        private int _frameLeftWall; // for coyote wall jumps
        private bool _isLeavingWall; // prevents immediate re-sticking to wall
        private bool _isOnWall;

        protected virtual void HandleWalls() {
            if (!_hasControl) return;
            if (!_data.AllowWalls) return;

            _currentWallJumpMoveMultiplier = Mathf.MoveTowards(_currentWallJumpMoveMultiplier, 1f, 1f / _data.WallJumpInputLossFrames);

            // May need to prioritize the nearest wall here... But who is going to make a climbable wall that tight?
            if (_wallHitCount > 0 && _wallHits[0].GetContacts(_wallContacts) > 0) {
                WallDirection = (int)Mathf.Sign(_wallContacts[0].point.x - transform.position.x);
                _lastWallDirection = WallDirection;
            }
            else WallDirection = 0;

            if (!_isOnWall && ShouldStickToWall() && _speed.y <= 0) ToggleOnWall(true);
            else if (_isOnWall && !ShouldStickToWall()) ToggleOnWall(false);

            bool ShouldStickToWall() {
                if (WallDirection == 0 || _grounded) return false;
                return !_data.RequireInputPush || (HorizontalInputPressed && Mathf.Sign(FrameInput.Move.x) == WallDirection);
            }
        }

        private void ToggleOnWall(bool on) {
            _isOnWall = on;
            if (on) {
                _speed = Vector2.zero;
                _currentExternalVelocity = Vector2.zero;
                _bufferedJumpUsable = true;
                _wallJumpCoyoteUsable = true;
            }
            else {
                _frameLeftWall = _fixedFrame;
                _isLeavingWall = false; // after we've left the wall
                ResetAirJumps(); // so that we can air jump even if we didn't leave via a wall jump
            }

            WallGrabChanged?.Invoke(on);
        }

        #endregion

        #region Ledges

        private Vector2 _ledgeCornerPos;
        private bool _climbIntoCrawl;

        protected virtual bool LedgeClimbInputDetected => Input.y > _data.VerticalDeadzoneThreshold || Input.x == WallDirection;

        protected virtual void HandleLedges() {
            if (!_hasControl) return;
            if (!_data.AllowLedges) return;
            if (ClimbingLedge || !_isOnWall) return;

            GrabbingLedge = TryGetLedgeCorner(out _ledgeCornerPos);

            if (GrabbingLedge) HandleLedgeGrabbing();
        }

        protected virtual bool TryGetLedgeCorner(out Vector2 cornerPos) {
            cornerPos = Vector2.zero;
            var grabHeight = _rb.position + _data.LedgeGrabPoint.y * Vector2.up;

            var hit1 = Physics2D.Raycast(grabHeight + _data.LedgeRaycastSpacing * Vector2.down, WallDirection * Vector2.right, 0.5f, _data.ClimbableLayer);
            if (!hit1.collider) return false; // Should hit below the ledge. Mainly used to determine xPos accurately

            var hit2 = Physics2D.Raycast(grabHeight + _data.LedgeRaycastSpacing * Vector2.up, WallDirection * Vector2.right, 0.5f, _data.ClimbableLayer);
            if (hit2.collider)
                return false; // we only are within ledge-grab range when the first hits and second doesn't

            var hit3 = Physics2D.Raycast(grabHeight + new Vector2(WallDirection * 0.5f, _data.LedgeRaycastSpacing), Vector2.down, 0.5f, _data.ClimbableLayer);
            if (!hit3.collider) return false; // gets our yPos of the corner

            cornerPos = new(hit1.point.x, hit3.point.y);
            return true;
        }

        protected virtual void HandleLedgeGrabbing() {
            if (!_hasControl) return;
            // Nudge towards better grabbing position
            if (Input.x == 0 && _hasControl) {
                var pos = _rb.position;
                var targetPos = _ledgeCornerPos - Vector2.Scale(_data.LedgeGrabPoint, new(WallDirection, 1f));
                _rb.position = Vector2.MoveTowards(pos, targetPos, _data.LedgeGrabDeceleration * Time.fixedDeltaTime);
            }

            if (LedgeClimbInputDetected) {
                var finalPos = _ledgeCornerPos + Vector2.Scale(_data.StandUpOffset, new(WallDirection, 1f));
                
                if (IsStandingPosClear(finalPos)) {
                    _climbIntoCrawl = false;
                    StartLedgeClimb();
                }
                else if (_data.AllowCrouching && IsCrouchingPosClear(finalPos)) {
                    _climbIntoCrawl = true;
                    StartLedgeClimb(intoCrawl: true);
                }
            }
        }

        protected virtual void StartLedgeClimb(bool intoCrawl = false) {
            LedgeClimbChanged?.Invoke(intoCrawl);
            TakeAwayControl();
            ClimbingLedge = true;
            GrabbingLedge = false;
            _rb.position = _ledgeCornerPos - Vector2.Scale(_data.LedgeGrabPoint, new(WallDirection, 1f));
        }

        public virtual void TeleportMidLedgeClimb() {
            transform.position = _rb.position = _ledgeCornerPos + Vector2.Scale(_data.StandUpOffset, new(WallDirection, 1f));
            if (_climbIntoCrawl) TryToggleCrouching(shouldCrouch: true);
            ToggleOnWall(false);
        }

        public virtual void FinishClimbingLedge() {
            ClimbingLedge = false;
            ReturnControl();
        }

        #endregion

        #region Ladders

        private Vector2 _ladderSnapVel;
        private int _frameLeftLadder;

        protected virtual bool CanEnterLadder => _ladderHitCount > 0 && _fixedFrame > _frameLeftLadder + _data.LadderCooldownFrames;
        protected virtual bool ShouldMountLadder => _data.AutoAttachToLadders || FrameInput.Move.y > _data.VerticalDeadzoneThreshold || (!_grounded && FrameInput.Move.y < -_data.VerticalDeadzoneThreshold);
        protected virtual bool ShouldDismountLadder => !_data.AutoAttachToLadders && _grounded && FrameInput.Move.y < -_data.VerticalDeadzoneThreshold;
        protected virtual bool ShouldCenterOnLadder => _data.SnapToLadders && FrameInput.Move.x == 0 && _hasControl;

        protected virtual void HandleLadders() {
            if (!_hasControl) return;
            if (!_data.AllowLadders) return;

            if (!ClimbingLadder && CanEnterLadder && ShouldMountLadder) ToggleClimbingLadder(true);
            else if (ClimbingLadder && (_ladderHitCount == 0 || ShouldDismountLadder)) ToggleClimbingLadder(false);

            if (ClimbingLadder && ShouldCenterOnLadder) {
                var pos = _rb.position;
                var targetX = _ladderHits[0].transform.position.x;
                _rb.position = Vector2.SmoothDamp(pos, new Vector2(targetX, pos.y), ref _ladderSnapVel, _data.LadderSnapTime);
            }
        }

        private void ToggleClimbingLadder(bool on) {
            if (ClimbingLadder == on) return;
            if (on) {
                _speed = Vector2.zero;
                _ladderSnapVel = Vector2.zero; // reset damping velocity for consistency
            }
            else {
                if (_ladderHitCount > 0) _frameLeftLadder = _fixedFrame; // to prevent immediately re-mounting ladder
                if (FrameInput.Move.y > 0) _speed.y += _data.LadderPopForce; // Pop off ladders
            }

            ClimbingLadder = on;
            ResetAirJumps();
        }

        #endregion

        #region Crouching

        private int _frameStartedCrouching;

        protected virtual bool CrouchPressed => FrameInput.Move.y < -_data.VerticalDeadzoneThreshold;
        protected virtual bool CanStand => IsStandingPosClear(_rb.position + new Vector2(0, _data.CrouchBufferCheck));

        protected virtual void HandleCrouching() {
            if (!_hasControl) return;
            if (!_data.AllowCrouching) return;

            if (!Crouching && CrouchPressed && _grounded) TryToggleCrouching(true);
            else if (Crouching && (!CrouchPressed || !_grounded)) TryToggleCrouching(false);
        }

        protected virtual bool TryToggleCrouching(bool shouldCrouch) {
            if (Crouching && !CanStand) return false;

            Crouching = shouldCrouch;
            ToggleColliders(!shouldCrouch);
            if (Crouching) _frameStartedCrouching = _fixedFrame;
            return true;
        }

        protected virtual void ToggleColliders(bool isStanding) {
            _col = isStanding ? _standingCollider : _crouchingCollider;
            _standingCollider.enabled = isStanding;
            _crouchingCollider.enabled = !isStanding;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private bool _wallJumpCoyoteUsable;
        private int _frameJumpWasPressed;
        private int _airJumpsRemaining;

        protected virtual bool HasBufferedJump => _bufferedJumpUsable && _fixedFrame < _frameJumpWasPressed + _data.JumpBufferFrames;
        protected virtual bool CanUseCoyote => _coyoteUsable && !_grounded && _fixedFrame < _frameLeftGrounded + _data.CoyoteFrames;
        protected virtual bool CanWallJump => (_isOnWall && !_isLeavingWall) || (_wallJumpCoyoteUsable && _fixedFrame < _frameLeftWall + _data.WallJumpCoyoteFrames);
        protected virtual bool CanAirJump => !_grounded && _airJumpsRemaining > 0;

        protected virtual void HandleJump() {
            if (!_hasControl) return;
            if (!_endedJumpEarly && !_grounded && !FrameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true; // Early end detection

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (CanWallJump) WallJump();
            else if (_grounded || ClimbingLadder || CanUseCoyote) NormalJump();
            else if (_jumpToConsume && CanAirJump) AirJump();

            _jumpToConsume = false; // Always consume the flag
        }

        // Includes Ladder Jumps
        protected virtual void NormalJump() {
            if (Crouching && !TryToggleCrouching(false)) return; // try standing up first so we don't get stuck in low ceilings
            _endedJumpEarly = false;
            _frameJumpWasPressed = 0; // prevents double-dipping 1 input's jumpToConsume and buffered jump for low ceilings
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            ToggleClimbingLadder(false);
            _speed.y = _data.JumpPower;
            Jumped?.Invoke(false);
        }

        protected virtual void WallJump() {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            if (_isOnWall) _isLeavingWall = true; // only toggle if it's a real WallJump, not CoyoteWallJump
            _wallJumpCoyoteUsable = false;
            _currentWallJumpMoveMultiplier = 0;
            _speed = Vector2.Scale(_data.WallJumpPower, new(-_lastWallDirection, 1));
            Jumped?.Invoke(true);
        }

        protected virtual void AirJump() {
            _endedJumpEarly = false;
            _airJumpsRemaining--;
            _speed.y = _data.JumpPower;
            _currentExternalVelocity.y = 0; // optional. test it out with a Bouncer if this feels better or worse
            AirJumped?.Invoke();
        }

        protected virtual void ResetJump() {
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            ResetAirJumps();
        }

        protected virtual void ResetAirJumps() => _airJumpsRemaining = _data.MaxAirJumps;

        #endregion

        #region Dashing

        private bool _dashToConsume;
        private bool _canDash;
        private Vector2 _dashVel;
        private bool _dashing;
        private int _startedDashing;
        private float _nextDashTime;

        protected virtual void HandleDash() {
            if (!_hasControl) return;
            if (_dashToConsume && _canDash && !Crouching && Time.time > _nextDashTime) {
                var dir = new Vector2(FrameInput.Move.x, Mathf.Max(FrameInput.Move.y, 0f)).normalized;
                if (dir == Vector2.zero) {
                    _dashToConsume = false;
                    return;
                }

                _dashVel = dir * _data.DashVelocity;
                _dashing = true;
                _canDash = false;
                _startedDashing = _fixedFrame;
                _nextDashTime = Time.time + _data.DashCooldown;
                DashingChanged?.Invoke(true, dir);

                _currentExternalVelocity = Vector2.zero; // Strip external buildup
            }

            if (_dashing) {
                _speed = _dashVel;
                // Cancel when the time is out or we've reached our max safety distance
                if (_fixedFrame > _startedDashing + _data.DashDurationFrames) {
                    _dashing = false;
                    DashingChanged?.Invoke(false, Vector2.zero);
                    _speed.y = Mathf.Min(0, _speed.y);
                    _speed.x *= _data.DashEndHorizontalMultiplier;
                    if (_grounded) ResetDash();
                }
            }

            _dashToConsume = false;
        }

        protected virtual void ResetDash() {
            _canDash = true;
        }

        #endregion

        #region Attacking

        private bool _attackToConsume;
        private int _frameLastAttacked = int.MinValue;


        protected virtual void HandleAttacking() {
            if (!_attackToConsume) return;
            if (!_hasControl) return;
            // note: animation looks weird if we allow attacking while crouched. consider different attack animations or not allow it while crouched
            if (_fixedFrame > _frameLastAttacked + _data.AttackFrameCooldown) {
                _frameLastAttacked = _fixedFrame;
                Attacked?.Invoke();
            }

            _attackToConsume = false;
        }

        #endregion

        #region Horizontal

        protected virtual bool HorizontalInputPressed => Mathf.Abs(FrameInput.Move.x) > _data.HorizontalDeadzoneThreshold;
        private bool _stickyFeet;

        protected virtual void HandleHorizontal() {
            if (_dashing || !_hasControl) return;

            // Deceleration
            if (!HorizontalInputPressed) {
                var deceleration = _grounded ? _data.GroundDeceleration * (_stickyFeet ? _data.StickyFeetMultiplier : 1) : _data.AirDeceleration;
                _speed.x = Mathf.MoveTowards(_speed.x, 0, deceleration * Time.fixedDeltaTime);
            }
            // Crawling
            else if (Crouching && _grounded) {
                var crouchPoint = Mathf.InverseLerp(0, _data.CrouchSlowdownFrames, _fixedFrame - _frameStartedCrouching);
                var diminishedMaxSpeed = _data.MaxSpeed * Mathf.Lerp(1, _data.CrouchSpeedPenalty, crouchPoint);
                _speed.x = Mathf.MoveTowards(_speed.x, FrameInput.Move.x * diminishedMaxSpeed, _data.GroundDeceleration * Time.fixedDeltaTime);
            }
            // Regular Horizontal Movement
            else {
                // Prevent useless horizontal speed buildup when against a wall
                if (_hittingWall.collider && Mathf.Abs(_rb.velocity.x) < 0.01f && !_isLeavingWall) _speed.x = 0;

                var xInput = FrameInput.Move.x * (ClimbingLadder ? _data.LadderShimmySpeedMultiplier : 1);
                _speed.x = Mathf.MoveTowards(_speed.x, xInput * _data.MaxSpeed, _currentWallJumpMoveMultiplier * _data.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Vertical

        protected virtual void HandleVertical() {
            if (_dashing) return;

            // Ladder
            if (ClimbingLadder && _hasControl) {
                var yInput = FrameInput.Move.y;
                _speed.y = yInput * (yInput > 0 ? _data.LadderClimbSpeed : _data.LadderSlideSpeed);
            }
            // Grounded & Slopes
            else if (_grounded && _speed.y <= 0f && _hasControl) {
                _speed.y = _data.GroundingForce;

                if (TryGetGroundNormal(out var groundNormal)) {
                    GroundNormal = groundNormal;
                    if (!Mathf.Approximately(GroundNormal.y, 1f)) {
                        // on a slope
                        _speed.y = _speed.x * -GroundNormal.x / GroundNormal.y;
                        if (_speed.x != 0) _speed.y += _data.GroundingForce;
                    }
                }
            }
            // Wall Climbing & Sliding
            else if (_isOnWall && !_isLeavingWall && _hasControl) {
                if (FrameInput.Move.y > 0) _speed.y = _data.WallClimbSpeed;
                else if (FrameInput.Move.y < 0) _speed.y = -_data.MaxWallFallSpeed;
                else if (GrabbingLedge) _speed.y = Mathf.MoveTowards(_speed.y, 0, _data.LedgeGrabDeceleration * Time.fixedDeltaTime);
                else _speed.y = Mathf.MoveTowards(Mathf.Min(_speed.y, 0), -_data.MaxWallFallSpeed, _data.WallFallAcceleration * Time.fixedDeltaTime);
            }
            // In Air
            else {
                var inAirGravity = _data.FallAcceleration;
                if (_endedJumpEarly && _speed.y > 0) inAirGravity *= _data.JumpEndEarlyGravityModifier;
                _speed.y = Mathf.MoveTowards(_speed.y, -_data.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Disables
        // private float _disableTimer = 0;
        // private float _defaultTimeDisabled = .75f;
        protected void HandleDisable()
        {
            if (_hasControl) return;


            if (_animator.CurrentState != PlayerAnimator.Hit) ReturnControl();
            /*
            if (_disableTimer >= _defaultTimeDisabled)
            {
                ReturnControl();
                _disableTimer = 0;
            }
            else
            {
                _disableTimer += Time.deltaTime;
            }
            */
        }
        public void ApplyKnockback(Vector2 knockback)
        {
            TakeAwayControl();
            SetVelocity(knockback, UnitForce.Burst);
        }

        #endregion

        #region Movement
        protected virtual void ApplyMovement() {
            _rb.velocity = _speed + _currentExternalVelocity;
            _currentExternalVelocity = Vector2.MoveTowards(_currentExternalVelocity, Vector2.zero, _data.ExternalVelocityDecay * Time.fixedDeltaTime);
        }
        #endregion

        #region Interacting

        protected virtual void HandleInteractable()
        {
            Vector2 circleOrigin = _rb.position + _standingCollider.offset;
            Collider2D interactableCollider = Physics2D.OverlapCircle(circleOrigin, _data.InteratableDetectorSize, _data.InteractableLayer);
            if (interactableCollider == null) return;
            
            // display interactable on hud

            if (FrameInput.Interact)
            {
                IInteractable interactable = interactableCollider.GetComponent<IInteractable>();
                interactable.Interact(this);
            }
        }

        public virtual void EndInteraction()
        {
            ReturnControl();
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (_data == null) return;

            if (_data.ShowWallDetection && _standingCollider != null) {
                Gizmos.color = Color.white;
                var bounds = GetBounds(_data.WallDetectorSize);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            if (_data.AllowLedges && _data.ShowLedgeDetection) {
                Gizmos.color = Color.red;
                var facingDir = Mathf.Sign(WallDirection);
                var grabHeight = transform.position + _data.LedgeGrabPoint.y * Vector3.up;
                var grabPoint = grabHeight + facingDir * _data.LedgeGrabPoint.x * Vector3.right;
                Gizmos.DrawWireSphere(grabPoint, 0.05f);
                Gizmos.DrawWireSphere(grabPoint + Vector3.Scale(_data.StandUpOffset, new(facingDir, 1)), 0.05f);
                Gizmos.DrawRay(grabHeight + _data.LedgeRaycastSpacing * Vector3.down, 0.5f * facingDir * Vector3.right);
                Gizmos.DrawRay(grabHeight + _data.LedgeRaycastSpacing * Vector3.up, 0.5f * facingDir * Vector3.right);
            }
        }

        private void OnValidate() {
            if (_data == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
            if (_standingCollider == null) Debug.LogWarning("Please assign a Capsule Collider to the Standing Collider slot", this);
            if (_crouchingCollider == null) Debug.LogWarning("Please assign a Capsule Collider to the Crouching Collider slot", this);
            if (_rb == null && !TryGetComponent(out _rb)) Debug.LogWarning("Ensure the GameObject with the Player Controller has a Rigidbody2D", this);
        }
#endif
    }

    public interface IPlayerController {
        /// <summary>
        /// true = Landed. false = Left the Ground. float is Impact Speed
        /// </summary>
        public event Action<bool, float> GroundedChanged;

        public event Action<bool, Vector2> DashingChanged; // Dashing - Dir
        public event Action<bool> WallGrabChanged;
        public event Action<bool> LedgeClimbChanged; // Into Crawl
        public event Action<bool> Jumped; // Is wall jump
        public event Action AirJumped;
        public event Action Attacked;

        public PlayerData PlayerStats { get; }
        public Vector2 Input { get; }
        public Vector2 Speed { get; }
        public Vector2 Velocity { get; }
        public Vector2 GroundNormal { get; }
        public int WallDirection { get; }
        public bool Crouching { get; }
        public bool ClimbingLadder { get; }
        public bool GrabbingLedge { get; }
        public bool ClimbingLedge { get; }
        public void ApplyVelocity(Vector2 vel, UnitForce forceType);
        public void SetVelocity(Vector2 vel, UnitForce velocityType);
    }

    public enum UnitForce {
        /// <summary>
        /// Added directly to the players movement speed, to be controlled by the standard deceleration
        /// </summary>
        Burst,

        /// <summary>
        /// An external velocity that decays over time, applied additively to the rigidbody's velocity
        /// </summary>
        Decay
    }
}