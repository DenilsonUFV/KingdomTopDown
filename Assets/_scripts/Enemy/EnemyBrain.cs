using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Máquina de estados do inimigo.
///
/// Fluxo:
///   Patrulhando → (detecta alvo) → Perseguindo → (entra em range) → Atacando
///   Atacando → (alvo foge) → Perseguindo
///   Qualquer estado → (amanheceu) → Recuando → destruído silenciosamente
///   Qualquer estado → (vida = 0) → Morto → drop loot → destruído
///
/// Componentes obrigatórios no mesmo GameObject:
///   EnemyMovement, EnemyHealth, EnemyTargetScanner
///   + UM dos: EnemyMeleeAttack ou EnemyRangedAttack (implementam IAttack)
/// </summary>
public class EnemyBrain : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Dados do Inimigo")]
    [SerializeField] private EnemyData data;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private EnemyMovement       _movement;
    private EnemyHealth         _health;
    private EnemyTargetScanner  _scanner;
    private IAttack             _attack;
    private EnemyAnimator       _animator;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private EnemyState      _state;
    private Transform       _currentTarget;
    private EnemySpawnPoint _spawnPoint;
    private Coroutine       _stateRoutine;
    private bool            _isDaytime = false;

    public EnemyState State  => _state;
    public bool       IsDead => _state == EnemyState.Morto;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _health   = GetComponent<EnemyHealth>();
        _scanner  = GetComponent<EnemyTargetScanner>();
        _attack   = GetComponent<IAttack>();
        _animator = GetComponent<EnemyAnimator>();
    }

    private void Start()
    {
        if (data != null)
        {
            _health.Init(data.maxHealth);
            _movement.Init(data.moveSpeed);
            _scanner.Init(data);
        }
        else
        {
            Debug.LogWarning($"[EnemyBrain] {gameObject.name} não tem EnemyData configurado!");
            _health.Init(10);
            _movement.Init(2f);
        }

        _health.OnDeath += HandleDeath;
        EnemyManager.Instance?.Register(this);

        EnterState(EnemyState.Patrulhando);
    }

    private void OnEnable()
    {
        DayNightCycle.OnDayStarted   += OnDayStarted;
        DayNightCycle.OnNightStarted += OnNightStarted;
        Star.OnDropped               += OnStarDropped;
    }

    private void OnDisable()
    {
        DayNightCycle.OnDayStarted   -= OnDayStarted;
        DayNightCycle.OnNightStarted -= OnNightStarted;
        Star.OnDropped               -= OnStarDropped;
    }

    private void Update()
    {
        if (_state != EnemyState.Morto && _health != null && _health.IsDead)
            HandleDeath();

        // CarregandoEstrela está indo ao spawner — deixa terminar mesmo de dia
        if (_isDaytime && _state != EnemyState.Morto
                       && _state != EnemyState.Recuando
                       && _state != EnemyState.CarregandoEstrela)
            OrderRetreat();
    }

    private void OnDestroy()
    {
        EnemyManager.Instance?.Unregister(this);
        _spawnPoint?.NotifyEnemyRemoved(this);
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
            EnemyState.Idle              => StartCoroutine(IdleRoutine()),
            EnemyState.Patrulhando       => StartCoroutine(PatrolRoutine()),
            EnemyState.Perseguindo       => StartCoroutine(ChaseRoutine()),
            EnemyState.Atacando          => StartCoroutine(AttackRoutine()),
            EnemyState.Recuando          => StartCoroutine(RetreatRoutine()),
            EnemyState.BuscandoEstrela   => StartCoroutine(BuscandoEstrelaRoutine()),
            EnemyState.CarregandoEstrela => StartCoroutine(CarregandoEstrelaRoutine()),
            EnemyState.Morto             => StartCoroutine(DeathRoutine()),
            _ => null
        };
    }

    #endregion

    // ─────────────────────────────────────────
    #region Idle

    private IEnumerator IdleRoutine()
    {
        _movement.Stop();
        float idleMin = data != null ? data.idleTimeMin : 1f;
        float idleMax = data != null ? data.idleTimeMax : 3f;
        yield return new WaitForSeconds(Random.Range(idleMin, idleMax));
        EnterState(EnemyState.Patrulhando);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Patrulha

    private IEnumerator PatrolRoutine()
    {
        Vector2 center = _spawnPoint != null
            ? (Vector2)_spawnPoint.SpawnPosition
            : (Vector2)transform.position;

        float radius = data != null ? data.patrolRadius : 5f;
        _movement.MoveTo(center + Random.insideUnitCircle * radius);

        while (!_movement.HasArrived)
        {
            // Escaneia alvos enquanto patrulha
            Transform target = _scanner.FindBestTarget();
            if (target != null)
            {
                _currentTarget = target;
                EnterState(EnemyState.Perseguindo);
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
        }

        EnterState(EnemyState.Idle);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Perseguição

    private IEnumerator ChaseRoutine()
    {
        float range = data != null ? data.attackRange : 1.2f;

        while (true)
        {
            // Atualiza alvo a cada tick (pode mudar de alvo se aparecer um mais prioritário)
            Transform newTarget = _scanner.FindBestTarget();
            if (newTarget != null) _currentTarget = newTarget;

            if (_currentTarget == null)
            {
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            // Checa se alvo morreu
            if (IsTargetDead(_currentTarget))
            {
                _currentTarget = null;
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            Vector2 targetCenter = CombatUtils.GetCenter(_currentTarget);
            float dist = Vector2.Distance(transform.position, targetCenter);
            if (dist <= range)
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
        float range    = data != null ? data.attackRange    : 1.2f;
        float cooldown = data != null ? data.attackCooldown : 1.5f;
        int   damage   = data != null ? data.attackDamage   : 1;

        while (true)
        {
            if (_currentTarget == null || IsTargetDead(_currentTarget))
            {
                _currentTarget = null;
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            float dist = Vector2.Distance(transform.position, CombatUtils.GetCenter(_currentTarget));

            // Alvo fugiu — volta a perseguir
            if (dist > range * 1.3f)
            {
                EnterState(EnemyState.Perseguindo);
                yield break;
            }

            // Dispara animação e executa o ataque
            _animator?.PlayAttackAnimation(_currentTarget.position);
            _attack?.PerformAttack(_currentTarget, damage);

            yield return new WaitForSeconds(cooldown);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Recuo (amanhece)

    private IEnumerator RetreatRoutine()
    {
        _currentTarget = null;

        Vector2 retreatPos = _spawnPoint != null
            ? (Vector2)_spawnPoint.SpawnPosition
            : (Vector2)transform.position;

        _movement.MoveTo(retreatPos);

        float timeout = 30f;
        float elapsed = 0f;

        while (!_movement.HasArrived)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) break;
            yield return null;
        }

        // Chegou ao ponto de spawn — some sem drop de loot
        Destroy(gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Morte

    private void HandleDeath()
    {
        // Se morrer carregando a Estrela, ela cai no chão novamente
        if (_state == EnemyState.CarregandoEstrela)
            Star.Instance?.ForceDropAt(transform.position);

        EnterState(EnemyState.Morto);
    }

    private IEnumerator DeathRoutine()
    {
        _movement.Freeze();
        _animator?.PlayDeathAnimation();

        // Drop de loot
        DropLoot();

        // Aguarda animação de morte (sem animação por enquanto, apenas 1s)
        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }

    private void DropLoot()
    {
        if (data?.lootTable == null) return;

        List<LootEntry> loot = data.lootTable.Roll();
        foreach (LootEntry entry in loot)
        {
            if (entry?.collectiblePrefab == null) continue;
            // Roll() já define minAmount == maxAmount com o valor rolado
            for (int i = 0; i < entry.minAmount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.4f;
                Instantiate(entry.collectiblePrefab,
                    (Vector2)transform.position + offset,
                    Quaternion.identity);
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Define o ponto de spawn de origem deste inimigo.</summary>
    public void SetSpawnPoint(EnemySpawnPoint point) => _spawnPoint = point;

    /// <summary>Chamado ao amanhecer — inicia recuo. Não interrompe quem já está entregando a Estrela.</summary>
    public void OrderRetreat()
    {
        if (_state == EnemyState.Morto || _state == EnemyState.CarregandoEstrela) return;
        EnterState(EnemyState.Recuando);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Handlers Dia/Noite e Estrela

    private void OnDayStarted()
    {
        _isDaytime = true;
        OrderRetreat();
    }

    private void OnNightStarted()
    {
        _isDaytime = false;
    }

    private void OnStarDropped()
    {
        if (_state == EnemyState.Morto || _state == EnemyState.Recuando) return;
        EnterState(EnemyState.BuscandoEstrela);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Estrela

    private IEnumerator BuscandoEstrelaRoutine()
    {
        while (true)
        {
            // Estrela foi recolhida por outro inimigo ou voltou ao player
            if (Star.Instance == null || !Star.Instance.IsDropped)
            {
                EnterState(EnemyState.Patrulhando);
                yield break;
            }

            _movement.MoveTo(Star.Instance.Position);

            if (_movement.HasArrived)
            {
                bool picked = Star.Instance.TryPickUp(transform);
                EnterState(picked ? EnemyState.CarregandoEstrela : EnemyState.Patrulhando);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator CarregandoEstrelaRoutine()
    {
        Vector2 deliveryPos = _spawnPoint != null
            ? (Vector2)_spawnPoint.SpawnPosition
            : (Vector2)transform.position;

        _movement.MoveTo(deliveryPos);

        float timeout = 30f;
        float elapsed = 0f;

        while (!_movement.HasArrived)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout) break;
            yield return null;
        }

        Star.Instance?.Deliver();
        Destroy(gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private bool IsTargetDead(Transform target)
    {
        if (target == null) return true;
        IDamageable dmg = target.GetComponent<IDamageable>()
                       ?? target.GetComponentInParent<IDamageable>();
        return dmg == null || dmg.IsDead;
    }

    #endregion
}
