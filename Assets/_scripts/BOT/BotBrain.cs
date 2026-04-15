using System.Collections;
using UnityEngine;

public class BotBrain : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Perambulação")]
    [SerializeField] private float wanderRadius = 10f;   // raio de perambulação
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 3f;
    [SerializeField] private float wanderInterval = 0.5f;  // tempo entre tentativas de novo ponto

    [Header("Construção")]
    [SerializeField] private float buildCheckRadius = 1f;  // distância para começar a construir
    [SerializeField] private LayerMask obstacleLayer;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private BotMovement _movement;
    private BotAnimator _botAnimator;
    private BotHealth _health;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private BotState _state = BotState.Idle;
    private Building _targetBuilding;
    private Building _pendingBuilding; // próxima construção após terminar a atual
    private Coroutine _stateRoutine;

    public BotState State => _state;
    public Building TargetBuilding => _targetBuilding;

    // Disponível = ocioso ou perambulando (não construindo)
    public bool IsAvailable => _state == BotState.Idle
                            || _state == BotState.Wandering;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _movement = GetComponent<BotMovement>();
        _botAnimator = GetComponent<BotAnimator>();
        _health = GetComponent<BotHealth>();
    }

    private void Start()
    {
        BotManager.Instance?.Register(this);
        EnterState(BotState.Wandering);
    }

    private void OnDestroy()
    {
        BotManager.Instance?.Unregister(this);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Máquina de Estados

    private void EnterState(BotState newState)
    {
        if (_stateRoutine != null)
            StopCoroutine(_stateRoutine);

        _state = newState;

        _stateRoutine = newState switch
        {
            BotState.Idle => StartCoroutine(IdleRoutine()),
            BotState.Wandering => StartCoroutine(WanderRoutine()),
            BotState.GoingToBuild => StartCoroutine(GoToBuildRoutine()),
            BotState.Building => StartCoroutine(BuildingRoutine()),
            BotState.Dead => StartCoroutine(DeathRoutine()),
            _ => null
        };
    }

    #endregion

    // ─────────────────────────────────────────
    #region Idle

    private IEnumerator IdleRoutine()
    {
        _movement.Stop();
        float idleTime = Random.Range(idleTimeMin, idleTimeMax);
        yield return new WaitForSeconds(idleTime);
        EnterState(BotState.Wandering);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Wandering

    private IEnumerator WanderRoutine()
    {
        Vector2 destination = GetRandomWanderPoint();
        _movement.MoveTo(destination);

        while (!_movement.HasArrived)
        {
            // Verifica obstáculo na trajetória — recalcula se necessário
            if (IsPathBlocked())
            {
                destination = GetRandomWanderPoint();
                _movement.MoveTo(destination);
            }

            yield return new WaitForSeconds(wanderInterval);
        }

        EnterState(BotState.Idle);
    }

    private Vector2 GetRandomWanderPoint()
    {
        // Tenta até 10 vezes achar um ponto sem obstáculo
        for (int i = 0; i < 10000; i++)
        {
            Vector2 randomPoint = (Vector2)transform.position
                                + Random.insideUnitCircle * wanderRadius;

            // Verifica se o ponto não está dentro de um obstáculo
            if (!Physics2D.OverlapCircle(randomPoint, 0.3f, obstacleLayer))
            {
                //Debug.Log("RANDOM POINT "+randomPoint);
                return randomPoint;
            }
        }

        // Fallback — fica parado
        return transform.position;
    }

    private bool IsPathBlocked()
    {
        Vector2 dir = (_movement.MoveInput).normalized;
        float dist = 1f;

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            1f,
            dir,
            dist,
            obstacleLayer
        );

        return hit.collider != null;
    }

    #endregion

    // ─────────────────────────────────────────
    #region GoToBuild

    private IEnumerator GoToBuildRoutine()
    {
        if (_targetBuilding == null) { EnterState(BotState.Wandering); yield break; }

        _movement.MoveTo(_targetBuilding.transform.position);

        while (true)
        {
            // Construção foi cancelada ou destruída
            if (_targetBuilding == null)
            {
                EnterState(BotState.Wandering);
                yield break;
            }

            buildCheckRadius = _targetBuilding.distanceToStartBuilding;
            
            float dist = Vector2.Distance(transform.position, _targetBuilding.transform.position);

            if (dist <= buildCheckRadius)
            {
                _movement.Stop();
                EnterState(BotState.Building);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Building

    private IEnumerator BuildingRoutine()
    {
        if (_targetBuilding == null) { EnterState(BotState.Wandering); yield break; }

        _targetBuilding.RegisterBuilder(this);
        _targetBuilding.StartConstruction(this);

        // Toca UMA vez ao chegar
        _botAnimator?.PlayBuildAnimation(_targetBuilding.transform.position);

        // Aguarda terminar sem ficar resetando a animação
        while (_targetBuilding != null
            && _targetBuilding.State == BuildingState.UnderConstruction)
        {
            yield return null;
        }
        // Para a animação assim que termina
        _botAnimator?.StopBuildAnimation();

        if (_targetBuilding != null)
            _targetBuilding.UnregisterBuilder(this);

        _targetBuilding = null;

        if (_pendingBuilding != null)
        {
            _targetBuilding = _pendingBuilding;
            _pendingBuilding = null;
            EnterState(BotState.GoingToBuild);
        }
        else
        {
            EnterState(BotState.Wandering);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Death

    private IEnumerator DeathRoutine()
    {
        _movement.Stop();
        _botAnimator?.PlayDeathAnimation();

        // Aguarda animação de morte terminar
        yield return new WaitForSeconds(1.5f);

        Destroy(gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Chamado pelo BotManager para assignar uma construção.
    /// </summary>
    public void AssignBuilding(Building building)
    {
        if (_state == BotState.Building)
        {
            // Está construindo — guarda como pendente para depois
            _pendingBuilding = building;
            Debug.Log($"[BotBrain] Ocupado — {building.Data?.buildingName} guardado como pendente.");
            return;
        }

        _targetBuilding = building;
        _pendingBuilding = null;
        EnterState(BotState.GoingToBuild);
    }

    /// <summary>
    /// Chamado quando a construção é cancelada ou concluída.
    /// </summary>
    public void ReleaseBuilding()
    {
        if (_targetBuilding != null)
            _targetBuilding.UnregisterBuilder(this);

        Debug.Log("2AQUIIIIIIIIIIIIIIIIIIIII-----------------------------------------------");
        // Para a animação assim que termina
        _botAnimator?.StopBuildAnimation();

        _targetBuilding = null;

        if (_pendingBuilding != null)
        {
            _targetBuilding = _pendingBuilding;
            _pendingBuilding = null;
            EnterState(BotState.GoingToBuild);
        }
        else
        {
            EnterState(BotState.Wandering);
        }

       // _targetBuilding = null;
       // _pendingBuilding = null;
        //if (_state != BotState.Dead)
        //    EnterState(BotState.Wandering);
    }

    /// <summary>
    /// Chamado pelo BotHealth ao morrer.
    /// </summary>
    public void OnDeath()
    {
        if (_targetBuilding != null)
        {
            _targetBuilding.UnregisterBuilder(this);
            _targetBuilding = null;
        }

        EnterState(BotState.Dead);
    }

    #endregion
}
