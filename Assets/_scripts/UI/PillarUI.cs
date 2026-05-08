using UnityEngine;

/// <summary>
/// UI em espaço de mundo do Pilar de Cristal.
/// Cria N ícones do recurso exigido acima do pilar.
/// Ícones vazios (semi-transparentes) vão sendo preenchidos (cor cheia) conforme
/// o jogador deposita recursos — estilo Kingdom Two Crowns.
/// </summary>
public class PillarUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Layout")]
    [Tooltip("Deslocamento local acima do pilar.")]
    [SerializeField] private Vector3 offset   = new Vector3(0f, 1f, 0f);
    [Tooltip("Espaçamento horizontal entre os ícones.")]
    [SerializeField] private float   spacing  = 0.35f;
    [Tooltip("Escala de cada ícone.")]
    [SerializeField] private float   iconScale = 0.3f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayer = "Dynamic";
    [SerializeField] private int    sortingOrder = 8;

    [Header("Cores")]
    [SerializeField] private Color emptyColor  = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color filledColor = Color.white;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private SpriteRenderer[] _slots;

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Constrói os N ícones do recurso. Chame uma vez ao inicializar o pilar.</summary>
    public void Build(Sprite icon, int count)
    {
        Clear();

        if (icon == null || count <= 0) return;

        _slots = new SpriteRenderer[count];

        float totalWidth = (count - 1) * spacing;
        Vector3 startLocal = offset + Vector3.left * (totalWidth * 0.5f);

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Slot_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = startLocal + Vector3.right * i * spacing;
            go.transform.localScale    = Vector3.one * iconScale;

            SpriteRenderer sr      = go.AddComponent<SpriteRenderer>();
            sr.sprite              = icon;
            sr.color               = emptyColor;
            sr.sortingLayerName    = sortingLayer;
            sr.sortingOrder        = sortingOrder;

            _slots[i] = sr;
        }
    }

    /// <summary>Define quantos ícones aparecem como preenchidos (0 = todos vazios).</summary>
    public void SetFilled(int count)
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null) continue;
            _slots[i].color = i < count ? filledColor : emptyColor;
        }
    }

    /// <summary>Mostra ou oculta todos os ícones (ex.: durante cooldown).</summary>
    public void SetVisible(bool visible)
    {
        if (_slots == null) return;
        foreach (SpriteRenderer sr in _slots)
            if (sr != null) sr.gameObject.SetActive(visible);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private void Clear()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        _slots = null;
    }

    #endregion
}
