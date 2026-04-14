using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BotMovement : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveRadius = 0.2f;

    [Header("Inteligência de Obstáculos")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionRange = 1.5f;
    [SerializeField] private float sideSensorAngle = 35f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Rigidbody2D _rb;
    private Vector2 _targetPosition;
    private bool _isMoving = false;

    public bool IsMoving => _isMoving;
    public bool HasArrived => Vector2.Distance(transform.position, _targetPosition) <= arriveRadius;
    public Vector2 MoveInput => _isMoving
                                   ? ((Vector2)_targetPosition - (Vector2)transform.position).normalized
                                   : Vector2.zero;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!_isMoving) { _rb.linearVelocity = Vector2.zero; return; }
        if (HasArrived) { Stop(); return; }

        Vector2 dir = ((Vector2)_targetPosition - (Vector2)transform.position).normalized;

        Vector2 smartDirection = AvoidObstacles(dir);

       // _rb.linearVelocity = dir * moveSpeed;

        _rb.linearVelocity = smartDirection * moveSpeed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region API

    private Vector2 AvoidObstacles(Vector2 dir)
    {
        // Sensores de detecção
        RaycastHit2D hitCenter = Physics2D.CircleCast(transform.position, 0.3f, dir, detectionRange, obstacleLayer);

        if (hitCenter.collider != null)
        {
            // Se houver algo, tenta desviar usando as normais do objeto atingido
            Vector2 leftDir = Quaternion.Euler(0, 0, sideSensorAngle) * dir;
            Vector2 rightDir = Quaternion.Euler(0, 0, -sideSensorAngle) * dir;

            RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, leftDir, detectionRange, obstacleLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightDir, detectionRange, obstacleLayer);

            if (hitLeft.collider == null) return leftDir;
            if (hitRight.collider == null) return rightDir;

            // Se ambos lados têm obstáculos, usa a normal da colisão para se afastar
            return Vector2.Reflect(dir, hitCenter.normal);
        }

        return dir;
    }

    public void MoveTo(Vector2 target)
    {
        _targetPosition = target;
        _isMoving = true;
    }

    public void Stop()
    {
        _isMoving = false;
        _rb.linearVelocity = Vector2.zero;
    }

    #endregion
}
