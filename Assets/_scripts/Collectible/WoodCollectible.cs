using UnityEngine;

public class WoodCollectible : Collectible
{
    protected override void OnCollect(GameObject collector)
    {
        ResourceManager.Add(ResourceType.Wood, itemData.value);
    }
}
