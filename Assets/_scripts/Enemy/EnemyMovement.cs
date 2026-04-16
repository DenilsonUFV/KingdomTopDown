using UnityEngine;

/// <summary>
/// Movimento baseado em Rigidbody2D para inimigos e BOTs defensores.
/// Suporta desvio inteligente de obstáculos via CircleCast.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Desvio de Obstáculos")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionRange  = 1.5f;
    [SerializeField] private float sideSensorAngle = 35f;
    [SerializeField] private float arriveRadius    = 0.25f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Rigidbody2D _rb;
    private Vector2 _targetPosition;
    private bool    _isMoving;
    private float   _moveSpeed = 2f;

    public bool HasArrived => Vector2.Distance(transform.position, _targetPosition) <= arriveRadius;
    public bool IsMoving   => _isMoving;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!_isMoving) { _rb.linearVelocity = Vector2.zero; return; }
        if (HasArrived)  { Stop(); return; }

        Vector2 dir      = ((Vector2)_targetPosition - (Vector2)transform.position).normalized;
        Vector2 smartDir = AvoidObstacles(dir);
        _rb.linearVelocity = smartDir * _moveSpeed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Inicialização

    /// <summary>Chamado pelo Brain com a velocidade do EnemyData.</summary>
    public void Init(float speed) => _moveSpeed = speed;

    #endregion

    // ─────────────────────────────────────────
    #region API

    public void MoveTo(Vector2 target) { _targetPosition = target; _isMoving = true; }

    public void Stop()
    {
        _isMoving = false;
        _rb.linearVelocity = Vector2.zero;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Desvio de Obstáculos

    private Vector2 AvoidObstacles(Vector2 dir)
    {
        RaycastHit2D hitCenter = Physics2D.CircleCast(transform.position, 0.3f, dir, detectionRange, obstacleLayer);
        if (hitCenter.collider == null) return dir;

        Vector2 leftDir  = Quaternion.Euler(0, 0,  sideSensorAngle) * dir;
        Vector2 rightDir = Quaternion.Euler(0, 0, -sideSensorAngle) * dir;

        RaycastHit2D hitLeft  = Physics2D.Raycast(transform.position, leftDir,  detectionRange, obstacleLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightDir, detectionRange, obstacleLayer);

        if (hitLeft.collider  == null) return leftDir;
        if (hitRight.collider == null) return rightDir;

        return Vector2.Reflect(dir, hitCenter.normal);
    }

    #endregion
}
