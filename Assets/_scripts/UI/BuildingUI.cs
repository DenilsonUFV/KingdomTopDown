using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Root Canvas (Slider)")]
    [SerializeField] private GameObject root;

    [Header("Barra de Progresso")]
    [SerializeField] private Slider slider;

    [Header("Cores da Barra")]
    [SerializeField] private Color colorStart    = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color colorComplete = new Color(0.2f, 0.9f, 0.3f);

    [Header("Ícones de Custo (world-space)")]
    [Tooltip("Transform pai dos ícones — deve ser um filho direto da construção, fora do Canvas. Se nulo, é criado automaticamente.")]
    [SerializeField] private Transform iconRoot;
    [SerializeField] private Vector3   iconOffset       = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float     iconSpacing      = 0.35f;
    [SerializeField] private float     iconScale        = 0.3f;
    [SerializeField] private string    iconSortingLayer = "Dynamic";
    [SerializeField] private int       iconSortingOrder = 8;
    [SerializeField] private Color     emptyColor       = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color     filledColor      = Color.white;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private bool  _isRunning;
    private float _duration;
    private float _elapsed;
    private Image _fillImage;

    private SpriteRenderer[] _slots;
    private Sprite           _builtIcon;
    private int              _builtCount;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        if (iconRoot == null)
        {
            Building building = GetComponentInParent<Building>();
            Transform parent  = building != null ? building.transform : transform.parent ?? transform;
            var go = new GameObject("IconRoot");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one;
            iconRoot = go.transform;
        }

        if (slider != null)
        {
            slider.minValue    = 0f;
            slider.maxValue    = 1f;
            slider.value       = 0f;
            slider.interactable = false;
            _fillImage = slider.fillRect?.GetComponent<Image>();
        }

        ShowRoot(false);
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
    #region Refresh

    public void Refresh(Building building)
    {
        switch (building.State)
        {
            case BuildingState.UnderConstruction:
                ShowRoot(true);
                ClearIconSlots();
                return;

            case BuildingState.WaitingBuilder:
                ShowRoot(false);
                ClearIconSlots();
                return;

            case BuildingState.Destroyed:
            case BuildingState.WaitingFunds:
                ShowRoot(false);
                BuildIconSlots(building.Data?.coinIcon, building.TargetCost);
                SetFilled(building.CoinsInvested);
                return;

            case BuildingState.Built:
            {
                bool isDamaged  = !building.IsAtFullHealth;
                bool canUpgrade = building.HasNextLevel && building.IsAtFullHealth;

                if (isDamaged)
                {
                    ShowRoot(false);
                    BuildIconSlots(building.Data?.coinIcon, building.Data.RepairCost);
                    SetFilled(building.CoinsInvested);
                    return;
                }
                if (canUpgrade)
                {
                    ShowRoot(false);
                    BuildIconSlots(building.Data?.coinIcon, building.Data.nextLevel.coinCost);
                    SetFilled(building.CoinsInvested);
                    return;
                }
                ShowRoot(false);
                ClearIconSlots();
                return;
            }

            default:
                ShowRoot(false);
                ClearIconSlots();
                return;
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region Barra de Progresso

    public void StartProgress(float duration)
    {
        _duration  = duration;
        _elapsed   = 0f;
        _isRunning = true;

        ClearIconSlots();
        ShowRoot(true);
        SetSliderVisible(true);
        UpdateSlider(0f);
    }

    public void StopProgress()
    {
        _isRunning = false;
        SetSliderVisible(false);
    }

    public void UpdateProgressManual(float progress)
    {
        if (slider == null) return;
        slider.value = Mathf.Clamp01(progress);
        if (_fillImage != null)
            _fillImage.color = Color.Lerp(colorStart, colorComplete, progress);
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
    #region Ícones de Custo

    private void BuildIconSlots(Sprite icon, int count)
    {
        if (icon == _builtIcon && count == _builtCount && _slots != null) return;

        _builtIcon  = icon;
        _builtCount = count;
        ClearIconSlots();

        if (icon == null || count <= 0 || iconRoot == null) return;

        _slots = new SpriteRenderer[count];
        float   totalWidth = (count - 1) * iconSpacing;
        Vector3 startLocal = iconOffset + Vector3.left * (totalWidth * 0.5f);

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Slot_" + i);
            go.transform.SetParent(iconRoot);
            go.transform.localPosition = startLocal + Vector3.right * i * iconSpacing;
            go.transform.localScale    = Vector3.one * iconScale;

            SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
            sr.sprite           = icon;
            sr.color            = emptyColor;
            sr.sortingLayerName = iconSortingLayer;
            sr.sortingOrder     = iconSortingOrder;

            _slots[i] = sr;
        }
    }

    public void SetFilled(int count)
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;
            _slots[i].color = i < count ? filledColor : emptyColor;
        }
    }

    private void ClearIconSlots()
    {
        if (iconRoot != null)
        {
            foreach (Transform child in iconRoot)
                Destroy(child.gameObject);
        }
        _slots      = null;
        _builtIcon  = null;
        _builtCount = 0;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private void ShowRoot(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    private void SetSliderVisible(bool visible)
    {
        if (slider != null)
            slider.gameObject.SetActive(visible);
    }

    #endregion
}
