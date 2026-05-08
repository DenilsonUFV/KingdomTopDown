using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de vida dos BOTs defensores. Implementa IDamageable
/// para que inimigos possam atacá-los via EnemyTargetScanner.
/// </summary>
public class DefenderBotHealth : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Vida")]
    [SerializeField] private int maxHealth = 8;

    [Header("Drop ao Morrer")]
    [SerializeField] private List<LootEntry> deathLoot = new();

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    public int  _currentHealth;
    private bool _isDead;

    public int  CurrentHealth => _currentHealth;
    public bool IsDead        => _isDead;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    public event Action OnDeath;

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

        DropLoot();
        OnDeath?.Invoke();
    }

    private void DropLoot()
    {
        foreach (LootEntry entry in deathLoot)
        {
            if (entry?.collectiblePrefab == null) continue;
            if (UnityEngine.Random.value > entry.dropChance) continue;

            int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);
            for (int i = 0; i < amount; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
                GameObject obj = Instantiate(entry.collectiblePrefab,
                    (Vector2)transform.position + offset, Quaternion.identity);

                if (entry.itemData != null)
                    obj.GetComponent<Collectible>()?.SetItemData(entry.itemData);
            }
        }
    }

    #endregion
}
