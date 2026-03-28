using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSorting : MonoBehaviour
{
    [Header("Ajuste fino de offset")]
    [Tooltip("Desloca o ponto de referência vertical. Útil para sprites com pivô no centro.")]
    [SerializeField] private float yOffset = 0f;

    [Header("Multiplicador")]
    [Tooltip("Quanto maior, mais preciso o sorting. Padrão: 100.")]
    [SerializeField] private int sortingPrecision = 1000;

    // Propriedade lida pelo Manager
    public float SortingY => transform.position.y + yOffset;

    private SpriteRenderer _spriteRenderer;

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sortingLayerName = "Dynamic";

        // Registra no manager ao nascer
        YSortingManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        // Remove do manager ao morrer
        if (YSortingManager.Instance != null)
            YSortingManager.Instance.Unregister(this);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Sorting

    /// <summary>
    /// Chamado pelo Manager para aplicar o sorting order.
    /// A fórmula inverte o Y: menor Y (mais abaixo) = maior order (frente).
    /// </summary>
    public void ApplySorting()
    {
        _spriteRenderer.sortingOrder = Mathf.RoundToInt(-SortingY * sortingPrecision);
    }

    #endregion
}