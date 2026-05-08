using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(InteractionSystem))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Identificação")]
    public int playerIndex = 0;
    public string playerName = "P1";
    public Color playerColor = Color.white;

    #endregion

    // ─────────────────────────────────────────
    #region Referências

    // Wallet removida — recursos são globais via ResourceManager
    public PlayerMovement Movement { get; private set; }
    public PlayerInventory Inventory { get; private set; }

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        Movement = GetComponent<PlayerMovement>();
        Inventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        Star.Instance.SetPlayer(transform);
    }

    private void OnEnable()
    {
        PlayerManager.Register(this);
    }

    private void OnDisable()
    {
        PlayerManager.Unregister(this);
    }

    #endregion
}
