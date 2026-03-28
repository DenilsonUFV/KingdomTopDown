using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJoinManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Jogador 2")]
    [SerializeField] private GameObject p2GameObject;
    [SerializeField] private Transform p2SpawnPoint;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private bool _p2Joined = false;
    private InputAction _joinAction;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        SetupJoinInput();

        if (p2GameObject != null)
            p2GameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _joinAction?.Enable();
        if (_joinAction != null)
            _joinAction.performed += OnJoinPerformed;
    }

    private void OnDisable()
    {
        if (_joinAction != null)
            _joinAction.performed -= OnJoinPerformed;
        _joinAction?.Disable();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Setup

    private void SetupJoinInput()
    {
        if (inputActionAsset == null) return;

        InputActionMap map = inputActionAsset.FindActionMap("Player2", throwIfNotFound: false);
        if (map == null) return;

        _joinAction = map.FindAction("Join", throwIfNotFound: false);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Join

    private void OnJoinPerformed(InputAction.CallbackContext ctx)
    {
        if (_p2Joined) return;
        JoinP2();
    }

    private void JoinP2()
    {
        if (p2GameObject == null) return;

        _p2Joined = true;

        if (p2SpawnPoint != null)
            p2GameObject.transform.position = p2SpawnPoint.position;

        // Ativa P2 — PlayerController.OnEnable registra no PlayerManager
        // Recursos já são globais — nenhuma wallet para inicializar
        p2GameObject.SetActive(true);

        Debug.Log("[PlayerJoinManager] P2 entrou! Recursos compartilhados com P1.");
    }

    #endregion
}
