using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controla a aparência visual do ciclo dia/noite via Global Light 2D.
/// Requer URP 2D com um componente Global Light 2D na cena.
/// </summary>
public class DayNightLighting : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Referência de Luz")]
    [Tooltip("Arraste aqui o GameObject com Global Light 2D. Se vazio, busca no próprio objeto.")]
    [SerializeField] private Light2D globalLight;

    [Header("Dia")]
    [SerializeField] private float dayIntensity = 1f;
    [SerializeField] private Color dayColor = new Color(1f, 0.95f, 0.82f, 1f);

    [Header("Noite")]
    [SerializeField] private float nightIntensity = 0.15f;
    [SerializeField] private Color nightColor = new Color(0.08f, 0.08f, 0.25f, 1f);

    [Header("Transição")]
    [Tooltip("Duração em segundos da transição suave entre dia e noite.")]
    [SerializeField] private float transitionDuration = 5f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Coroutine _transitionRoutine;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        if (globalLight == null)
            globalLight = GetComponent<Light2D>();
    }

    private void OnEnable()
    {
        DayNightCycle.OnDayStarted   += HandleDayStarted;
        DayNightCycle.OnNightStarted += HandleNightStarted;
    }

    private void OnDisable()
    {
        DayNightCycle.OnDayStarted   -= HandleDayStarted;
        DayNightCycle.OnNightStarted -= HandleNightStarted;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Handlers

    private void HandleDayStarted()   => StartLightTransition(dayIntensity, dayColor);
    private void HandleNightStarted() => StartLightTransition(nightIntensity, nightColor);

    #endregion

    // ─────────────────────────────────────────
    #region Transição

    private void StartLightTransition(float targetIntensity, Color targetColor)
    {
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(TransitionRoutine(targetIntensity, targetColor));
    }

    private IEnumerator TransitionRoutine(float targetIntensity, Color targetColor)
    {
        if (globalLight == null) yield break;

        float startIntensity = globalLight.intensity;
        Color startColor     = globalLight.color;
        float elapsed        = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            globalLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            globalLight.color     = Color.Lerp(startColor, targetColor, t);

            yield return null;
        }

        globalLight.intensity = targetIntensity;
        globalLight.color     = targetColor;
    }

    #endregion
}
