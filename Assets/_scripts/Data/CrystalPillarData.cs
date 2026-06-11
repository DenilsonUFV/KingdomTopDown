using UnityEngine;

[CreateAssetMenu(fileName = "CrystalPillar_New", menuName = "Pillars/Crystal Pillar Data")]
public class CrystalPillarData : ScriptableObject
{
    [Header("Identificação")]
    public string pillarName = "Pilar de Cristal";

    [Header("Prefab Invocado")]
    [Tooltip("Qualquer prefab: BOT, construção, item etc.")]
    public GameObject spawnPrefab;

    [Header("Custo de Invocação")]
    public ResourceType resourceType = ResourceType.Coin;
    [Min(1)] public int resourceCost = 5;

    [Header("Visual")]
    [Tooltip("Ícone do recurso — usado nos slots da UI e na animação de voo.")]
    public Sprite resourceIcon;

    [Header("Invocação")]
    [Tooltip("Segundos entre o último recurso chegar e o BOT aparecer.")]
    public float spawnDelay = 2f;

    [Header("Cooldown entre Invocações")]
    [Tooltip("Segundos de espera após invocar antes de aceitar novos recursos.")]
    public float spawnCooldown = 15f;

    [Header("Reembolso")]
    [Tooltip("Prefab coletável spawnado fisicamente quando o jogador cancela o investimento.")]
    public GameObject refundPrefab;
}
