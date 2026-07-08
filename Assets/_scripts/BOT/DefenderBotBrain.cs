using System.Collections;
using UnityEngine;

/// <summary>
/// Máquina de estados do BOT defensor (melee ou arqueiro).
///
/// Fluxo normal:
///   Patrulhando → (detecta inimigo) → Perseguindo → (entra em range) → Atacando
///   Atacando → (inimigo morreu/fugiu) → Patrulhando
///
/// Fluxo de encaixe em torre (DefenderArcher + BotMountPoint):
///   Patrulhando/Idle → (BotMountPoint.OnBecameAvailable) → IndoParaBase → Montado
///   Montado → (torre destruída) → Patrulhando  (via Dismount())
///
/// Quando Montado:
///   - Colliders desabilitados → inimigos não detectam o BOT via Physics2D
///   - Movimento travado na posição do encaixe
///   - Apenas atira nos inimigos dentro do raio de detecção
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

    [Header("Encaixe em Construção")]
    [Tooltip("Distância do ponto de encaixe para considerar chegada.")]
    [SerializeField] private float mountArrivalThreshold = 0.5f;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private EnemyMovement       _movement;
    private DefenderBotHealth   _health;
    private IAttack             _attack;
    private DefenderBotAnimator _animator;
    private Collider2D[]        _colliders;
    private SpriteRenderer[]    _renderers;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private EnemyState    _state;
    private Transform     _currentTarget;
    private Coroutine     _stateRoutine;
    private Vector2       _homePosition;
    private BotMountPoint _mountPoint;

    public EnemyState State  => _state;
    public bool       IsDead => _state == EnemyState.Morto;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _movement  = GetComponent<EnemyMovement>();
        _health    = GetComponent<DefenderBotHealth>();
        _attack    = GetComponent<IAttack>();
        _animator  = GetComponent<DefenderBotAnimator>();
        _colliders = GetComponents<Collider2D>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _homePosition = transform.position;
        _movement.Init(moveSpeed);
        _health.OnDeath += HandleDeath;

        if (!TrySeekNearestMount())
            EnterState(EnemyState.Patrulhando);
    }

    private void OnEnable()
    {
        BotMountPoint.OnBecameAvailable += HandleMountAvailable;
    }

    private void OnDisable()
    {
        BotMountPoint.OnBecameAvailable -= HandleMountAvailable;
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
            EnemyState.Idle         => StartCoroutine(IdleRoutine()),
            EnemyState.Patrulhando  => StartCoroutine(PatrolRoutine()),
            EnemyState.Perseguindo  => StartCoroutine(ChaseRoutine()),
            EnemyState.Atacando     => StartCoroutine(AttackRoutine()),
            EnemyState.IndoParaBase => StartCoroutine(GoToMountRoutine()),
            EnemyState.Montado      => StartCoroutine(MountedRoutine()),
            EnemyState.Morto        => StartCoroutine(DeathRoutine()),
            _                       => null
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

    private Vector2 GetPatrolPoint()
    {
        Vector2 dir    = Random.insideUnitCircle.normalized;
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
    #region Encaixe em Construção

    private bool TrySeekNearestMount()
    {
        BotMountPoint[] all     = FindObjectsByType<BotMountPoint>(FindObjectsSortMode.None);
        BotMountPoint   nearest = null;
        float           bestDist = float.MaxValue;

        foreach (BotMountPoint mount in all)
        {
            if (!mount.IsAvailable) continue;
            float dist = Vector2.Distance(transform.position, mount.MountWorldPosition);
            if (dist < bestDist) { bestDist = dist; nearest = mount; }
        }

        if (nearest == null) return false;
        _mountPoint = nearest;
        EnterState(EnemyState.IndoParaBase);
        return true;
    }

    private void HandleMountAvailable(BotMountPoint mount)
    {
        // Responde apenas se estiver livre e sem destino de mount já definido
        if (_state != EnemyState.Patrulhando && _state != EnemyState.Idle) return;
        if (_mountPoint != null) return;

        _mountPoint = mount;
        EnterState(EnemyState.IndoParaBase);
    }

    private IEnumerator GoToMountRoutine()
    {
        if (_mountPoint == null || !_mountPoint.IsAvailable)
        {
            _mountPoint = null;
            EnterState(EnemyState.Patrulhando);
            yield break;
        }

        while (true)
        {
            // Mount ficou indisponível enquanto caminhava (outro BOT chegou primeiro)
            if (_mountPoint == null || !_mountPoint.IsAvailable)
            {
                _mountPoint = null;
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            _movement.MoveTo(_mountPoint.MountWorldPosition);

            if (Vector2.Distance(transform.position, _mountPoint.MountWorldPosition) <= mountArrivalThreshold)
            {
                _movement.Stop();

                if (_mountPoint.TryMount(this))
                {
                    SetCollidersEnabled(false);
                    SetSortingLayer("MountedBot");
                    transform.position = _mountPoint.MountWorldPosition;
                    EnterState(EnemyState.Montado);
                }
                else
                {
                    // Outro BOT chegou no mesmo instante e ganhou o mutex
                    _mountPoint = null;
                    EnterState(EnemyState.Patrulhando);
                }
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MountedRoutine()
    {
        while (true)
        {
            // Posição travada no encaixe (torre pode se mover via animação, etc.)
            if (_mountPoint != null)
                transform.position = _mountPoint.MountWorldPosition;

            Transform enemy = FindNearestEnemy();
            if (enemy != null)
            {
                _animator?.PlayAttackAnimation(enemy.position);
                _attack?.PerformAttack(enemy, attackDamage);
            }

            yield return new WaitForSeconds(attackCooldown);
        }
    }

    /// <summary>
    /// Chamado pelo BotMountPoint quando a construção é destruída.
    /// Restaura o BOT ao estado normal de patrulha.
    /// </summary>
    public void Dismount()
    {
        SetCollidersEnabled(true);
        SetSortingLayer("Dynamic");
        _mountPoint?.Vacate(this);
        _mountPoint = null;

        if (_state != EnemyState.Morto)
            EnterState(EnemyState.Patrulhando);
    }

    private void SetCollidersEnabled(bool value)
    {
        foreach (Collider2D col in _colliders)
            if (col != null) col.enabled = value;
    }

    private void SetSortingLayer(string layerName)
    {
        int id = SortingLayer.NameToID(layerName);
        foreach (SpriteRenderer sr in _renderers)
            if (sr != null) sr.sortingLayerID = id;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Morte

    private void HandleDeath()
    {
        if (_mountPoint != null)
        {
            SetCollidersEnabled(true);
            SetSortingLayer("Dynamic");
            _mountPoint.Vacate(this);
            _mountPoint = null;
        }
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

    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        Transform nearest     = null;
        float     nearestDist = float.MaxValue;

        foreach (Collider2D col in hits)
        {
            EnemyBrain enemy = col.GetComponent<EnemyBrain>();
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector2.Distance(transform.position, col.transform.position);
            if (dist < nearestDist) { nearestDist = dist; nearest = col.transform; }
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
