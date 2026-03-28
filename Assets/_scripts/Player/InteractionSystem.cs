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
    private PlayerMovement _playerMovement;
    private PlayerAnimator _playerAnimator;
    private InputAction _interactAction;

    public IInteractable CurrentTarget { get; private set; }

    private static readonly Collider2D[] _buffer = new Collider2D[8];

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerAnimator = GetComponentInChildren<PlayerAnimator>();
        SetupInput();
    }

    private void OnEnable()
    {
        _interactAction?.Enable();
        if (_interactAction != null)
            _interactAction.performed += OnInteractPerformed;

        // Ouve o fim da animação para liberar o movimento
        if (_playerAnimator != null)
            _playerAnimator.OnActionAnimationEnd += OnActionAnimationEnd;
    }

    private void OnDisable()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteractPerformed;
        _interactAction?.Disable();

        if (_playerAnimator != null)
            _playerAnimator.OnActionAnimationEnd -= OnActionAnimationEnd;

        // Garante que o movimento é liberado ao desabilitar
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
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            interactRadius,
            _buffer,
            interactableLayer
        );

        IInteractable best = null;
        float bestDist = float.MaxValue;

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

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        IInteractable target = FindBestInteractable();
        if (target == null) return;

        Vector3 targetPosition = ((MonoBehaviour)target).transform.position;
        bool actionExecuted = false;

        if (target.RequiredTool == ToolType.None)
        {
            actionExecuted = target.Interact(gameObject);
        }
        else if (_inventory.HasTool(target.RequiredTool))
        {
            actionExecuted = target.Interact(gameObject);
        }
        else
        {
            Debug.Log($"[Interaction] Precisa de: {target.RequiredTool}");
        }

        if (actionExecuted)
        {
            // Trava o movimento durante a animação
            if (target.RequiredTool != ToolType.None)
                _playerMovement?.LockMovement();
            _playerAnimator?.PlayActionAnimation(target.RequiredTool, targetPosition);
        }
    }

    private void OnActionAnimationEnd()
    {
        // Libera o movimento ao terminar a animação
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
