using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animações de feedback visual da Estrela para eventos do jogo.
///
/// Adicione ao mesmo GameObject da Star.
/// Para disparar de outros sistemas: StarFeedback.Play(FeedbackType.X)
/// Para novos eventos: adicione o tipo no enum, configure no Inspector e
/// inscreva o handler em OnEnable/OnDisable.
/// </summary>
public class StarFeedback : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Tipos

    public enum FeedbackType
    {
        ResourcePickup,
        BuildingFunded,
        BuildingBuilt,
    }

    [Serializable]
    public class FeedbackConfig
    {
        [Tooltip("Escala máxima no pico da animação (1 = tamanho normal).")]
        public float scalePeak  = 1.4f;
        [Tooltip("Duração total da animação em segundos (tempo real).")]
        public float duration   = 0.35f;
        [Tooltip("Cor do flash no pico.")]
        public Color flashColor = Color.yellow;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Configuração

    [Header("Recurso Coletado")]
    [SerializeField] private FeedbackConfig _resourcePickup = new FeedbackConfig
        { scalePeak = 1.25f, duration = 0.22f, flashColor = new Color(1f, 0.9f, 0.2f) };

    [Header("Construção Financiada")]
    [SerializeField] private FeedbackConfig _buildingFunded = new FeedbackConfig
        { scalePeak = 1.40f, duration = 0.30f, flashColor = new Color(1f, 0.75f, 0f) };

    [Header("Construção Concluída")]
    [SerializeField] private FeedbackConfig _buildingBuilt = new FeedbackConfig
        { scalePeak = 1.65f, duration = 0.45f, flashColor = new Color(0.2f, 1f, 0.45f) };

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private static StarFeedback _instance;

    private Star           _star;
    private SpriteRenderer _sr;
    private Vector3        _baseScale;
    private Color          _baseColor;
    private Coroutine      _anim;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _instance  = this;
        _star      = GetComponent<Star>();
        _sr        = GetComponentInChildren<SpriteRenderer>();
        _baseScale = transform.localScale;
        _baseColor = _sr != null ? _sr.color : Color.white;
    }

    private void OnEnable()
    {
        ResourceManager.OnResourceAdded      += HandleResourceAdded;
        Building.OnAnyBuildingFullyFunded    += HandleBuildingFunded;
        Building.OnAnyBuilt                  += HandleBuildingBuilt;
    }

    private void OnDisable()
    {
        ResourceManager.OnResourceAdded      -= HandleResourceAdded;
        Building.OnAnyBuildingFullyFunded    -= HandleBuildingFunded;
        Building.OnAnyBuilt                  -= HandleBuildingBuilt;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Handlers de Eventos

    private void HandleResourceAdded(ResourceType _, int __) => Play(FeedbackType.ResourcePickup);
    private void HandleBuildingFunded(Building _)            => Play(FeedbackType.BuildingFunded);
    private void HandleBuildingBuilt(Building _)             => Play(FeedbackType.BuildingBuilt);

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Dispara uma animação de feedback. Pode ser chamado de qualquer sistema.
    /// Cancela e reinicia se já houver uma animação em andamento.
    /// </summary>
    public static void Play(FeedbackType type)
    {
        if (_instance == null) return;
        _instance.PlayInternal(type);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Animação

    private void PlayInternal(FeedbackType type)
    {
        // Só anima enquanto a Estrela está com o player
        if (_star != null && _star.State != Star.StarState.FollowingPlayer) return;

        FeedbackConfig cfg = type switch
        {
            FeedbackType.ResourcePickup => _resourcePickup,
            FeedbackType.BuildingFunded => _buildingFunded,
            FeedbackType.BuildingBuilt  => _buildingBuilt,
            _                           => _resourcePickup
        };

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimRoutine(cfg));
    }

    private IEnumerator AnimRoutine(FeedbackConfig cfg)
    {
        float elapsed = 0f;

        while (elapsed < cfg.duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t     = Mathf.Clamp01(elapsed / cfg.duration);
            float curve = Mathf.Sin(t * Mathf.PI);  // sobe suavemente e volta (0→1→0)

            transform.localScale = _baseScale * (1f + (cfg.scalePeak - 1f) * curve);

            if (_sr != null)
                _sr.color = Color.Lerp(_baseColor, cfg.flashColor, curve * 0.75f);

            yield return null;
        }

        transform.localScale = _baseScale;
        if (_sr != null) _sr.color = _baseColor;
        _anim = null;
    }

    #endregion
}
