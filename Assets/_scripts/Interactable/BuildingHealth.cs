using System;
using UnityEngine;

/// <summary>
/// Sistema de vida de construções. Implementa IDamageable.
/// Ao chegar a zero, dispara OnDestroyed mas NÃO destrói o GameObject —
/// Building.cs cuida da transição visual e de estado.
/// </summary>
public class BuildingHealth : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Vida")]
    [SerializeField] private int maxHealth = 20;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private int  _currentHealth;
    private bool _isDead;

    public int  CurrentHealth => _currentHealth;
    public int  MaxHealth     => maxHealth;
    public bool IsDead        => _isDead;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    /// <summary>Disparado ao receber dano. Parâmetro: vida normalizada (0–1).</summary>
    public event Action<float> OnDamaged;

    /// <summary>Disparado quando a vida chega a zero. Não destrói o GameObject.</summary>
    public event Action OnDestroyed;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    #endregion

    // ─────────────────────────────────────────
    #region IDamageable

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnDamaged?.Invoke((float)_currentHealth / maxHealth);

        if (_currentHealth <= 0)
            Die();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Destruição / Reparo

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDestroyed?.Invoke();
    }

    /// <summary>Restaura a vida completamente após construção ou reparo.</summary>
    public void FullRepair()
    {
        _isDead        = false;
        _currentHealth = maxHealth;
    }

    /// <summary>Restaura parte da vida.</summary>
    public void Repair(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
    }

    #endregion
}
