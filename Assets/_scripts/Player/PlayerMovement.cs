using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;

    [Header("Identificação")]
    [SerializeField] private int playerIndex = 0;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private Vector2 _moveInput;

    // Bloqueio externo — qualquer sistema pode travar o movimento
    private int _movementLockCount = 0;
    public bool IsLocked => _movementLockCount > 0;

    public Vector2 MoveInput => IsLocked ? Vector2.zero : _moveInput;

    // ─────────────────────────────────────────
    #region Lock API

    /// <summary>
    /// Trava o movimento. Cada chamada deve ter um Unlock correspondente.
    /// </summary>
    public void LockMovement()
    {
        _movementLockCount++;

        // Para imediatamente ao travar
        _rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Libera o movimento. Só move novamente quando todos os locks forem removidos.
    /// </summary>
    public void UnlockMovement()
    {
        _movementLockCount = Mathf.Max(0, _movementLockCount - 1);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SetupInput();
    }

    private void OnEnable() => _moveAction?.Enable();
    private void OnDisable() => _moveAction?.Disable();

    private void Update()
    {
        if (_moveAction == null) return;
        _moveInput = _moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // MoveInput já retorna zero se travado
        _rb.linearVelocity = MoveInput.normalized * moveSpeed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Setup

    private void SetupInput()
    {
        if (inputActionAsset == null) return;

        string mapName = playerIndex == 0 ? "Player1" : "Player2";
        InputActionMap map = inputActionAsset.FindActionMap(mapName, throwIfNotFound: false);
        if (map == null) return;

        _moveAction = map.FindAction("Move", throwIfNotFound: false);
    }

    #endregion
}
