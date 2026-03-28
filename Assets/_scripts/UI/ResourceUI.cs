using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI woodText;
    [SerializeField] private TextMeshProUGUI oreText;
    [SerializeField] private TextMeshProUGUI foodText;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void OnEnable()
    {
        ResourceManager.OnResourceChanged += OnResourceChanged;
    }

    private void OnDisable()
    {
        ResourceManager.OnResourceChanged -= OnResourceChanged;
    }

    private void Start()
    {
        // Inicializa todos os textos
        UpdateText(ResourceType.Coin, ResourceManager.Get(ResourceType.Coin));
        UpdateText(ResourceType.Wood, ResourceManager.Get(ResourceType.Wood));
        UpdateText(ResourceType.Ore, ResourceManager.Get(ResourceType.Ore));
        UpdateText(ResourceType.Food, ResourceManager.Get(ResourceType.Food));
    }

    #endregion

    // ─────────────────────────────────────────
    #region Callbacks

    private void OnResourceChanged(ResourceType type, int current)
    {
        UpdateText(type, current);
    }

    private void UpdateText(ResourceType type, int value)
    {
        switch (type)
        {
            case ResourceType.Coin: if (coinText) coinText.text = $"{value}"; break;
            case ResourceType.Wood: if (woodText) woodText.text = $"{value}"; break;
            case ResourceType.Ore: if (oreText) oreText.text = $"{value}"; break;
            case ResourceType.Food: if (foodText) foodText.text = $"{value}"; break;
        }
    }

    #endregion
}
