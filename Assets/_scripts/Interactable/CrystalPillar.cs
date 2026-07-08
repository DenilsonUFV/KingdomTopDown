using System.Collections;
using UnityEngine;

/// <summary>
/// Pilar de Cristal invocador de BOTs.
///
/// TAP no botão Interact    → pegar / soltar o pilar
/// SEGURAR InteractHold     → PillarInteractionHandler lança recursos e chama ReceiveResource()
///
/// Cada pilar invoca apenas um tipo de BOT definido por CrystalPillarData.
/// </summary>
public class CrystalPillar : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Dados")]
    [SerializeField] private CrystalPillarData data;

    [Header("Carregamento")]
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, 0.7f, 0f);

     [Header("Offset da Construção")]
    [SerializeField] private Vector3 build_offset = new Vector3(0f, 0.2f, 0f);

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private enum PillarState { Idle, Cooldown, BeingCarried }

    private PillarState _state = PillarState.Idle;
    private int         _resourcesInvested;
    private bool        _isOnCooldown;
    private bool        _isSpawning;   // aguardando delay antes do spawn
    private Transform   _carrier;
    private Coroutine   _cooldownRoutine;
    private Coroutine   _refundRoutine;
    private Coroutine   _spawnRoutine;
    private PillarUI    _ui;

    public CrystalPillarData Data => data;

    #endregion

    // ─────────────────────────────────────────
    #region IInteractable

    public bool     CanInteract  => true;
    public ToolType RequiredTool => ToolType.None;

    public string InteractionHint
    {
        get
        {
            if (data == null) return "";
            return _state switch
            {
                PillarState.BeingCarried => $"Soltar {data.pillarName}",
                PillarState.Cooldown     => $"{data.pillarName} — recarga",
                _ => _resourcesInvested > 0
                    ? $"[{_resourcesInvested}/{data.resourceCost}]  Tap: Pegar"
                    : $"Segurar: {data.resourceType} ×{data.resourceCost}  |  Tap: Pegar"
            };
        }
    }

    public bool Interact(GameObject interactor)
    {
        if (_state == PillarState.BeingCarried)
        {
            PutDown();
            return true;
        }

        PickUp(interactor.transform);
        return true;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _ui = GetComponentInChildren<PillarUI>();
    }

    private void Start()
    {
        if (data != null)
            _ui?.Build(data.resourceIcon, data.resourceCost);
    }

    private void Update()
    {
        if (_state == PillarState.BeingCarried && _carrier != null)
            transform.position = _carrier.position + carryOffset;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Carregamento

    private void PickUp(Transform carrier)
    {
        _state   = PillarState.BeingCarried;
        _carrier = carrier;
        InteractionSystem.ForcedTarget = this;
    }

    private void PutDown()
    {
        _carrier = null;
        _state   = _isOnCooldown ? PillarState.Cooldown : PillarState.Idle;
        InteractionSystem.ForcedTarget = null;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Recebimento de Recursos

    public bool CanReceiveResource() =>
        data != null && !_isOnCooldown && !_isSpawning && _state != PillarState.BeingCarried;

    /// <summary>
    /// Chamado pelo efeito de voo ao chegar no pilar.
    /// Deduz o recurso e avança o progresso.
    /// </summary>
    public void ReceiveResource()
    {
        if (!CanReceiveResource()) return;
        if (!ResourceManager.Has(data.resourceType, 1)) return;

        // Cancela reembolso pendente quando um recurso chega
        if (_refundRoutine != null) { StopCoroutine(_refundRoutine); _refundRoutine = null; }

        ResourceManager.Spend(data.resourceType, 1);
        _resourcesInvested++;
        _ui?.SetFilled(_resourcesInvested);

        if (_resourcesInvested >= data.resourceCost)
        {
            _isSpawning = true;
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SpawnBotDelayed());
        }
    }

    /// <summary>
    /// Inicia contagem regressiva para reembolsar os recursos investidos.
    /// Chamado pelo PillarInteractionHandler quando o jogador para de segurar.
    /// </summary>
    public void StartRefundTimer(float delay = 3f)
    {
        if (_resourcesInvested <= 0) return;
        if (_refundRoutine != null) StopCoroutine(_refundRoutine);
        _refundRoutine = StartCoroutine(RefundRoutine(delay));
    }

    private IEnumerator RefundRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_resourcesInvested <= 0) yield break;

        SpawnRefundResources();
        _resourcesInvested = 0;
        _ui?.SetFilled(0);
    }

    private void SpawnRefundResources()
    {
        if (data == null || data.refundPrefab == null) return;
        for (int i = 0; i < _resourcesInvested; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Instantiate(data.refundPrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Invocação

    private IEnumerator SpawnBotDelayed()
    {
        // Ícones todos preenchidos — aguarda o delay antes de spawnar
        yield return new WaitForSeconds(data.spawnDelay);
        _spawnRoutine = null;
        _isSpawning   = false;
        SpawnBot();
    }

    private void SpawnBot()
    {
        _resourcesInvested = 0;
        _ui?.SetFilled(0);
        _ui?.SetVisible(false);

        if (data.spawnPrefab != null){
            Debug.Log("PRE INSTANCIADO");
            Instantiate(data.spawnPrefab, transform.position + build_offset, Quaternion.identity);
            Debug.Log("POS INSTANCIADO");
        }
        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _isOnCooldown = true;
        if (_state != PillarState.BeingCarried)
            _state = PillarState.Cooldown;

        yield return new WaitForSeconds(data.spawnCooldown);

        _isOnCooldown    = false;
        _cooldownRoutine = null;
        if (_state != PillarState.BeingCarried)
            _state = PillarState.Idle;

        _ui?.SetFilled(0);
        _ui?.SetVisible(true);
    }

    #endregion
}
