using System.Collections.Generic;
using UnityEngine;

public class BotHealth : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;

    [Header("Drop ao morrer")]
    [SerializeField] private List<LootEntry> deathLoot = new();

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private int _currentHealth;
    private bool _isDead = false;

    public bool IsDead => _isDead;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    #endregion

    // ─────────────────────────────────────────
    #region API

    /// <summary>
    /// Chamado por inimigos ao atacar. Preparado para o sistema de combate futuro.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        Debug.Log($"[BotHealth] {gameObject.name} recebeu {amount} de dano. HP: {_currentHealth}/{maxHealth}");

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

        // Notifica o BotBrain para lidar com o estado
        GetComponent<BotBrain>()?.OnDeath();

        // Dropa itens
        DropLoot();
    }

    private void DropLoot()
    {
        foreach (LootEntry entry in deathLoot)
        {
            if (entry == null || entry.collectiblePrefab == null) continue;
            if (Random.value > entry.dropChance) continue;

            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);

            for (int i = 0; i < amount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                Vector3 pos = transform.position + (Vector3)offset;

                GameObject obj = Instantiate(entry.collectiblePrefab, pos, Quaternion.identity);
                Collectible collectible = obj.GetComponent<Collectible>();
                if (collectible != null && entry.itemData != null)
                    collectible.SetItemData(entry.itemData);
            }
        }
    }

    #endregion
}
