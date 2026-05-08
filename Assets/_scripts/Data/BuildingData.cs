using UnityEngine;

[CreateAssetMenu(fileName = "Building_New", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identificação")]
    public string buildingName = "Nova Construção";

    [Header("Visual")]
    public Sprite spriteBuilt;       // sprite quando construída e saudável
    public Sprite spriteBuilding;    // sprite durante construção/reparo (andaime)
    public Sprite spriteSlot;        // sprite quando destruída / ainda não construída
    public Sprite coinIcon;          // ícone exibido nos slots de custo acima da construção

    [Header("Construção / Reconstrução")]
    public int   coinCost     = 10;
    public bool  needsBuilder = true;
    public float buildTime    = 10f;

    [Header("Evolução")]
    [Tooltip("Próximo nível desta construção. Null = nível máximo.")]
    public BuildingData nextLevel;

    [Header("Reparo (quando danificada mas não destruída)")]
    [Range(0f, 1f)]
    [Tooltip("Fração do coinCost cobrada para reparar.")]
    public float repairCostRatio = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Fração do buildTime usada para reparar.")]
    public float repairTimeRatio = 0.5f;

    public int   RepairCost => Mathf.Max(1, Mathf.RoundToInt(coinCost * repairCostRatio));
    public float RepairTime => buildTime * repairTimeRatio;
}
