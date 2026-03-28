using UnityEngine;

[RequireComponent(typeof(ItemSpawner))]
public class ChestInteractable : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Baú")]
    [SerializeField] private Sprite spriteOpen;
    [SerializeField] private Sprite spriteClosed;
    [SerializeField] private Animator chestAnimator;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private bool _isOpen = false;
    private ItemSpawner _spawner;
    private SpriteRenderer _sr;

    // IInteractable
    public bool CanInteract => !_isOpen;
    public ToolType RequiredTool => ToolType.None;   // baú não precisa de ferramenta
    public string InteractionHint => "Abrir Baú";

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _spawner = GetComponent<ItemSpawner>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (_sr != null && spriteClosed != null)
            _sr.sprite = spriteClosed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region IInteractable

    public bool Interact(GameObject interactor)
    {
        if (!CanInteract) return false;
        Open();
        return true;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Open

    private void Open()
    {
        _isOpen = true;

        if (_sr != null && spriteOpen != null)
            _sr.sprite = spriteOpen;

        if (chestAnimator != null)
            chestAnimator.SetTrigger("Open");

        _spawner.Spawn();
        enabled = false;

        Debug.Log($"[Chest] {gameObject.name} aberto!");
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isOpen ? Color.gray : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    #endregion
}
