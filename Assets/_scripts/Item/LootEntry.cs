using System;
using UnityEngine;

[Serializable]
public class LootEntry
{
    [Tooltip("Item a ser spawnado.")]
    public ItemData itemData;

    [Tooltip("Prefab do coletável que representa este item.")]
    public GameObject collectiblePrefab;

    [Range(0f, 1f)]
    [Tooltip("Chance de 0 a 1. Ex: 0.8 = 80%.")]
    public float dropChance = 1f;

    [Tooltip("Quantidade mínima spawnada.")]
    public int minAmount = 1;

    [Tooltip("Quantidade máxima spawnada.")]
    public int maxAmount = 3;
}