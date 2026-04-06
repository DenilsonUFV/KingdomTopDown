using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Referências

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Textos")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Barra de Progresso")]
    [SerializeField] private Slider slider;

    [Header("Cores da Barra")]
    [SerializeField] private Color colorStart = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color colorComplete = new Color(0.2f, 0.9f, 0.3f);

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private bool _isRunning = false;
    private float _duration = 0f;
    private float _elapsed = 0f;

    // Referência à imagem de fill do Slider para mudar a cor
    private Image _fillImage;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        // Configura o Slider
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;  // não é interagível pelo jogador

            // Pega a imagem de fill para controlar a cor
            _fillImage = slider.fillRect?.GetComponent<Image>();
        }

        SetSliderVisible(false);
    }

    private void Update()
    {
        if (!_isRunning) return;

        _elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(_elapsed / _duration);
        UpdateSlider(progress);

        if (progress >= 1f)
            CompleteBar();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Refresh — textos de custo

    public void Refresh(Building building)
    {
        if (building.State == BuildingState.UnderConstruction)
        {
            root?.SetActive(true);
            SetTextsVisible(false);
            return;
        }

        if (building.Data?.nextLevel == null)
        {
            root?.SetActive(false);
            SetSliderVisible(false);
            return;
        }

        root?.SetActive(true);
        SetTextsVisible(true);
        SetSliderVisible(false);

        int cost = building.Data.nextLevel.coinCost;
        int invested = building.CoinsInvested;
        int remaining = building.CoinsRemaining;

        if (invested <= 0)
        {
            if (costText) costText.text = $"{cost} moedas";
            if (progressText) progressText.text = "";
        }
        else
        {
            if (costText) costText.text = $"{remaining} restantes";
            if (progressText) progressText.text = $"{invested}/{cost}";
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Barra de Progresso

    public void StartProgress(float duration)
    {
        _duration = duration;
        _elapsed = 0f;
        _isRunning = true;

        SetTextsVisible(false);
        SetSliderVisible(true);
        UpdateSlider(0f);
    }

    public void StopProgress()
    {
        _isRunning = false;
        SetSliderVisible(false);
    }

    private void UpdateSlider(float progress)
    {
        if (slider == null) return;

        slider.value = progress;

        if (_fillImage != null)
            _fillImage.color = Color.Lerp(colorStart, colorComplete, progress);
    }

    private void CompleteBar()
    {
        _isRunning = false;
        UpdateSlider(1f);
        Invoke(nameof(HideBar), 0.5f);
    }

    private void HideBar()
    {
        SetSliderVisible(false);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private void SetSliderVisible(bool visible)
    {
        if (slider != null)
            slider.gameObject.SetActive(visible);
    }

    private void SetTextsVisible(bool visible)
    {
        if (costText) costText.gameObject.SetActive(visible);
        if (progressText) progressText.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Atualiza a barra manualmente (0 a 1) — usado pelo BuildRoutine com múltiplos BOTs.
    /// </summary>
    public void UpdateProgressManual(float progress)
    {
        if (slider == null) return;
        slider.value = Mathf.Clamp01(progress);
        if (_fillImage != null)
            _fillImage.color = Color.Lerp(colorStart, colorComplete, progress);
    }

    #endregion
}
