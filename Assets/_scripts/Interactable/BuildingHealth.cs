using System;
using UnityEngine;

/// <summary>
/// Sistema de vida de construções. Implementa IDamageable para que
/// inimigos possam atacá-las via EnemyTargetScanner.
///
/// Adicione este componente ao prefab de construção ao lado do Building.cs.
/// Quando a vida chega a zero, a construção é destruída (ou regride conforme design).
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
    public bool IsDead        => _isDead;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    /// <summary>Disparado ao receber dano. Parâmetro: vida normalizada (0–1).</summary>
    public event Action<float> OnDamaged;

    /// <summary>Disparado quando a construção é destruída.</summary>
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

        _currentHealth -= damage;
        Debug.Log($"[BuildingHealth] {gameObject.name} recebeu {damage} de dano. HP: {_currentHealth}/{maxHealth}");

        OnDamaged?.Invoke((float)_currentHealth / maxHealth);

        if (_currentHealth <= 0)
            DestroyBuilding();
    }

    #endregion

    // ─────────────────────────────────────────
    #region Destruição

    private void DestroyBuilding()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[BuildingHealth] {gameObject.name} foi destruída!");
        OnDestroyed?.Invoke();

        // Notifica o Building para resetar ao estado Slot (se existir)
        Building building = GetComponent<Building>();
        if (building != null)
        {
            // Pode ser expandido: chamar building.Reset() ou simplesmente destruir
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Restaura a vida da construção (reparos).</summary>
    public void Repair(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
    }

    #endregion
}
