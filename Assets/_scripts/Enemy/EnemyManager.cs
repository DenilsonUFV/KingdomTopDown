using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton que rastreia todos os inimigos ativos na cena.
/// Ao amanhecer, ordena o recuo de todos os inimigos sobreviventes.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static EnemyManager Instance { get; private set; }

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private readonly List<EnemyBrain> _activeEnemies = new();

    public int ActiveCount => _activeEnemies.Count;
    public IReadOnlyList<EnemyBrain> ActiveEnemies => _activeEnemies;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        DayNightCycle.OnDayStarted += HandleDayStarted;
    }

    private void OnDisable()
    {
        DayNightCycle.OnDayStarted -= HandleDayStarted;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Registro

    public void Register(EnemyBrain enemy)
    {
        if (!_activeEnemies.Contains(enemy))
            _activeEnemies.Add(enemy);
    }

    public void Unregister(EnemyBrain enemy)
    {
        _activeEnemies.Remove(enemy);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Handlers

    private void HandleDayStarted()
    {
        // Itera em cópia para evitar problemas caso algum inimigo se destrua durante o loop
        EnemyBrain[] snapshot = _activeEnemies.ToArray();
        foreach (EnemyBrain enemy in snapshot)
        {
            if (enemy != null && !enemy.IsDead)
                enemy.OrderRetreat();
        }

        Debug.Log($"[EnemyManager] Amanheceu — {snapshot.Length} inimigo(s) ordenados a recuar.");
    }

    #endregion
}
