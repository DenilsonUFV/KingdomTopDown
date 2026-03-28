using UnityEngine;

public class AxeCollectible : Collectible
{
    protected override void OnCollect(GameObject collector)
    {
        PlayerInventory inventory = collector.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning($"[AxeCollectible] {collector.name} não possui PlayerInventory.");
            return;
        }

        // Verifica apenas o inventário DESTE jogador — não bloqueia o P2
        if (inventory.HasTool(ToolType.Axe))
        {
            Debug.Log($"[AxeCollectible] {collector.name} já possui o Machado.");

            // Não coleta — item permanece no chão para o outro jogador
            return;
        }

        inventory.AddItem(itemData);
        Debug.Log($"[AxeCollectible] {collector.name} adquiriu o Machado!");
    }

    public override bool CanPlayerCollect(GameObject collector)
    {
        PlayerInventory inventory = collector.GetComponent<PlayerInventory>();
        if (inventory == null) return false;

        // Só pode coletar se ainda não tiver o machado
        return !inventory.HasTool(ToolType.Axe);
    }

}
