using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionSystem : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Detecção")]
    [SerializeField] private float interactRadius = 1.2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private int playerIndex = 0;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    private PlayerInventory _inventory;
    private PlayerMovement  _playerMovement;
    private PlayerAnimator  _playerAnimator;
    private InputAction     _interactAction;

    public IInteractable CurrentTarget { get; private set; }

    /// <summary>
    /// Quando preenchido (ex.: pilar carregado), é retornado como alvo prioritário
    /// independentemente de distância ou collider.
    /// </summary>
    public static IInteractable ForcedTarget { get; set; }

    private static readonly Collider2D[] _buffer = new Collider2D[8];

    // ── Detecção tap vs hold ──────────────────
    // Tap = press + release em menos de tapThreshold segundos.
    // Hold = segurado além do threshold → ignorado pelo InteractionSystem
    //        (tratado pela action InteractHold no PillarInteractionHandler).
    [Header("Tap")]
    [Tooltip("Tempo máximo (segundos) para considerar um press como tap.")]
    [SerializeField] private float tapThreshold = 0.28f;

    private float _pressTime = -1f;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _inventory      = GetComponent<PlayerInventory>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        SetupInput();
    }

    private void OnEnable()
    {
        _interactAction?.Enable();
        if (_interactAction != null)
        {
            _interactAction.started  += OnInteractStarted;
            _interactAction.canceled += OnInteractCanceled;
        }

        if (_playerAnimator != null)
            _playerAnimator.OnActionAnimationEnd += OnActionAnimationEnd;
    }

    private void OnDisable()
    {
        if (_interactAction != null)
        {
            _interactAction.started  -= OnInteractStarted;
            _interactAction.canceled -= OnInteractCanceled;
        }
        _interactAction?.Disable();

        if (_playerAnimator != null)
            _playerAnimator.OnActionAnimationEnd -= OnActionAnimationEnd;

        _playerMovement?.UnlockMovement();
    }

    private void Update()
    {
        CurrentTarget = FindBestInteractable();
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
    #region Detecção

    private IInteractable FindBestInteractable()
    {
        // Objeto forçado (ex.: pilar carregado) tem prioridade absoluta
        if (ForcedTarget != null && ForcedTarget.CanInteract)
            return ForcedTarget;

        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            interactRadius,
            _buffer,
            interactableLayer
        );

        IInteractable best     = null;
        float         bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (_buffer[i] == null) continue;

            IInteractable interactable = _buffer[i].GetComponent<IInteractable>();
            if (interactable == null || !interactable.CanInteract) continue;

            float dist = Vector2.Distance(transform.position, _buffer[i].transform.position);
            if (dist < bestDist) { bestDist = dist; best = interactable; }
        }

        return best;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Interação

    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        _pressTime = Time.time;
    }

    private void OnInteractCanceled(InputAction.CallbackContext ctx)
    {
        if (_pressTime >= 0f && (Time.time - _pressTime) < tapThreshold)
            ExecuteTap();

        _pressTime = -1f;
    }

    private void ExecuteTap()
    {
        IInteractable target = FindBestInteractable();
        if (target == null) return;

        Vector3 targetPosition = ((MonoBehaviour)target).transform.position;
        bool actionExecuted;

        if (target.RequiredTool == ToolType.None)
            actionExecuted = target.Interact(gameObject);
        else if (_inventory.HasTool(target.RequiredTool))
            actionExecuted = target.Interact(gameObject);
        else
        {
            Debug.Log($"[Interaction] Precisa de: {target.RequiredTool}");
            return;
        }

        if (actionExecuted)
        {
            // Só adiciona um lock se não há nenhum ativo.
            // Golpes consecutivos na mesma árvore/rocha não acumulam contadores —
            // um trigger de animação já em andamento nunca garante que OnActionEnd
            // dispara N vezes, então mantemos o lock count em no máximo 1.
            if (target.RequiredTool != ToolType.None && !(_playerMovement?.IsLocked ?? false))
                _playerMovement?.LockMovement();
            _playerAnimator?.PlayActionAnimation(target.RequiredTool, targetPosition);
        }
    }

    private void OnActionAnimationEnd()
    {
        _playerMovement?.UnlockMovement();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    #endregion
}
