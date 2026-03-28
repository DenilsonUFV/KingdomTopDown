using UnityEngine;

public class BotSpawner : MonoBehaviour
{
    public static BotSpawner Instance { get; private set; }

    [SerializeField] private GameObject builderBotPrefab;
    [SerializeField] private GameObject defenderBotPrefab;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Spawna um BOT construtor. Chamado automaticamente ao evoluir para Tenda.
    /// </summary>
    public void SpawnBuilderBot(Vector3 position)
    {
        if (builderBotPrefab == null) return;
        Instantiate(builderBotPrefab, position, Quaternion.identity);
        Debug.Log("[BotSpawner] BOT Construtor spawnado!");
    }

    /// <summary>
    /// Spawna um BOT defensor. Para uso futuro.
    /// </summary>
    public void SpawnDefenderBot(Vector3 position)
    {
        if (defenderBotPrefab == null) return;
        Instantiate(defenderBotPrefab, position, Quaternion.identity);
        Debug.Log("[BotSpawner] BOT Defensor spawnado!");
    }
}
