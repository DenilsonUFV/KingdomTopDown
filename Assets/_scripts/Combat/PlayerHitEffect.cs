using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Efeito ao jogador perder a Estrela (receber dano).
/// Adicione ao prefab do PlayerController.
/// Requer um CinemachineImpulseSource neste GameObject
/// e um CinemachineImpulseListener na câmera virtual.
/// </summary>
[RequireComponent(typeof(CinemachineImpulseSource))]
public class PlayerHitEffect : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Camera Shake")]
    [SerializeField] private float shakeForce = 3f;

    [Header("Câmera Lenta")]
    [Tooltip("Escala de tempo durante o slow motion (0.1 = 10x mais lento).")]
    [SerializeField] private float slowMotionScale    = 0.15f;
    [Tooltip("Duração real (segundos) do slow motion.")]
    [SerializeField] private float slowMotionDuration = 3f;

    [Header("Lançamento da Estrela")]
    [Tooltip("Altura máxima do arco da Estrela durante o slow motion.")]
    [SerializeField] private float starLaunchHeight = 2.5f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private CinemachineImpulseSource _impulse;
    private Coroutine                _slowRoutine;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _impulse = GetComponent<CinemachineImpulseSource>();
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Dispara shake + slow motion + arco da Estrela.</summary>
    public void TriggerHit()
    {
        _impulse.GenerateImpulse(shakeForce);

        Star.Instance?.LaunchArc(starLaunchHeight, slowMotionDuration * 0.75f);

        if (_slowRoutine != null) StopCoroutine(_slowRoutine);
        _slowRoutine = StartCoroutine(SlowMotionRoutine());
    }

    #endregion

    // ─────────────────────────────────────────
    #region Slow Motion

    private IEnumerator SlowMotionRoutine()
    {
        Time.timeScale      = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * slowMotionScale;

        yield return new WaitForSecondsRealtime(slowMotionDuration);

        // Só restaura se o GameStateManager não assumiu o controle do timeScale
        // (Pausado e GameOver já definiram seus próprios valores)
        if (JogoAtivoNormal())
        {
            Time.timeScale      = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        _slowRoutine = null;
    }

    private void OnDestroy()
    {
        // Garante que o jogo não fica em slow motion se o player for destruído,
        // mas respeita o timeScale do GameStateManager caso já tenha assumido o controle
        if (JogoAtivoNormal())
        {
            Time.timeScale      = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    // Retorna true quando o jogo está rodando normalmente (Dia ou Noite).
    // Em Pausado e GameOver o GameStateManager já controla o timeScale.
    private static bool JogoAtivoNormal()
    {
        if (GameStateManager.Instance == null) return true;
        GameState s = GameStateManager.Instance.CurrentState;
        return s == GameState.Dia || s == GameState.Noite;
    }

    #endregion
}
