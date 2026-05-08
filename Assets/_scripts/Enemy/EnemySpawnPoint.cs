using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnReward
{
    public GameObject prefab;
    [Min(1)] public int amount;
}

/// <summary>
/// Ponto de spawn de inimigos no mapa.
/// Spawna inimigos ao anoitecer até esgotar as cargas totais (maxSpawns).
/// Quando a noite acaba, os inimigos retornam a este ponto e somem.
/// Ao esgotar todas as cargas, no próximo amanhecer dropa recompensas e se destrói.
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Prefab")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Cargas")]
    [Tooltip("Total de inimigos que este ponto pode gerar durante o jogo inteiro.")]
    [SerializeField] private int maxSpawns = 10;

    [Tooltip("Quantos inimigos spawna por noite (limitado pelas cargas restantes).")]
    [SerializeField] private int spawnsPerNight = 3;

    [Header("Temporização")]
    [Tooltip("Intervalo entre cada spawn na mesma noite (segundos).")]
    [SerializeField] private float spawnInterval = 2f;

    [Tooltip("Dispersão aleatória ao redor do ponto de spawn.")]
    [SerializeField] private float spawnRadius = 1f;

    [Header("Recompensa Final")]
    [Tooltip("Itens dropados ao amanhecer quando todas as cargas se esgotam.")]
    [SerializeField] private SpawnReward[] rewards;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private int _spawnsRemaining;
    private readonly List<EnemyBrain> _spawnedEnemies = new();
    private bool _isSpawning;

    public bool IsExhausted    => _spawnsRemaining <= 0;
    public int  SpawnsRemaining => _spawnsRemaining;
    public Vector3 SpawnPosition => transform.position;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _spawnsRemaining = maxSpawns;
    }

    private void OnEnable()
    {
        DayNightCycle.OnNightStarted += HandleNightStarted;
        DayNightCycle.OnDayStarted   += HandleDayStarted;
    }

    private void OnDisable()
    {
        DayNightCycle.OnNightStarted -= HandleNightStarted;
        DayNightCycle.OnDayStarted   -= HandleDayStarted;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Spawn

    private void HandleNightStarted()
    {
        if (IsExhausted || _isSpawning || enemyPrefab == null) return;
        StartCoroutine(SpawnRoutine());
    }

    private void HandleDayStarted()
    {
        if (!IsExhausted) return;
        DropRewards();
        Destroy(gameObject);
    }

    private void DropRewards()
    {
        if (rewards == null) return;
        foreach (SpawnReward reward in rewards)
        {
            if (reward.prefab == null) continue;
            for (int i = 0; i < reward.amount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                Instantiate(reward.prefab, (Vector2)transform.position + offset, Quaternion.identity);
            }
        }
    }

    private IEnumerator SpawnRoutine()
    {
        _isSpawning = true;
        int toSpawn = Mathf.Min(spawnsPerNight, _spawnsRemaining);

        for (int i = 0; i < toSpawn; i++)
        {
            SpawnOneEnemy();
            if (i < toSpawn - 1)
                yield return new WaitForSeconds(spawnInterval);
        }

        _isSpawning = false;
    }

    private void SpawnOneEnemy()
    {
        if (IsExhausted) return;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos    = transform.position + (Vector3)offset;

        GameObject obj = Instantiate(enemyPrefab, pos, Quaternion.identity);

        EnemyBrain brain = obj.GetComponent<EnemyBrain>();
        if (brain != null)
        {
            brain.SetSpawnPoint(this);
            _spawnedEnemies.Add(brain);
        }

        _spawnsRemaining--;
        Debug.Log($"[EnemySpawnPoint] Inimigo spawnado. Cargas restantes: {_spawnsRemaining}/{maxSpawns}");
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Chamado pelo EnemyBrain ao ser destruído.</summary>
    public void NotifyEnemyRemoved(EnemyBrain enemy)
    {
        _spawnedEnemies.Remove(enemy);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Exibe cargas restantes como label aproximado via cor
        Gizmos.color = IsExhausted ? Color.gray : Color.red;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }

    #endregion
}
