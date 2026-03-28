using UnityEngine;

public class PickCollectible : Collectible
{
    protected override void OnCollect(GameObject collector)
    {
        PlayerInventory inventory = collector.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning($"[PickCollectible] {collector.name} não possui PlayerInventory.");
            return;
        }

        // Verifica apenas o inventário DESTE jogador — não bloqueia o P2
        if (inventory.HasTool(ToolType.Pickaxe))
        {
            Debug.Log($"[PickCollectible] {collector.name} já possui a Picareta.");

            // Não coleta — item permanece no chão para o outro jogador
            return;
        }

        inventory.AddItem(itemData);
        Debug.Log($"[PickCollectible] {collector.name} adquiriu a Picareta!");
    }

    public override bool CanPlayerCollect(GameObject collector)
    {
        PlayerInventory inventory = collector.GetComponent<PlayerInventory>();
        if (inventory == null) return false;

        // Só pode coletar se ainda não tiver o machado
        return !inventory.HasTool(ToolType.Pickaxe);
    }

}
