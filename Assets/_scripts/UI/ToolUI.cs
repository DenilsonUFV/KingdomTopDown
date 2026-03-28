using UnityEngine;
using UnityEngine.UI;

public class ToolUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Identificação")]
    [SerializeField] private int playerIndex = 0;

    [Header("Ícones das Ferramentas")]
    [SerializeField] private Image axeIcon;
    [SerializeField] private Image pickaxeIcon;
    [SerializeField] private Image fishingRodIcon;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private PlayerInventory _inventory;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void OnEnable()
    {
        PlayerManager.OnPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        PlayerManager.OnPlayerJoined -= OnPlayerJoined;
    }

    private void Start()
    {
        // Tenta vincular ao jogador já existente
        if (PlayerManager.Players.Count > playerIndex)
            BindInventory(PlayerManager.Players[playerIndex]);

        // Começa tudo invisível
        SetAllIconsVisible(false);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Callbacks

    private void OnPlayerJoined(PlayerController player)
    {
        if (player.playerIndex != playerIndex) return;
        BindInventory(player);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Bind

    private void BindInventory(PlayerController player)
    {
        _inventory = player.Inventory;

        // Inscreve no evento de item adicionado
        _inventory.OnItemAdded += RefreshIcons;
        RefreshIcons();
    }

    private void RefreshIcons()
    {
        if (_inventory == null) return;

        SetIconVisible(axeIcon, _inventory.HasTool(ToolType.Axe));
        SetIconVisible(pickaxeIcon, _inventory.HasTool(ToolType.Pickaxe));
        SetIconVisible(fishingRodIcon, _inventory.HasTool(ToolType.FishingRod));
    }

    private void SetIconVisible(Image icon, bool visible)
    {
        if (icon != null) icon.gameObject.SetActive(visible);
    }

    private void SetAllIconsVisible(bool visible)
    {
        SetIconVisible(axeIcon, visible);
        SetIconVisible(pickaxeIcon, visible);
        SetIconVisible(fishingRodIcon, visible);
    }

    #endregion
}
