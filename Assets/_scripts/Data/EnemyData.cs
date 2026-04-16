using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que define todas as propriedades de um tipo de inimigo.
/// Crie via Assets → Create → KingdomTopDown → Enemy Data.
/// </summary>
[CreateAssetMenu(fileName = "EnemyData_New", menuName = "KingdomTopDown/Enemy Data")]
public class EnemyData : ScriptableObject
{
    // ─────────────────────────────────────────
    #region Identidade

    [Header("Identidade")]
    public string enemyName = "Inimigo";
    public Sprite sprite;

    #endregion

    // ─────────────────────────────────────────
    #region Estatísticas de Combate

    [Header("Vida")]
    public int maxHealth = 10;

    [Header("Movimento")]
    public float moveSpeed = 2f;

    [Header("Detecção")]
    [Tooltip("Raio em que o inimigo detecta alvos ao redor.")]
    public float detectionRadius = 8f;

    [Header("Ataque")]
    public float attackRange = 1.2f;
    public int attackDamage = 2;
    [Tooltip("Tempo entre ataques (segundos).")]
    public float attackCooldown = 1.5f;

    #endregion

    // ─────────────────────────────────────────
    #region Patrulha

    [Header("Patrulha")]
    [Tooltip("Raio de perambulação ao redor do ponto de spawn.")]
    public float patrolRadius = 5f;
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;

    #endregion

    // ─────────────────────────────────────────
    #region Loot

    [Header("Loot ao Morrer")]
    public LootTable lootTable;

    #endregion

    // ─────────────────────────────────────────
    #region Prioridade de Alvo

    [Header("Prioridade de Alvo")]
    [Tooltip(
        "Deixe VAZIO para atacar sempre o mais próximo.\n" +
        "Com itens: segue a ordem — dentro de cada tipo, ataca o mais próximo.\n" +
        "Exemplo: [Jogador, BotDefensor, BotConstrutor, Construcao]"
    )]
    public List<TargetType> targetPriority = new();

    #endregion
}
