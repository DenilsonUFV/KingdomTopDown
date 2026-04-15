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
    [SerializeField] private BuildingUI _ui;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private BuildingState _state = BuildingState.Built;
    private int _coinsInvested = 0;
    private Coroutine _refundRoutine;

    private readonly List<BotBrain> _activeBuilders = new();
    public int ActiveBuilderCount => _activeBuilders.Count;

    public BuildingState State => _state;
    public BuildingData Data => currentData;
    public int CoinsInvested => _coinsInvested;
    public int CoinsRemaining => currentData?.nextLevel != null
                                           ? currentData.nextLevel.coinCost - _coinsInvested
                                           : 0;
    public float distanceToStartBuilding = 1f;

    // Eventos
    public event Action<Building> OnFullyFunded;
    public event Action<Building> OnBuilt;
    public static event Action<Building> OnAnyBuildingFullyFunded;

    #endregion

    // ─────────────────────────────────────────
    #region IInteractable

    public bool CanInteract => CanReceiveCoins();
    public ToolType RequiredTool => ToolType.None;
    public string InteractionHint => currentData?.nextLevel != null
                                       ? $"Construir {currentData.nextLevel.buildingName}"
                                       : "";
    public bool Interact(GameObject interactor) => false; // não usado — usa Hold

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    protected virtual void Awake()
    {
        _sr ??= GetComponent<SpriteRenderer>();
        _ui ??= GetComponentInChildren<BuildingUI>();
    }

    protected virtual void Start()
    {
        RefreshVisual();
        _ui?.Refresh(this);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Coin Reception

    public bool CanReceiveCoins()
    {
        if (currentData?.nextLevel == null) return false;
        if (_state == BuildingState.WaitingBuilder) return false;
        if (_state == BuildingState.UnderConstruction) return false;
        return true;
        // ✅ Aceita Built E WaitingFunds
    }

    /// <summary>
    /// Recebe 1 moeda. Retorna true se aceita.
    /// </summary>
    public bool ReceiveCoin()
    {
        if (!CanReceiveCoins()) return false;
        if (!ResourceManager.Has(ResourceType.Coin, 1)) return false;

        ResourceManager.Spend(ResourceType.Coin, 1);
        _coinsInvested++;

        // Muda estado para WaitingFunds ao receber primeira moeda
        if (_state == BuildingState.Built)
            _state = BuildingState.WaitingFunds;

        // Cancela o timer de reembolso ao receber moeda
        if (_refundRoutine != null)
        {
            StopCoroutine(_refundRoutine);
            _refundRoutine = null;
        }

        _ui?.Refresh(this);

        if (_coinsInvested >= currentData.nextLevel.coinCost)
            OnFundingComplete();

        return true;
    }

    /// <summary>
    /// Inicia o timer de reembolso — se o jogador parar de investir,
    /// as moedas caem no chão após alguns segundos.
    /// </summary>
    public void StartRefundTimer(float delay = 3f)
    {
        //if (_coinsInvested <= 0) return;

        Debug.Log("StartRefundTimer " + _state);

        // ✅ Aceita WaitingFunds também — não só Built
        if (_state != BuildingState.WaitingFunds) return;

        if (_refundRoutine != null)
            StopCoroutine(_refundRoutine);

        _refundRoutine = StartCoroutine(RefundRoutine(delay));
    }

    private IEnumerator RefundRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // ✅ Verifica WaitingFunds — não Built
        if (_state != BuildingState.WaitingFunds) yield break;
        if (_coinsInvested <= 0) yield break;

        SpawnRefundCoins();
        Debug.Log($"[Building] {_coinsInvested} moedas reembolsadas!");

        _coinsInvested = 0;
        _state = BuildingState.Built;

        _ui?.Refresh(this);

    }

    private void SpawnRefundCoins()
    {
        // Spawna moedas no chão para o jogador pegar novamente
        if (CoinSpawnHelper.Instance == null) return;

        for (int i = 0; i < _coinsInvested; i++)
            CoinSpawnHelper.Instance.SpawnCoin(transform.position);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Funding & Build

    public void ForceStateToWaitingBuilder()
    {
        _state = BuildingState.WaitingBuilder;
    }

    private void OnFundingComplete()
    {
        _state = currentData.nextLevel.needsBuilder
            ? BuildingState.WaitingBuilder
            : BuildingState.UnderConstruction;

        _ui?.Refresh(this);

        OnFullyFunded?.Invoke(this);
        OnAnyBuildingFullyFunded?.Invoke(this);

        if (currentData.nextLevel.needsBuilder)
        {
            distanceToStartBuilding = GetMaxRadius(_sr) + 1f;
            BotManager.Instance?.RequestBuilders(this); // ← notifica o BotManager
        }
        else
            StartCoroutine(BuildRoutine());
    }

    /// <summary>
    /// Chamado pelo BuilderBot quando começa a construir.
    /// </summary>
    public void StartConstruction(BotBrain bot)
    {
        if (_state != BuildingState.WaitingBuilder) return;
        _state = BuildingState.UnderConstruction;
        _ui?.Refresh(this);

        if (_buildRoutine != null) StopCoroutine(_buildRoutine);
        _buildRoutine = StartCoroutine(BuildRoutine());
    }

    private Coroutine _buildRoutine;

    private IEnumerator BuildRoutine()
    {
        float totalTime = currentData.nextLevel.buildTime;
        float elapsed = 0f;

        if (currentData.nextLevel.spriteBuilding != null)
        {
            _sr.sprite = currentData.nextLevel.spriteBuilding;
            UpdatePolygonCollider();
        }

        _ui?.StartProgress(totalTime);

        while (elapsed < totalTime)
        {
            float speedMultiplier = 1f + (_activeBuilders.Count - 1) * 0.3f;
            elapsed += Time.deltaTime * speedMultiplier;
            _ui?.UpdateProgressManual(elapsed / totalTime);
            yield return null;
        }

        _ui?.StopProgress();
        CompleteBuild();
    }

    private void CompleteBuild()
    {
        // Libera todos os BOTs
        foreach (BotBrain bot in _activeBuilders.ToList())
            bot.ReleaseBuilding();

        _activeBuilders.Clear();

        BuildingData nextData = currentData.nextLevel;
        currentData = nextData;
        _coinsInvested = 0;
        _state = BuildingState.Built;

        RefreshVisual();
        SpawnChildSlots();

        _ui?.Refresh(this);
        OnBuilt?.Invoke(this);

        if (currentData.buildingName == "Tenda")
            BotSpawner.Instance?.SpawnBuilderBot(transform.position);

        Debug.Log($"[Building] {currentData.buildingName} construída!");
    }

    /// <summary>
    /// BOT chegou e está construindo.
    /// Quanto mais BOTs, mais rápido.
    /// </summary>
    public void RegisterBuilder(BotBrain bot)
    {
        if (_activeBuilders.Contains(bot)) return;
        _activeBuilders.Add(bot);


        Debug.Log($"[Building] {bot.name} chegou. BOTs ativos: {_activeBuilders.Count}");
    }

    private float GetMaxRadius(SpriteRenderer sr)
    {
        if (sr.sprite == null) return 0f;

        // Extents é a metade do tamanho total (Size / 2)
        Vector3 extents = sr.sprite.bounds.extents;

        // Retorna o maior valor entre a metade da largura e a metade da altura
        return Mathf.Max(extents.x, extents.y);
    }

    public void UnregisterBuilder(BotBrain bot)
    {
        _activeBuilders.Remove(bot);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Slots & Visual

    private void SpawnChildSlots()
    {
        if (currentData.childSlots == null) return;

        foreach (BuildingSlotConfig config in currentData.childSlots)
        {
            Vector3 pos = transform.position + (Vector3)config.localOffset;
            BuildingSlot.Spawn(pos, config.defaultData);
        }
    }

    private void RefreshVisual()
    {
        if (_sr != null && currentData?.spriteBuilt != null)
        {
            _sr.sprite = currentData.spriteBuilt;
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
