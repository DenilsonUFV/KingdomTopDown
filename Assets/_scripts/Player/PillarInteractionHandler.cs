using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Componente do player que gerencia a interação de "segurar" com o Pilar de Cristal.
/// Usa a action "InteractHold" (igual ao BuildingInteractionHandler com moedas).
///
/// Enquanto segurado perto de um pilar:
///   - Lança fisicamente o recurso exigido (animação de voo via CoinFlyEffect)
///   - Ao chegar, CrystalPillar.ReceiveResource() é chamado
///
/// Ao soltar sem completar:
///   - Inicia timer de reembolso no pilar (recursos são jogados de volta ao mundo)
/// </summary>
public class PillarInteractionHandler : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private int playerIndex = 0;

    [Header("Lançamento")]
    [SerializeField] private float resourceInterval = 0.8f;
    [SerializeField] private float flySpeed         = 5f;
    [SerializeField] private float arcHeight        = 0.8f;

    [Header("Reembolso")]
    [SerializeField] private float refundDelay = 3f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private InputAction   _interactHoldAction;
    private CrystalPillar _targetPillar;
    private float         _timer;
    private bool          _isHolding;

    private static readonly Collider2D[] _buffer = new Collider2D[8];

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        SetupInput();
    }

    private void OnEnable()
    {
        _interactHoldAction?.Enable();
        if (_interactHoldAction != null)
        {
            _interactHoldAction.performed += OnHoldStarted;
            _interactHoldAction.canceled  += OnHoldCanceled;
        }
    }

    private void OnDisable()
    {
        if (_interactHoldAction != null)
        {
            _interactHoldAction.performed -= OnHoldStarted;
            _interactHoldAction.canceled  -= OnHoldCanceled;
        }
        _interactHoldAction?.Disable();
        StopHolding();
    }

    private void Update()
    {
        if (!_isHolding || _targetPillar == null) return;

        _timer += Time.deltaTime;
        if (_timer >= resourceInterval)
        {
            _timer = 0f;
            TrySendResource();
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

        _interactHoldAction = map.FindAction("InteractHold", throwIfNotFound: false);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Hold Logic

    private void OnHoldStarted(InputAction.CallbackContext ctx)
    {
        _targetPillar = FindNearestPillar();
        if (_targetPillar == null) return;

        _isHolding = true;
        _timer     = resourceInterval; // primeiro lançamento imediato
    }

    private void OnHoldCanceled(InputAction.CallbackContext ctx)
    {
        if (_targetPillar != null)
            _targetPillar.StartRefundTimer(refundDelay);

        StopHolding();
    }

    private void StopHolding()
    {
        _isHolding    = false;
        _targetPillar = null;
        _timer        = 0f;
    }

    private void TrySendResource()
    {
        if (_targetPillar == null || !_targetPillar.CanReceiveResource())
        {
            StopHolding();
            return;
        }

        CrystalPillarData data = _targetPillar.Data;
        if (data == null || data.resourceIcon == null) return;

        if (!ResourceManager.Has(data.resourceType, 1))
        {
            StopHolding();
            return;
        }

        Vector3       from       = transform.position;
        Vector3       to         = _targetPillar.transform.position;
        CrystalPillar pillarRef  = _targetPillar;

        CoinFlyEffect.Spawn(
            from,
            to,
            data.resourceIcon,
            onArrive:  () => pillarRef.ReceiveResource(),
            flySpeed:  flySpeed,
            arcHeight: arcHeight
        );
    }

    #endregion

    // ─────────────────────────────────────────
    #region Detecção

    private CrystalPillar FindNearestPillar()
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            1.5f,
            _buffer,
            LayerMask.GetMask("Interactable")
        );

        CrystalPillar best     = null;
        float         bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (_buffer[i] == null) continue;

            CrystalPillar p = _buffer[i].GetComponent<CrystalPillar>();
            if (p == null || !p.CanReceiveResource()) continue;

            float dist = Vector2.Distance(transform.position, _buffer[i].transform.position);
            if (dist < bestDist) { bestDist = dist; best = p; }
        }

        return best;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    #endregion
}
