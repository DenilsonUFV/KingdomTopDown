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
    [SerializeField] private float coinInterval = 1f;   // 1 moeda por segundo

    [Header("Reembolso")]
    [SerializeField] private float refundDelay = 3f;   // segundos para reembolsar

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

        // Usa a mesma action de Interact — mas lê o canceled também
        _interactAction = map.FindAction("Interact", throwIfNotFound: false);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Hold Logic

    private void OnHoldStarted(InputAction.CallbackContext ctx)
    {
        // Verifica se tem Building no raio
        _targetBuilding = FindNearestBuilding();
        if (_targetBuilding == null) return;
        if (!_targetBuilding.CanReceiveCoins()) return;

        _isHolding = true;
        _coinTimer = coinInterval; // solta a primeira moeda imediatamente
    }

    private void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        if (_targetBuilding != null)
            _targetBuilding.StartRefundTimer(refundDelay);

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

        // Voa a moeda visualmente
        CoinFlyEffect.Spawn(
            transform.position,
            _targetBuilding.transform.position,
            onArrive: () => _targetBuilding.ReceiveCoin()
        );
    }

    #endregion

    // ─────────────────────────────────────────
    #region Detecção

    private Building FindNearestBuilding()
    {
        Collider2D[] buffer = new Collider2D[8];
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, 1.5f, buffer,
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
}
