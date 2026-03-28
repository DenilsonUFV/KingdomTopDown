using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Loot")]
    [SerializeField] private LootTable lootTable;

    [Header("Spawn")]
    [Tooltip("Raio em que os itens são espalhados ao spawnar.")]
    [SerializeField] private float spawnRadius = 0.5f;

    [Tooltip("Intervalo entre cada item spawnado (efeito cascata).")]
    [SerializeField] private float spawnInterval = 0.1f;

    [Tooltip("Spawna apenas uma vez. Impede reativação.")]
    [SerializeField] private bool spawnOnce = true;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private bool _hasSpawned = false;

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>
    /// Chamado por qualquer interactable (baú, árvore, inimigo, etc).
    /// </summary>
    public void Spawn()
    {
        if (spawnOnce && _hasSpawned) return;
        if (lootTable == null)
        {
            Debug.LogWarning($"[ItemSpawner] LootTable não atribuída em {gameObject.name}");
            return;
        }

        _hasSpawned = true;

        List<LootEntry> rolledItems = lootTable.Roll();

        if (rolledItems.Count == 0) return;

        StartCoroutine(SpawnRoutine(rolledItems));
    }

    #endregion

    // ─────────────────────────────────────────
    #region Spawn Routine

    private IEnumerator SpawnRoutine(List<LootEntry> entries)
    {
        foreach (LootEntry entry in entries)
        {
            for (int i = 0; i < entry.minAmount; i++)
            {
                SpawnItem(entry);

                // Intervalo entre itens — cria efeito cascata visual
                if (spawnInterval > 0f)
                    yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private void SpawnItem(LootEntry entry)
    {
        // Posição aleatória dentro do raio
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

        // Instancia o prefab
        GameObject obj = Instantiate(entry.collectiblePrefab, spawnPos, Quaternion.identity);

        // Injeta o ItemData no coletável
        Collectible collectible = obj.GetComponent<Collectible>();
        if (collectible != null)
            collectible.SetItemData(entry.itemData);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }

    #endregion
}
