using System.Collections;
using UnityEngine;

/// <summary>
/// Máquina de estados do BOT defensor (melee ou arqueiro).
///
/// Fluxo:
///   Patrulhando → (detecta inimigo) → Perseguindo → (entra em range) → Atacando
///   Atacando → (inimigo morreu/fugiu) → Patrulhando
///   Qualquer estado → (vida = 0) → Morto
///
/// Componentes obrigatórios no mesmo GameObject:
///   EnemyMovement, DefenderBotHealth
///   + UM dos: DefenderBotMeleeAttack ou DefenderBotArcherAttack (implementam IAttack)
/// </summary>
public class DefenderBotBrain : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Estatísticas")]
    [SerializeField] private float moveSpeed       = 2.5f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackRange     = 1.5f;
    [SerializeField] private int   attackDamage    = 3;
    [SerializeField] private float attackCooldown  = 1.2f;

    [Header("Patrulha")]
    [SerializeField] private float patrolRadius      = 6f;
    [Tooltip("Distância mínima do ponto de patrulha (evita ponto muito próximo).")]
    [SerializeField] private float patrolMinDistance = 2f;
    [Tooltip("Tempo máximo tentando chegar a um ponto de patrulha antes de desistir.")]
    [SerializeField] private float patrolTimeout     = 6f;
    [SerializeField] private float idleTimeMin       = 1f;
    [SerializeField] private float idleTimeMax       = 3f;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private EnemyMovement      _movement;
    private DefenderBotHealth  _health;
    private IAttack            _attack;
    private DefenderBotAnimator _animator;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private EnemyState  _state;
    private Transform   _currentTarget;
    private Coroutine   _stateRoutine;
    private Vector2     _homePosition;

    public EnemyState State  => _state;
    public bool       IsDead => _state == EnemyState.Morto;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health   = GetComponent<DefenderBotHealth>();
        _attack   = GetComponent<IAttack>();
        _animator = GetComponent<DefenderBotAnimator>();
    }

    private void Start()
    {
        _homePosition = transform.position;

        _movement.Init(moveSpeed);
        _health.OnDeath += HandleDeath;

        EnterState(EnemyState.Patrulhando);
    }

    private void Update()
    {
        if (_state != EnemyState.Morto && _health != null && _health.IsDead)
            HandleDeath();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Máquina de Estados

    private void EnterState(EnemyState newState)
    {
        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);

        _state = newState;

        _stateRoutine = newState switch
        {
            EnemyState.Idle        => StartCoroutine(IdleRoutine()),
            EnemyState.Patrulhando => StartCoroutine(PatrolRoutine()),
            EnemyState.Perseguindo => StartCoroutine(ChaseRoutine()),
            EnemyState.Atacando    => StartCoroutine(AttackRoutine()),
            EnemyState.Morto       => StartCoroutine(DeathRoutine()),
            _ => null
        };
    }

    #endregion

    // ─────────────────────────────────────────
    #region Idle

    private IEnumerator IdleRoutine()
    {
        _movement.Stop();
        yield return new WaitForSeconds(Random.Range(idleTimeMin, idleTimeMax));
        EnterState(EnemyState.Patrulhando);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Patrulha

    private IEnumerator PatrolRoutine()
    {
        _movement.MoveTo(GetPatrolPoint());

        float elapsed = 0f;

        while (!_movement.HasArrived)
        {
            elapsed += 0.2f;

            // Timeout: desiste e vai pro Idle se não conseguir chegar
            if (elapsed >= patrolTimeout) break;

            Transform enemy = FindNearestEnemy();
            if (enemy != null)
            {
                _currentTarget = enemy;
                EnterState(EnemyState.Perseguindo);
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        _movement.Stop();
        EnterState(EnemyState.Idle);
    }

    /// <summary>
    /// Sorteia um ponto de patrulha sempre entre patrolMinDistance e patrolRadius,
    /// garantindo que o bot realmente ande e não fique parado.
    /// </summary>
    private Vector2 GetPatrolPoint()
    {
        Vector2 dir    = Random.insideUnitCircle.normalized;   // direção aleatória (nunca zero)
        float   radius = Random.Range(patrolMinDistance, patrolRadius);
        return _homePosition + dir * radius;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Perseguição

    private IEnumerator ChaseRoutine()
    {
        while (true)
        {
            // Reavalia o alvo mais próximo a cada tick
            Transform newTarget = FindNearestEnemy();
            if (newTarget != null) _currentTarget = newTarget;

            if (_currentTarget == null || IsEnemyDead(_currentTarget))
            {
                _currentTarget = null;
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            Vector2 targetCenter = CombatUtils.GetCenter(_currentTarget);
            float dist = Vector2.Distance(transform.position, targetCenter);
            if (dist <= attackRange)
            {
                _movement.Stop();
                EnterState(EnemyState.Atacando);
                yield break;
            }

            _movement.MoveTo(targetCenter);
            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Ataque

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (_currentTarget == null || IsEnemyDead(_currentTarget))
            {
                _currentTarget = null;
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            float dist = Vector2.Distance(transform.position, CombatUtils.GetCenter(_currentTarget));
            if (dist > attackRange * 1.3f)
            {
                EnterState(EnemyState.Perseguindo);
                yield break;
            }

            _animator?.PlayAttackAnimation(_currentTarget.position);
            _attack?.PerformAttack(_currentTarget, attackDamage);
            yield return new WaitForSeconds(attackCooldown);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Morte

    private void HandleDeath()
    {
        EnterState(EnemyState.Morto);
    }

    private IEnumerator DeathRoutine()
    {
        _movement.Freeze();
        _animator?.PlayDeathAnimation();
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Scanner de Inimigos

    /// <summary>
    /// Encontra o EnemyBrain mais próximo dentro do raio de detecção.
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D col in hits)
        {
            EnemyBrain enemy = col.GetComponent<EnemyBrain>();
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = col.transform;
            }
        }

        return nearest;
    }

    private bool IsEnemyDead(Transform target)
    {
        if (target == null) return true;
        EnemyBrain enemy = target.GetComponent<EnemyBrain>();
        return enemy == null || enemy.IsDead;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    #endregion
}
