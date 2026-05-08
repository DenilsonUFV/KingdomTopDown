using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Building : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Dados")]
    [SerializeField] protected BuildingData currentData;

    [Header("Referências")]
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private BuildingUI     _ui;
    [SerializeField] private bool startBuilt;
    #endregion

    // ─────────────────────────────────────────
    #region Estado


    [SerializeField] private BuildingState  _state = BuildingState.Destroyed;
    private int            _coinsInvested;
    private bool           _isRepair;    // true = reparo de dano
    private bool           _isUpgrade;   // true = evolução para nextLevel
    private Coroutine      _refundRoutine;
    private Coroutine      _buildRoutine;
    private BuildingHealth _health;

    private readonly List<BotBrain> _activeBuilders = new();

    public int           ActiveBuilderCount      => _activeBuilders.Count;
    public BuildingState State                   => _state;
    public BuildingData  Data                    => currentData;
    public int           CoinsInvested           => _coinsInvested;
    public int           TargetCost              => _isRepair  ? (currentData != null ? currentData.RepairCost : 0)
                                                  : _isUpgrade ? (currentData != null && currentData.nextLevel != null ? currentData.nextLevel.coinCost : 0)
                                                  : (currentData != null ? currentData.coinCost : 0);
    public int           CoinsRemaining          => TargetCost - _coinsInvested;
    public float         distanceToStartBuilding = 1f;
    public bool          HasNextLevel            => currentData != null && currentData.nextLevel != null;
    public bool          IsAtFullHealth          => _health == null || _health.CurrentHealth >= _health.MaxHealth;

    public event Action<Building>        OnFullyFunded;
    public event Action<Building>        OnBuilt;
    public static event Action<Building> OnAnyBuildingFullyFunded;

    #endregion

    // ─────────────────────────────────────────
    #region IInteractable

    public bool     CanInteract  => CanReceiveCoins();
    public ToolType RequiredTool => ToolType.None;

    public string InteractionHint
    {
        get
        {
            if (currentData == null) return "";
            return _state switch
            {
                BuildingState.Destroyed        => $"Construir {currentData.buildingName} ({currentData.coinCost} moedas)",
                BuildingState.WaitingFunds     => $"{currentData.buildingName} [{_coinsInvested}/{TargetCost}]",
                BuildingState.WaitingBuilder   => "Aguardando BOT...",
                BuildingState.UnderConstruction => _isRepair ? "Reparando..." : _isUpgrade ? "Evoluindo..." : "Construindo...",
                BuildingState.Built when _health != null && _health.CurrentHealth < _health.MaxHealth
                                               => $"Reparar {currentData.buildingName} ({currentData.RepairCost} moedas)",
                BuildingState.Built when currentData.nextLevel != null
                                               => $"Evoluir: {currentData.nextLevel.buildingName} ({currentData.nextLevel.coinCost} moedas)",
                _ => ""
            };
        }
    }

    public bool Interact(GameObject interactor) => false;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    protected virtual void Awake()
    {
        _sr    ??= GetComponent<SpriteRenderer>();
        _ui    ??= GetComponentInChildren<BuildingUI>();
        _health  = GetComponent<BuildingHealth>();

        if (_health != null)
        {
            _health.OnDamaged   += OnHealthDamaged;
            _health.OnDestroyed += OnHealthDestroyed;
        }
    }

    protected virtual void Start()
    {
        RefreshVisual();
        _ui?.Refresh(this);

        if(!startBuilt)
            _state = BuildingState.Destroyed;
    }

    private void OnDestroy()
    {
        if (_health == null) return;
        _health.OnDamaged   -= OnHealthDamaged;
        _health.OnDestroyed -= OnHealthDestroyed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Dano & Destruição

    private void OnHealthDamaged(float normalized)
    {
        if (_state == BuildingState.Built)
            ApplyDamageTint(normalized);
        _ui?.Refresh(this);
    }

    private void OnHealthDestroyed()
    {
        if (_buildRoutine  != null) { StopCoroutine(_buildRoutine);  _buildRoutine  = null; }
        if (_refundRoutine != null) { StopCoroutine(_refundRoutine); _refundRoutine = null; }

        foreach (BotBrain bot in _activeBuilders.ToList())
            bot.ReleaseBuilding();
        _activeBuilders.Clear();

        _coinsInvested = 0;
        _isRepair      = false;
        _isUpgrade     = false;
        _state         = BuildingState.Destroyed;

        ShowDestroyedVisual();
        _ui?.Refresh(this);
    }

    // > 66% HP → branco | 33–66% → laranja | < 33% → vermelho
    private void ApplyDamageTint(float normalized)
    {
        if (_sr == null) return;
        _sr.color = normalized > 0.66f
            ? Color.white
            : normalized > 0.33f
                ? new Color(1f, 0.65f, 0.2f)
                : new Color(1f, 0.25f, 0.25f);
    }

    private void ShowDestroyedVisual()
    {
        if (_sr == null) return;
        if (currentData?.spriteSlot != null)
        {
            _sr.sprite = currentData.spriteSlot;
            UpdatePolygonCollider();
        }
        _sr.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Recebimento de Moedas

    public bool CanReceiveCoins()
    {
        if (_state == BuildingState.WaitingBuilder)    return false;
        if (_state == BuildingState.UnderConstruction) return false;
        if (_state == BuildingState.Destroyed)         return true;
        if (_state == BuildingState.WaitingFunds)      return true;
        if (_state == BuildingState.Built && _health != null && _health.CurrentHealth < _health.MaxHealth)
            return true;
        if (_state == BuildingState.Built && currentData != null && currentData.nextLevel != null)
            return true;
        return false;
    }

    public bool ReceiveCoin()
    {
        if (!CanReceiveCoins()) return false;
        if (!ResourceManager.Has(ResourceType.Coin, 1)) return false;

        if (_state == BuildingState.Built || _state == BuildingState.Destroyed)
        {
            bool damaged  = _state == BuildingState.Built && _health != null && _health.CurrentHealth < _health.MaxHealth;
            _isRepair      = damaged;
            _isUpgrade     = !damaged && _state == BuildingState.Built && currentData != null && currentData.nextLevel != null;
            _coinsInvested = 0;
            _state         = BuildingState.WaitingFunds;
        }

        ResourceManager.Spend(ResourceType.Coin, 1);
        _coinsInvested++;

        if (_refundRoutine != null) { StopCoroutine(_refundRoutine); _refundRoutine = null; }

        _ui?.Refresh(this);

        if (_coinsInvested >= TargetCost)
            OnFundingComplete();

        return true;
    }

    public void StartRefundTimer(float delay = 3f)
    {
        if (_state != BuildingState.WaitingFunds) return;
        if (_refundRoutine != null) StopCoroutine(_refundRoutine);
        _refundRoutine = StartCoroutine(RefundRoutine(delay));
    }

    private IEnumerator RefundRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_state != BuildingState.WaitingFunds || _coinsInvested <= 0) yield break;

        SpawnRefundCoins();
        _coinsInvested = 0;

        if (_isRepair || _isUpgrade)
        {
            _state = BuildingState.Built;
            if (_isRepair && _health != null)
                ApplyDamageTint((float)_health.CurrentHealth / _health.MaxHealth);
        }
        else
        {
            _state = BuildingState.Destroyed;
            ShowDestroyedVisual();
        }

        _isRepair  = false;
        _isUpgrade = false;
        _ui?.Refresh(this);
    }

    private void SpawnRefundCoins()
    {
        if (CoinSpawnHelper.Instance == null) return;
        for (int i = 0; i < _coinsInvested; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
            CoinSpawnHelper.Instance.SpawnCoin(transform.position + (Vector3)offset);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Financiamento & Construção

    public void ForceStateToWaitingBuilder()
    {
        _state = BuildingState.WaitingBuilder;
    }

    private void OnFundingComplete()
    {
        BuildingData targetData = _isUpgrade && currentData.nextLevel != null
            ? currentData.nextLevel
            : currentData;

        _state = targetData.needsBuilder
            ? BuildingState.WaitingBuilder
            : BuildingState.UnderConstruction;

        _ui?.Refresh(this);
        OnFullyFunded?.Invoke(this);
        OnAnyBuildingFullyFunded?.Invoke(this);

        if (targetData.needsBuilder)
        {
            distanceToStartBuilding = GetMaxRadius(_sr) + 1f;
            BotManager.Instance?.RequestBuilders(this);
        }
        else
            StartCoroutine(BuildRoutine());
    }

    public void StartConstruction(BotBrain bot)
    {
        if (_state != BuildingState.WaitingBuilder) return;
        _state = BuildingState.UnderConstruction;
        _ui?.Refresh(this);

        if (_buildRoutine != null) StopCoroutine(_buildRoutine);
        _buildRoutine = StartCoroutine(BuildRoutine());
    }

    private IEnumerator BuildRoutine()
    {
        BuildingData targetData = _isUpgrade && currentData.nextLevel != null
            ? currentData.nextLevel
            : currentData;

        float totalTime = _isRepair ? currentData.RepairTime : targetData.buildTime;
        float elapsed   = 0f;

        if (targetData.spriteBuilding != null)
        {
            _sr.sprite = targetData.spriteBuilding;
            _sr.color  = Color.white;
            UpdatePolygonCollider();
        }

        _ui?.StartProgress(totalTime);

        while (elapsed < totalTime)
        {
            float speedMult = 1f + (_activeBuilders.Count - 1) * 0.3f;
            elapsed += Time.deltaTime * speedMult;
            _ui?.UpdateProgressManual(elapsed / totalTime);
            yield return null;
        }

        _ui?.StopProgress();
        CompleteBuild();
    }

    private void CompleteBuild()
    {
        foreach (BotBrain bot in _activeBuilders.ToList())
            bot.ReleaseBuilding();
        _activeBuilders.Clear();

        _coinsInvested = 0;

        if (_isUpgrade && currentData.nextLevel != null)
            currentData = currentData.nextLevel;

        _isRepair  = false;
        _isUpgrade = false;
        _state     = BuildingState.Built;

        _health?.FullRepair();
        RefreshVisual();

        _ui?.Refresh(this);
        OnBuilt?.Invoke(this);

        if (currentData.buildingName == "Tenda")
            BotSpawner.Instance?.SpawnBuilderBot(transform.position);
    }

    public void RegisterBuilder(BotBrain bot)
    {
        if (!_activeBuilders.Contains(bot))
            _activeBuilders.Add(bot);
    }

    public void UnregisterBuilder(BotBrain bot)
    {
        _activeBuilders.Remove(bot);
    }

    private float GetMaxRadius(SpriteRenderer sr)
    {
        if (sr?.sprite == null) return 0f;
        Vector3 e = sr.sprite.bounds.extents;
        return Mathf.Max(e.x, e.y);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Visual

    private void RefreshVisual()
    {
        if (_sr == null || currentData == null) return;

        if (_state == BuildingState.Destroyed)
        {
            ShowDestroyedVisual();
            return;
        }

        if (currentData.spriteBuilt != null)
        {
            _sr.sprite = currentData.spriteBuilt;
            _sr.color  = Color.white;
            UpdatePolygonCollider();
        }
    }

    private void UpdatePolygonCollider()
    {
        Destroy(GetComponent<PolygonCollider2D>());
        gameObject.AddComponent<PolygonCollider2D>();
    }

    #endregion
}
