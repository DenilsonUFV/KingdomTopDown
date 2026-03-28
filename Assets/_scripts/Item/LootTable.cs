using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable_New", menuName = "Collectibles/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("Entradas")]
    public List<LootEntry> entries = new();

    [Header("Quantidade Global")]
    [Tooltip("Limite máximo de itens spawnados independente das entradas.")]
    public int maxTotalItems = 10;

    // ─────────────────────────────────────────
    /// <summary>
    /// Rola a loot table e retorna os itens que devem ser spawnados.
    /// </summary>
    public List<LootEntry> Roll()
    {
        List<LootEntry> result = new();
        int total = 0;

        foreach (LootEntry entry in entries)
        {
            if (entry.itemData == null || entry.collectiblePrefab == null) continue;
            if (total >= maxTotalItems) break;

            // Rola a chance
            if (Random.value > entry.dropChance) continue;

            // Clona a entrada com quantidade rolada
            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            amount = Mathf.Min(amount, maxTotalItems - total);

            LootEntry rolledEntry = new LootEntry
            {
                itemData = entry.itemData,
                collectiblePrefab = entry.collectiblePrefab,
                dropChance = entry.dropChance,
                minAmount = amount,
                maxAmount = amount
            };

            result.Add(rolledEntry);
            total += amount;
        }

        return result;
    }
}
