using System.Collections;
using UnityEngine;

/// <summary>
/// Feedback visual e físico ao receber dano.
/// Adicione ao prefab de inimigos e BOTs.
///
/// ApplyHit(origin) → empurra o alvo na direção oposta ao ponto de ataque
///                   → pisca o sprite por um curto período.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Knockback")]
    [SerializeField] private float knockbackForce    = 5f;
    [SerializeField] private float knockbackDuration = 0.18f;

    [Header("Flash")]
    [SerializeField] private Color flashColor    = Color.white;
    [SerializeField] private float flashInterval = 0.07f;
    [SerializeField] private int   flashCount    = 3;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private EnemyMovement    _enemyMov;
    private BotMovement      _botMov;
    private SpriteRenderer[] _renderers;
    private Coroutine        _flashRoutine;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _enemyMov  = GetComponent<EnemyMovement>();
        _botMov    = GetComponent<BotMovement>();
        _renderers = GetComponents<SpriteRenderer>();
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Aplica knockback e flash. attackOrigin é a posição de onde veio o golpe.
    /// </summary>
    public void ApplyHit(Vector2 attackOrigin)
    {
        Vector2 dir = ((Vector2)transform.position - attackOrigin).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;

        _enemyMov?.Knockback(dir * knockbackForce, knockbackDuration);
        _botMov?.Knockback(dir * knockbackForce, knockbackDuration);

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    #endregion

    // ─────────────────────────────────────────
    #region Flash

    private IEnumerator FlashRoutine()
    {
        // Guarda as cores atuais no início do flash
        Color[] saved = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            saved[i] = _renderers[i] != null ? _renderers[i].color : Color.white;

        for (int i = 0; i < flashCount; i++)
        {
            SetColor(flashColor);
            yield return new WaitForSeconds(flashInterval);
            RestoreColors(saved);
            if (i < flashCount - 1)
                yield return new WaitForSeconds(flashInterval);
        }

        _flashRoutine = null;
    }

    private void SetColor(Color c)
    {
        foreach (SpriteRenderer sr in _renderers)
            if (sr != null) sr.color = c;
    }

    private void RestoreColors(Color[] saved)
    {
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = saved[i];
    }

    #endregion
}
