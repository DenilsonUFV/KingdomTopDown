using UnityEngine;

public class CoinCollectible : Collectible
{
    [Header("Moeda")]
    [SerializeField] private float valueMultiplier = 1f;
    [SerializeField] private ParticleSystem collectParticle;

    protected override void OnCollect(GameObject collector)
    {
        // Recurso global — não importa qual jogador coletou
        int finalValue = Mathf.RoundToInt(itemData.value * valueMultiplier);
        ResourceManager.Add(ResourceType.Coin, finalValue);
        SpawnParticle();
    }

    private void SpawnParticle()
    {
        if (collectParticle == null) return;
        ParticleSystem effect = Instantiate(collectParticle, transform.position, Quaternion.identity);
        Destroy(effect.gameObject, effect.main.duration);
    }
}
