using System;
using UnityEngine;

/// <summary>
/// Companheira do jogador. Substitui o sistema de HP —
/// quando o player sofre dano, perde a Estrela.
/// Um inimigo pode pegar a Estrela e carregá-la de volta ao seu SpawnPoint.
/// </summary>
public class Star : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static Star Instance { get; private set; }

    #endregion

    // ─────────────────────────────────────────
    #region Configuração

    [Header("Seguimento do Player")]
    [SerializeField] private float offsetDistance = 0.6f;
    [SerializeField] private float followSpeed    = 10f;
    [SerializeField] private float pickupRadius   = 1f;

    [Header("Drop")]
    [Tooltip("Tempo real (unscaled) antes do player poder recolher a Estrela após perder.")]
    [SerializeField] private float dropPickupDelay = 3f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    public enum StarState { FollowingPlayer, Dropped, CarriedByEnemy }

    private StarState _state = StarState.FollowingPlayer;
    private Transform _player;
    private Transform _carrier;
    private Vector2   _lastPlayerDir   = Vector2.left;
    private Vector3   _previousPlayerPos;
    private bool      _isLaunching;
    private float     _noPickupTimer;

    public StarState State     => _state;
    public bool      IsDropped => _state == StarState.Dropped;
    public Vector2   Position  => transform.position;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    /// <summary>Disparado quando a Estrela cai no chão.</summary>
    public static event Action OnDropped;

    /// <summary>Disparado quando um inimigo entrega a Estrela ao SpawnPoint.</summary>
    public static event Action OnLost;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        switch (_state)
        {
            case StarState.FollowingPlayer:
                if (_player != null)
                {
                    // Detecta direção de movimento do player para posicionar atrás
                    Vector3 delta = _player.position - _previousPlayerPos;
                    if (delta.sqrMagnitude > 0.0001f)
                        _lastPlayerDir = delta.normalized;
                    _previousPlayerPos = _player.position;

                    Vector3 target = _player.position - (Vector3)(_lastPlayerDir * offsetDistance);
                    transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);
                }
                break;

            case StarState.Dropped:
                if (_noPickupTimer > 0f)
                {
                    _noPickupTimer -= Time.unscaledDeltaTime;
                    break;
                }
                if (_player != null &&
                    Vector2.Distance(transform.position, _player.position) <= pickupRadius)
                    ReturnToPlayer();
                break;

            case StarState.CarriedByEnemy:
                if (_carrier != null)
                    transform.position = _carrier.position + Vector3.up * 0.4f;
                break;
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Vincula a Estrela ao player. Chamado no início do jogo.</summary>
    public void SetPlayer(Transform player)
    {
        _player            = player;
        _previousPlayerPos = player != null ? player.position : Vector3.zero;
        _state             = StarState.Dropped;
        //_state             = StarState.FollowingPlayer;
    }

    private void ReturnToPlayer()
    {
        _state = StarState.FollowingPlayer;
    }

    /// <summary>
    /// Dropa a Estrela na posição indicada. Só funciona se estiver seguindo o player.
    /// Chamado por PlayerHealth quando o player sofre dano.
    /// </summary>
    public void Drop(Vector2 position)
    {
        if (_state != StarState.FollowingPlayer) return;
        _state         = StarState.Dropped;
        _noPickupTimer = dropPickupDelay;
        transform.position = position;
        OnDropped?.Invoke();
    }

    /// <summary>
    /// Força o drop da Estrela mesmo se carregada por um inimigo.
    /// Chamado quando o inimigo portador morre.
    /// </summary>
    public void ForceDropAt(Vector2 position)
    {
        if (_state != StarState.CarriedByEnemy) return;
        _carrier = null;
        _state   = StarState.Dropped;
        transform.position = position;
        OnDropped?.Invoke();
    }

    /// <summary>
    /// Tenta pegar a Estrela. Retorna true apenas se ainda estava no chão.
    /// Garante que apenas o primeiro inimigo a consegue.
    /// </summary>
    public bool TryPickUp(Transform carrier)
    {
        if (_state != StarState.Dropped) return false;
        _state   = StarState.CarriedByEnemy;
        _carrier = carrier;
        return true;
    }

    /// <summary>
    /// Chamado quando o inimigo entrega a Estrela ao SpawnPoint.
    /// </summary>
    public void Deliver()
    {
        _carrier = null;
        _state   = StarState.Dropped;
        OnLost?.Invoke();
    }

    /// <summary>
    /// Lança a Estrela em arco para cima e de volta — usada durante o slow motion do player.
    /// Usa tempo não escalado para funcionar corretamente com Time.timeScale alterado.
    /// </summary>
    public void LaunchArc(float height = 2.5f, float duration = 2.25f)
    {
        if (_isLaunching) return;
        StartCoroutine(LaunchArcRoutine(height, duration));
    }

    private System.Collections.IEnumerator LaunchArcRoutine(float height, float duration)
    {
        _isLaunching = true;
        Vector3 origin  = transform.position;
        float   elapsed = 0f;

        while (elapsed < duration && _state == StarState.Dropped)
        {
            elapsed += Time.unscaledDeltaTime;
            float t       = Mathf.Clamp01(elapsed / duration);
            float yOffset = Mathf.Sin(t * Mathf.PI) * height;
            transform.position = new Vector3(origin.x, origin.y + yOffset, origin.z);
            yield return null;
        }

        _isLaunching = false;

        // Se a estrela ainda está no chão, posiciona no ponto de queda e permanece Dropped
        if (_state == StarState.Dropped)
            transform.position = origin;
    }

    #endregion
}
