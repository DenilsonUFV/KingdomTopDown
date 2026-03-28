using UnityEngine;

public class OreCollectible : Collectible
{
    protected override void OnCollect(GameObject collector)
    {
        ResourceManager.Add(ResourceType.Ore, itemData.value);
    }
}
