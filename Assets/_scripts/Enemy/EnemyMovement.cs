using UnityEngine;

/// <summary>
/// Movimento baseado em Rigidbody2D para inimigos e BOTs defensores.
/// Suporta desvio inteligente de obstáculos e suavização de direção.
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
    private Vector2     _targetPosition;
    private bool        _isMoving;
    private float       _moveSpeed = 2f;
    private Vector2     _smoothDir;          // suaviza mudanças bruscas de direção
    private bool        _isKnockedBack;
    private Coroutine   _knockbackRoutine;

    public bool    HasArrived => Vector2.Distance(transform.position, _targetPosition) <= arriveRadius;
    public bool    IsMoving   => _isMoving;
    /// <summary>Direção de movimento suavizada — usada pelo animador.</summary>
    public Vector2 MoveInput  => _isMoving ? _smoothDir : Vector2.zero;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb                  = GetComponent<Rigidbody2D>();
        _rb.gravityScale     = 0f;
        _rb.freezeRotation   = true;
        _rb.linearDamping    = 8f;   // amortece micro-forças do physics engine
    }

    private void FixedUpdate()
    {
        if (_isKnockedBack) return;
        if (!_isMoving) { _rb.linearVelocity = Vector2.zero; return; }
        if (HasArrived) { Stop(); return; }

        Vector2 dir      = ((Vector2)_targetPosition - (Vector2)transform.position).normalized;
        Vector2 smartDir = AvoidObstacles(dir);

        // Suaviza a direção para eliminar oscilação frame-a-frame
        _smoothDir = Vector2.Lerp(_smoothDir, smartDir, 0.3f).normalized;
        _rb.linearVelocity = _smoothDir * _moveSpeed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Inicialização

    /// <summary>Chamado pelo Brain com a velocidade configurada.</summary>
    public void Init(float speed) => _moveSpeed = speed;

    #endregion

    // ─────────────────────────────────────────
    #region API

    public void MoveTo(Vector2 target)
    {
        _targetPosition = target;
        _isMoving       = true;
    }

    public void Knockback(Vector2 impulse, float duration = 0.18f)
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(impulse, ForceMode2D.Impulse);
        if (_knockbackRoutine != null) StopCoroutine(_knockbackRoutine);
        _knockbackRoutine = StartCoroutine(KnockbackRoutine(duration));
    }

    private System.Collections.IEnumerator KnockbackRoutine(float duration)
    {
        _isKnockedBack = true;
        yield return new WaitForSeconds(duration);
        _isKnockedBack    = false;
        _knockbackRoutine = null;
    }

    public void Stop()
    {
        _isMoving          = false;
        _smoothDir         = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Congela completamente o Rigidbody — usado na morte para impedir que
    /// colisões externas movam o corpo enquanto a animação de morte toca.
    /// </summary>
    public void Freeze()
    {
        Stop();
        _rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Desvio de Obstáculos

    private Vector2 AvoidObstacles(Vector2 dir)
    {
        RaycastHit2D hitCenter = Physics2D.CircleCast(transform.position, 0.3f, dir, detectionRange, obstacleLayer);
        if (hitCenter.collider == null) return dir;

        Vector2 leftDir  = Quaternion.Euler(0, 0,  sideSensorAngle) * (Vector3)dir;
        Vector2 rightDir = Quaternion.Euler(0, 0, -sideSensorAngle) * (Vector3)dir;

        RaycastHit2D hitLeft  = Physics2D.Raycast(transform.position, leftDir,  detectionRange, obstacleLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, rightDir, detectionRange, obstacleLayer);

        if (hitLeft.collider  == null) return leftDir;
        if (hitRight.collider == null) return rightDir;

        // Ambos bloqueados — desliza pela normal, escolhendo o sentido mais útil
        Vector2 slideA = Vector2.Perpendicular(hitCenter.normal);
        Vector2 slideB = -slideA;
        return Vector2.Dot(slideA, dir) >= 0f ? slideA : slideB;
    }

    #endregion
}
