using UnityEngine;

public class CoinSpawnHelper : MonoBehaviour
{
    public static CoinSpawnHelper Instance { get; private set; }

    [SerializeField] private GameObject coinPrefab;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnCoin(Vector3 position)
    {
        if (coinPrefab == null) return;

        Vector2 offset = Random.insideUnitCircle * 0.5f;
        Instantiate(coinPrefab, position + (Vector3)offset, Quaternion.identity);
    }
}
