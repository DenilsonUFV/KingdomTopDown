using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BotMovement : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Movimento")]
    [SerializeField] private float moveSpeed    = 2f;
    [SerializeField] private float arriveRadius = 0.2f;

    [Header("Context Steering")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Distância de detecção de obstáculos.")]
    [SerializeField] private float detectionRange = 1.2f;
    [Tooltip("Raio do agente — deve aproximar o tamanho do collider.")]
    [SerializeField] private float bodyRadius     = 0.25f;
    [Tooltip("Quantidade de raios no arco de detecção. Mais raios = navegação mais suave.")]
    [Range(5, 16)]
    [SerializeField] private int   rayCount  = 9;
    [Tooltip("Abertura total do arco de detecção em graus.")]
    [Range(60f, 180f)]
    [SerializeField] private float sensorArc = 120f;

    [Header("Anti-Travamento")]
    [Tooltip("Tempo parado (s) antes de aplicar empurrão de desengajamento.")]
    [SerializeField] private float stuckDetectTime = 0.6f;
    [Tooltip("Força do empurrão perpendicular ao detectar travamento.")]
    [SerializeField] private float stuckPushForce  = 4f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Rigidbody2D _rb;
    private Vector2     _targetPosition;
    private bool        _isMoving;
    private Vector2     _smoothDir;          // direção suavizada para evitar micro-jitter

    // Detecção de travamento
    private Vector2 _lastCheckedPos;
    private float   _stuckTimer;
    private bool    _stuckPushRight = true;  // alterna lado a cada empurrão

    private bool      _isKnockedBack;
    private Coroutine _knockbackRoutine;

    public bool    IsMoving   => _isMoving;
    public bool    HasArrived => Vector2.Distance(transform.position, _targetPosition) <= arriveRadius;

    /// <summary>Direção de movimento atual (usada pelo animador).</summary>
    public Vector2 MoveInput  => _isMoving ? _smoothDir : Vector2.zero;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb                = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
        _lastCheckedPos    = transform.position;
    }

    private void FixedUpdate()
    {
        if (_isKnockedBack) return;
        if (!_isMoving) { _rb.linearVelocity = Vector2.zero; return; }
        if (HasArrived) { Stop(); return; }

        Vector2 desiredDir = ((Vector2)_targetPosition - (Vector2)transform.position).normalized;
        Vector2 steerDir   = ContextSteer(desiredDir);

        // Suaviza a direção para eliminar jitter frame-a-frame
        _smoothDir = Vector2.Lerp(_smoothDir, steerDir, 0.35f).normalized;
        _rb.linearVelocity = _smoothDir * moveSpeed;

        DetectAndUnstick(desiredDir);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Context Steering

    /// <summary>
    /// Distribui raios num arco e escolhe a direção livre que mais aponta
    /// para o destino. Fallback: desliza pela normal do obstáculo.
    /// </summary>
    private Vector2 ContextSteer(Vector2 desiredDir)
    {
        // Caminho direto livre — segue sem desvio
        if (!Physics2D.CircleCast(transform.position, bodyRadius, desiredDir, detectionRange, obstacleLayer))
            return desiredDir;

        // Distribui rayCount raios no arco e pontua cada um
        Vector2 bestDir   = Vector2.zero;
        float   bestScore = float.MinValue;
        float   halfArc   = sensorArc * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float t         = rayCount > 1 ? (float)i / (rayCount - 1) : 0.5f;
            float angle     = Mathf.Lerp(-halfArc, halfArc, t);
            Vector2 candDir = (Quaternion.Euler(0f, 0f, angle) * (Vector3)desiredDir).normalized;

            // Usa raio menor para os sensores laterais — mais sensível a passagens estreitas
            bool blocked = Physics2D.CircleCast(
                transform.position, bodyRadius * 0.5f, candDir, detectionRange, obstacleLayer
            );
            if (blocked) continue;

            // Pontuação: quanto essa direção aponta para o destino
            float score = Vector2.Dot(candDir, desiredDir);
            if (score > bestScore)
            {
                bestScore = score;
                bestDir   = candDir;
            }
        }

        // Todos os raios bloqueados → desliza pela normal do obstáculo central
        if (bestDir == Vector2.zero)
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position, bodyRadius, desiredDir, detectionRange, obstacleLayer
            );
            if (hit.collider != null)
            {
                // Escolhe o sentido perpendicular que mais aponta para o destino
                Vector2 slideA = Vector2.Perpendicular(hit.normal);
                Vector2 slideB = -slideA;
                bestDir = Vector2.Dot(slideA, desiredDir) >= 0f ? slideA : slideB;
            }
            else
            {
                bestDir = desiredDir;
            }
        }

        return bestDir;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Anti-Travamento

    private void DetectAndUnstick(Vector2 desiredDir)
    {
        _stuckTimer += Time.fixedDeltaTime;
        if (_stuckTimer < stuckDetectTime) return;

        float moved = Vector2.Distance(transform.position, _lastCheckedPos);

        // Limiar: menos de 5 % do movimento esperado no período = travado
        if (moved < 0.05f * stuckDetectTime * moveSpeed)
        {
            Vector2 push = Vector2.Perpendicular(desiredDir) * (_stuckPushRight ? 1f : -1f);
            _rb.AddForce(push * stuckPushForce, ForceMode2D.Impulse);
            _stuckPushRight = !_stuckPushRight; // alterna lado para próxima vez
        }

        _stuckTimer     = 0f;
        _lastCheckedPos = transform.position;
    }

    #endregion

    // ─────────────────────────────────────────
    #region API

    public void MoveTo(Vector2 target)
    {
        _targetPosition = target;
        _isMoving       = true;
    }

    public void Stop()
    {
        _isMoving          = false;
        _rb.linearVelocity = Vector2.zero;
        _smoothDir         = Vector2.zero;
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

    #endregion
}
