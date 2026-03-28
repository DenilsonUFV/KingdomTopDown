using System;
using System.Collections;
using System.Collections.Generic;
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

    public BuildingState State => _state;
    public BuildingData Data => currentData;
    public int CoinsInvested => _coinsInvested;
    public int CoinsRemaining => currentData?.nextLevel != null
                                           ? currentData.nextLevel.coinCost - _coinsInvested
                                           : 0;

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

        // Cancela o timer de reembolso se estava contando
        if (_refundRoutine != null)
        {
            StopCoroutine(_refundRoutine);
            _refundRoutine = null;
        }

        _ui?.Refresh(this);

        int required = currentData.nextLevel.coinCost;

        if (_coinsInvested >= required)
            OnFundingComplete();

        return true;
    }

    /// <summary>
    /// Inicia o timer de reembolso — se o jogador parar de investir,
    /// as moedas caem no chão após alguns segundos.
    /// </summary>
    public void StartRefundTimer(float delay = 3f)
    {
        if (_coinsInvested <= 0) return;
        if (_state != BuildingState.WaitingFunds) return;

        if (_refundRoutine != null)
            StopCoroutine(_refundRoutine);

        _refundRoutine = StartCoroutine(RefundRoutine(delay));
    }

    private IEnumerator RefundRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_state != BuildingState.WaitingFunds) yield break;

        // Devolve as moedas no chão
        SpawnRefundCoins();
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

    private void OnFundingComplete()
    {
        _state = currentData.nextLevel.needsBuilder
            ? BuildingState.WaitingBuilder
            : BuildingState.UnderConstruction;

        _ui?.Refresh(this);

        OnFullyFunded?.Invoke(this);
        OnAnyBuildingFullyFunded?.Invoke(this);

        if (!currentData.nextLevel.needsBuilder)
            StartCoroutine(BuildRoutine());
    }

    /// <summary>
    /// Chamado pelo BuilderBot quando começa a construir.
    /// </summary>
    public void StartConstruction()
    {
        if (_state != BuildingState.WaitingBuilder) return;
        _state = BuildingState.UnderConstruction;
        _ui?.Refresh(this);
        StartCoroutine(BuildRoutine());
    }

    private IEnumerator BuildRoutine()
    {
        // Sprite de andaime
        if (currentData.nextLevel.spriteBuilding != null)
            _sr.sprite = currentData.nextLevel.spriteBuilding;

        yield return new WaitForSeconds(currentData.nextLevel.buildTime);

        CompleteBuild();
    }

    private void CompleteBuild()
    {
        BuildingData nextData = currentData.nextLevel;

        currentData = nextData;
        _coinsInvested = 0;
        _state = BuildingState.Built;

        RefreshVisual();
        SpawnChildSlots();

        _ui?.Refresh(this);
        OnBuilt?.Invoke(this);

        // Spawna BOT construtor ao evoluir de fogueira para tenda
        if (currentData.buildingName == "Tenda")
            BotSpawner.Instance?.SpawnBuilderBot(transform.position);

        Debug.Log($"[Building] {currentData.buildingName} construída!");
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
            _sr.sprite = currentData.spriteBuilt;
    }

    #endregion
}
