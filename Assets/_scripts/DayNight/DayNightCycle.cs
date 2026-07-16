using System;
using UnityEngine;

/// <summary>
/// Singleton que gerencia o ciclo de dia e noite.
/// Outros sistemas assinam os eventos estáticos para reagir à mudança de fase.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static DayNightCycle Instance { get; private set; }

    #endregion

    // ─────────────────────────────────────────
    #region Configuração

    [Header("Duração das Fases (segundos)")]
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private float nightDuration = 60f;

    [Header("Início")]
    [SerializeField] private bool startAtDay = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private bool _isDay;
    private float _timeInPhase;
    private int _dayCount = 0;

    public bool IsDay   => _isDay;
    public bool IsNight => !_isDay;

    /// <summary>Progresso da fase atual (0 = início, 1 = fim).</summary>
    public float PhaseProgress => _timeInPhase / (_isDay ? dayDuration : nightDuration);

    /// <summary>Número do dia atual (começa em 1).</summary>
    public int DayCount => _dayCount;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos Estáticos

    /// <summary>Disparado quando o dia começa.</summary>
    public static event Action OnDayStarted;

    /// <summary>Disparado quando a noite começa.</summary>
    public static event Action OnNightStarted;

    /// <summary>Disparado a cada frame com o progresso da fase (0–1).</summary>
    public static event Action<float> OnTimeUpdated;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _isDay = startAtDay;
        _timeInPhase = 0f;

        if (_isDay)
        {
            _dayCount = 1;
            if (showDebugLog) Debug.Log("[DayNight] Dia 1 começou!");
            OnDayStarted?.Invoke();
        }
        else
        {
            if (showDebugLog) Debug.Log("[DayNight] Noite começou!");
            OnNightStarted?.Invoke();
        }
    }

    private void Update()
    {
        _timeInPhase += Time.deltaTime;

        OnTimeUpdated?.Invoke(PhaseProgress);

        float currentDuration = _isDay ? dayDuration : nightDuration;
        if (_timeInPhase >= currentDuration)
        {
            _timeInPhase = 0f;
            TogglePhase();
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Lógica

    private void TogglePhase()
    {
        _isDay = !_isDay;

        if (_isDay)
        {
            _dayCount++;
            if (showDebugLog) Debug.Log($"[DayNight] Dia {_dayCount} começou!");
            OnDayStarted?.Invoke();
        }
        else
        {
            if (showDebugLog) Debug.Log($"[DayNight] Noite do dia {_dayCount} começou!");
            OnNightStarted?.Invoke();
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Pula imediatamente para a próxima fase. Útil para debug no Inspector.
    /// </summary>
    [ContextMenu("Avançar Fase")]
    public void SkipToNextPhase()
    {
        _timeInPhase = _isDay ? dayDuration : nightDuration;
    }

    /// <summary>
    /// Força a fase imediatamente, resetando o timer.
    /// Chamado pelo GameStateManager quando o estado do jogo muda.
    /// Dispara os mesmos eventos do ciclo natural — todos os sistemas reagem igualmente.
    /// </summary>
    public void ForcePhase(bool toDay)
    {
        if (_isDay == toDay) return;

        _isDay       = toDay;
        _timeInPhase = 0f;

        if (_isDay)
        {
            _dayCount++;
            if (showDebugLog) Debug.Log($"[DayNight] Forçado → Dia {_dayCount}");
            OnDayStarted?.Invoke();
        }
        else
        {
            if (showDebugLog) Debug.Log($"[DayNight] Forçado → Noite do dia {_dayCount}");
            OnNightStarted?.Invoke();
        }
    }

    #endregion
}
