using System;
using UnityEngine;

/// <summary>
/// Fonte única de verdade sobre "em que fase do jogo estamos".
///
/// GameStateManager COMANDA — DayNightCycle obedece.
/// Quando o estado muda para Dia ou Noite, GameStateManager chama DayNightCycle.ForcePhase(),
/// que dispara os mesmos eventos do ciclo natural (OnDayStarted / OnNightStarted).
/// Todos os outros sistemas (EnemySpawnPoint, DayNightLighting, EnemyManager, etc.)
/// já escutam esses eventos e reagem sem precisar de alteração.
///
/// O timer do DayNightCycle ainda funciona normalmente — quando esgota, notifica
/// GameStateManager, que processa a transição e comanda de volta.
///
/// [DefaultExecutionOrder(100)] garante que Start() roda APÓS todos os outros,
/// então DayNightCycle.Instance já existe e seu estado inicial já foi aplicado.
/// </summary>
[DefaultExecutionOrder(100)]
public class GameStateManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static GameStateManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // Remova a linha abaixo se o projeto não usa troca de cena
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Configuração

    [Header("Estado ao entrar em cena")]
    [Tooltip("GameStateManager sobrepõe o 'Start At Day' do DayNightCycle. " +
             "Se Noite, os spawn points já começam spawnando.")]
    [SerializeField] private GameState estadoInicial = GameState.Dia;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private GameState _currentState;
    private GameState _estadoAntesPause;
    private float     _timeScaleAntesPause = 1f;

    /// <summary>Estado atual do jogo. Somente leitura externamente.</summary>
    public GameState CurrentState => _currentState;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos Estáticos

    /// <summary>Disparado sempre que o estado muda. Parâmetros: (anterior, novo).</summary>
    public static event Action<GameState, GameState> OnStateChanged;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Start()
    {
        // _currentState permanece como default (MenuInicial) durante Awake/OnEnable
        // para que o HandleDayStarted disparado pelo DayNightCycle.Start() seja ignorado.
        // Agora, com todos os Start() já executados, aplicamos o estado real.
        _currentState = estadoInicial;
        Debug.Log($"[GameState] Iniciado em: {_currentState}");

        // Sincroniza o DayNightCycle com o estado inicial escolhido no Inspector
        SincronizarDayNight(_currentState);
    }

    private void OnEnable()
    {
        // Timer do DayNightCycle → notifica GameStateManager → GameStateManager comanda de volta
        DayNightCycle.OnDayStarted   += HandleDayStarted;
        DayNightCycle.OnNightStarted += HandleNightStarted;
        Star.OnLost                  += HandleStarLost;
    }

    private void OnDisable()
    {
        DayNightCycle.OnDayStarted   -= HandleDayStarted;
        DayNightCycle.OnNightStarted -= HandleNightStarted;
        Star.OnLost                  -= HandleStarLost;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Handlers de Eventos Externos

    // Timer do DayNightCycle esgotou — GameStateManager processa e comanda o ciclo completo
    private void HandleDayStarted()
    {
        // Ignora durante inicialização (state = MenuInicial) e estados não-noite
        if (_currentState == GameState.Noite)
            ChangeState(GameState.Dia);
    }

    private void HandleNightStarted()
    {
        if (_currentState == GameState.Dia)
            ChangeState(GameState.Noite);
    }

    // Inimigo entregou a Estrela ao spawn — sessão encerrada
    private void HandleStarLost()
    {
        ChangeState(GameState.GameOver);
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Transiciona para o novo estado, validando a transição e sincronizando todos os sistemas.
    /// Gerencia Time.timeScale automaticamente para Pausado e GameOver.
    /// </summary>
    public void ChangeState(GameState novoEstado)
    {
        if (novoEstado == _currentState) return;

        if (!IsTransicaoPermitida(_currentState, novoEstado))
        {
            Debug.LogWarning($"[GameState] Transição inválida: {_currentState} → {novoEstado}");
            return;
        }

        GameState anterior = _currentState;

        // — Efeitos de SAÍDA do estado anterior —
        if (_currentState == GameState.Pausado)
            Time.timeScale = _timeScaleAntesPause;

        if (_currentState == GameState.GameOver)
            Time.timeScale = 1f;

        // — Efeitos de ENTRADA no novo estado —
        if (novoEstado == GameState.Pausado)
        {
            _estadoAntesPause    = _currentState;
            _timeScaleAntesPause = Time.timeScale;
            Time.timeScale       = 0f;
        }

        if (novoEstado == GameState.GameOver)
            Time.timeScale = 0f;

        // — Atualiza estado e notifica — deve acontecer antes de SincronizarDayNight
        // para que os handlers do OnStateChanged já vejam o novo estado
        _currentState = novoEstado;
        Debug.Log($"[GameState] {anterior} → {novoEstado}");
        OnStateChanged?.Invoke(anterior, novoEstado);

        // — Sincroniza DayNightCycle com o novo estado —
        // ForcePhase dispara OnDayStarted/OnNightStarted, acionando EnemySpawnPoints,
        // DayNightLighting, EnemyManager, EnemyBrain, etc.
        SincronizarDayNight(novoEstado);
    }

    /// <summary>
    /// Alterna entre Pausado e o estado anterior (Dia ou Noite).
    /// Uso: botão de pausa — não exige que o chamador conheça o estado atual.
    /// </summary>
    public void TogglePause()
    {
        if (_currentState == GameState.Pausado)
            ChangeState(_estadoAntesPause);
        else if (_currentState == GameState.Dia || _currentState == GameState.Noite)
            ChangeState(GameState.Pausado);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Sincronização com DayNightCycle

    /// <summary>
    /// Força o DayNightCycle a refletir o estado do jogo.
    /// Só age quando necessário — ForcePhase já guarda contra re-entrada no mesmo estado.
    /// </summary>
    private void SincronizarDayNight(GameState estado)
    {
        if (DayNightCycle.Instance == null) return;

        switch (estado)
        {
            case GameState.Dia:
                // ForcePhase(true) só age se DayNightCycle não está em dia — sem loop
                DayNightCycle.Instance.ForcePhase(true);
                break;

            case GameState.Noite:
                DayNightCycle.Instance.ForcePhase(false);
                break;

            // Pausado e GameOver congelam via timeScale — DayNightCycle congela junto
            // MenuInicial não altera o ciclo
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Transições Permitidas

    private static bool IsTransicaoPermitida(GameState de, GameState para) => (de, para) switch
    {
        (GameState.MenuInicial, GameState.Dia)         => true,
        (GameState.MenuInicial, GameState.Noite)       => true,  // início direto na noite
        (GameState.Dia,         GameState.Noite)       => true,
        (GameState.Noite,       GameState.Dia)         => true,
        (GameState.Dia,         GameState.Pausado)     => true,
        (GameState.Noite,       GameState.Pausado)     => true,
        (GameState.Pausado,     GameState.Dia)         => true,
        (GameState.Pausado,     GameState.Noite)       => true,
        (GameState.Dia,         GameState.GameOver)    => true,
        (GameState.Noite,       GameState.GameOver)    => true,
        (GameState.GameOver,    GameState.MenuInicial) => true,
        _ => false
    };

    #endregion
}
