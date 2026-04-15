using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingInteractionHandler : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private int playerIndex = 0;

    [Header("Moedas")]
    [SerializeField] private float coinInterval = 1f;
    [SerializeField] private Sprite coinSprite;           // ← arraste o sprite da moeda
    [SerializeField] private float coinFlySpeed = 4f;
    [SerializeField] private float coinArcHeight = 0.8f;

    [Header("Reembolso")]
    [SerializeField] private float refundDelay = 3f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private InputAction _interactAction;
    private Building _targetBuilding;
    private float _coinTimer = 0f;
    private bool _isHolding = false;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        SetupInput();
    }

    private void OnEnable()
    {
        _interactAction?.Enable();
        if (_interactAction != null)
        {
            _interactAction.performed += OnHoldStarted;
            _interactAction.canceled += OnHoldCanceled;
        }
    }

    private void OnDisable()
    {
        if (_interactAction != null)
        {
            _interactAction.performed -= OnHoldStarted;
            _interactAction.canceled -= OnHoldCanceled;
        }
        _interactAction?.Disable();
        StopHolding();
    }

    private void Update()
    {
        if (!_isHolding || _targetBuilding == null) return;

        _coinTimer += Time.deltaTime;

        if (_coinTimer >= coinInterval)
        {
            _coinTimer = 0f;
            TrySendCoin();
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Input Setup

    private void SetupInput()
    {
        if (inputActionAsset == null) return;

        string mapName = playerIndex == 0 ? "Player1" : "Player2";
        InputActionMap map = inputActionAsset.FindActionMap(mapName, throwIfNotFound: false);
        if (map == null) return;

        _interactAction = map.FindAction("Interact", throwIfNotFound: false);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Hold Logic

    private void OnHoldStarted(InputAction.CallbackContext ctx)
    {
        Debug.Log("OnHoldStarted");
        _targetBuilding = FindNearestBuilding();

        if (_targetBuilding == null) return;
        if (!_targetBuilding.CanReceiveCoins()) return;

        _isHolding = true;
        _coinTimer = coinInterval;  // primeira moeda imediata
    }

    private void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        Debug.Log("OnHoldCanceled " + _targetBuilding);
        // Inicia reembolso se parou antes de completar
        if (_targetBuilding != null /*&& _targetBuilding.CoinsInvested > 0*/)
        {
            Debug.Log($"[BuildingInteraction] Parou de investir — reembolso em {refundDelay}s");
            _targetBuilding.StartRefundTimer(refundDelay);
        }

        StopHolding();
    }

    private void StopHolding()
    {
        _isHolding = false;
        _targetBuilding = null;
        _coinTimer = 0f;
    }

    private void TrySendCoin()
    {
        if (_targetBuilding == null || !_targetBuilding.CanReceiveCoins())
        {
            StopHolding();
            return;
        }

        // Verifica saldo antes de voar
        if (!ResourceManager.Has(ResourceType.Coin, 1))
        {
            Debug.Log("[BuildingInteraction] Sem moedas!");
            StopHolding();
            return;
        }

        Vector3 from = transform.position;
        Vector3 to = _targetBuilding.transform.position;

        Building buildingRef = _targetBuilding;
        buildingRef._state = BuildinState.WaitingBuilder;

        // Voa a moeda com sprite correto
        CoinFlyEffect.Spawn(
            from,
            to,
            coinSprite,
            onArrive: () => buildingRef.ReceiveCoin(),
            flySpeed: coinFlySpeed,
            arcHeight: coinArcHeight
        );
    }

    #endregion

    // ─────────────────────────────────────────
    #region Detecção

    private Building FindNearestBuilding()
    {
        Collider2D[] buffer = new Collider2D[8];

        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            1.5f,
            buffer,
            LayerMask.GetMask("Interactable")
        );

        Building best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (buffer[i] == null) continue;

            Building b = buffer[i].GetComponent<Building>();
            if (b == null || !b.CanReceiveCoins()) continue;

            float dist = Vector2.Distance(transform.position, buffer[i].transform.position);
            if (dist < bestDist) { bestDist = dist; best = b; }
        }

        return best;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    #endregion
}
