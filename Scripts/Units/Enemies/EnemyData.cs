using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    [HideInInspector] public int Health;
    [HideInInspector] public bool IsAlive = true;
    [HideInInspector] public bool IsInvincible = false;
    [HideInInspector] public bool IsTrackingPlayer = false;
    [HideInInspector] public bool HasControl = true;
    [HideInInspector] public Vector2 CurrentPosition;

    /****************** USER MENU ******************/
    [Header("LAYERS")]

    [Tooltip("Set this to the layer the enemy is on")]
    [SerializeField] private LayerMask _enemyLayer;

    [Tooltip("Set this to the layer walls are on")]
    [SerializeField] private LayerMask _wallLayer;

    // General stats
    [Header("GENERAL")]

    [Tooltip("Name")]
    [SerializeField] private string _enemyName;

    [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
    [SerializeField] private float _grounderDistance = 0.05f;

    [Tooltip("Bounds for detecting walls on either side. Ensure it's wider than your vertical capsule collider")]
    [SerializeField] private Vector2 _wallDetectorSize = new(0.75f, 0.25f);

    [Tooltip("Search size for finding the player")]
    [SerializeField] private Vector2 _playerSearchDetectionSize = new(15, 5);

    [Tooltip("Search size for attacking the player")]
    [SerializeField] private Vector2 _playerAttackDetectionSize = new(5, 3);

    [Header("MOVEMENT")]

    [Tooltip("Toggle if enemy can walk or not")]
    [SerializeField] private bool _isWalkingEnemy;

    [Tooltip("Toggle if enemy can fly or not")]
    [SerializeField] private bool _isFlyingEnemy;

    [Tooltip("Toggle if enemy can jump or not")]
    [SerializeField] private bool _isJumpingEnemy;

    [Tooltip("Walking Speed")]
    [SerializeField] private float _walkSpeed;

    [Tooltip("Flying Speed")]
    [SerializeField] private float _flySpeed;

    [Tooltip("Flying Speed")]
    [SerializeField] private float _jumpStrength;

    [Tooltip("Movement Slowdown On Attack")]
    [SerializeField] private float _decelerationRate = 0.01f;

    // Combat stats
    [Header("COMBAT")]

    [Tooltip("Max Health")]
    [SerializeField] private int _maxHealth;

    [Tooltip("Invincible Time After Hit")]
    [SerializeField] private float _invincibilityTime = 0.25f;

    [Tooltip("Minimum time between attacks")]
    [SerializeField] private float _attackCooldownTime = 1f;

    [Tooltip("Default damage dealt on hit")]
    [SerializeField] private int _baseDamage = 1;

    [Tooltip("Default knockback dealt on hit")]
    [SerializeField] private Vector2 _baseKnockback = new Vector2 (5, 1);

    /****************** PRIVATE DATA ******************/


    /****************** PUBLIC FIELDS ******************/
    public LayerMask EnemyLayer => _enemyLayer;
    public LayerMask WallLayer => _wallLayer;
    public string EnemyName => _enemyName;
    public float GrounderDistance => _grounderDistance;
    public Vector2 WallDetectorSize => _wallDetectorSize;
    public Vector2 PlayerSearchDetectionSize => _playerSearchDetectionSize;
    public Vector2 PlayerAttackDetectionSize => _playerAttackDetectionSize;
    public bool IsWalkingEnemy => _isWalkingEnemy;
    public bool IsFlyingEnemy => _isFlyingEnemy;
    public bool IsJumpingEnemy => _isJumpingEnemy;
    public float WalkSpeed => _walkSpeed;
    public float FlySpeed => _flySpeed;
    public float JumpStrength => _jumpStrength;
    public float DecelerationRate => _decelerationRate;
    public int MaxHealth => _maxHealth;
    public float InvincibilityTime => _invincibilityTime;
    public float AttackCooldownTime => _attackCooldownTime;
    public int BaseDamage => _baseDamage;
    public Vector2 BaseKnockback => _baseKnockback;
}
