using System;
using UnityEngine;

/// <summary>
/// Ponto de encaixe de BOT em uma construção.
///
/// Adicione este componente a qualquer prefab de construção que sirva como base para um BOT.
/// Crie um Transform filho na construção para definir a posição exata de encaixe e atribua
/// no campo Mount Transform. Se vazio, usa a posição da construção + 0.6 no eixo Y.
///
/// O BOT se movimenta autonomamente até cá quando o evento OnBecameAvailable dispara.
/// Quando a construção é destruída, o BOT é liberado via Dismount().
/// </summary>
[RequireComponent(typeof(Building))]
public class BotMountPoint : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Tooltip("Transform filho que define onde o BOT será posicionado. Deixe vazio para usar a posição da construção + offset.")]
    [SerializeField] private Transform mountTransform;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Building         _building;
    private BuildingHealth   _health;
    private DefenderBotBrain _occupant;

    public bool    IsOccupied         => _occupant != null;
    public bool    IsAvailable        => !IsOccupied && _building != null && _building.State == BuildingState.Built;
    public Vector3 MountWorldPosition => mountTransform != null
        ? mountTransform.position
        : transform.position + Vector3.up * 0.6f;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    /// <summary>Disparado quando este encaixe fica disponível (construção pronta e sem ocupante).</summary>
    public static event Action<BotMountPoint> OnBecameAvailable;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _building = GetComponent<Building>();
        _health   = GetComponent<BuildingHealth>();
    }

    private void Start()
    {
        // Construção já estava pronta ao entrar em cena (startBuilt = true ou spawnada pronta)
        if (_building != null && _building.State == BuildingState.Built)
            OnBecameAvailable?.Invoke(this);
    }

    private void OnEnable()
    {
        if (_building != null) _building.OnBuilt   += HandleBuilt;
        if (_health   != null) _health.OnDestroyed += HandleDestroyed;
    }

    private void OnDisable()
    {
        if (_building != null) _building.OnBuilt   -= HandleBuilt;
        if (_health   != null) _health.OnDestroyed -= HandleDestroyed;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Handlers

    private void HandleBuilt(Building _) => OnBecameAvailable?.Invoke(this);

    private void HandleDestroyed()
    {
        if (_occupant == null) return;
        _occupant.Dismount();
        _occupant = null;
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Tenta ocupar este encaixe. Retorna true apenas se estava disponível.
    /// Garante que apenas o primeiro BOT a chegar consiga montar.
    /// </summary>
    public bool TryMount(DefenderBotBrain bot)
    {
        if (!IsAvailable) return false;
        _occupant = bot;
        return true;
    }

    /// <summary>Libera o encaixe. Chamado pelo BOT ao desmontar voluntariamente ou ao morrer.</summary>
    public void Vacate(DefenderBotBrain bot)
    {
        if (_occupant == bot) _occupant = null;
    }

    #endregion
}
