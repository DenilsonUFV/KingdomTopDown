using System;
using UnityEngine;

/// <summary>
/// Sistema de vida dos inimigos. Implementa IDamageable para ser
/// detectado pelo EnemyTargetScanner dos defensores e pelo scanner dos inimigos.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────
    #region Estado

    private int _maxHealth;
    private int _currentHealth;
    private bool _isDead;

    public int  CurrentHealth => _currentHealth;
    public bool IsDead        => _isDead;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    /// <summary>Disparado quando o inimigo morre.</summary>
    public event Action OnDeath;

    #endregion

    // ─────────────────────────────────────────
    #region Inicialização

    /// <summary>Chamado pelo EnemyBrain no Start.</summary>
    public void Init(int maxHealth)
    {
        _maxHealth     = maxHealth;
        _currentHealth = maxHealth;
        _isDead        = false;
    }

    #endregion

    // ─────────────────────────────────────────
    #region IDamageable

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;
        Debug.Log($"[EnemyHealth] {gameObject.name} recebeu {damage} de dano. HP: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0)
            Die();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Morte

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDeath?.Invoke();
    }

    #endregion
}
