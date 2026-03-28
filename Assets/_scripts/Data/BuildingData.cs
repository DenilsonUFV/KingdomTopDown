using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Building_New", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identificação")]
    public string buildingName = "Nova Construção";

    [Header("Visual")]
    public Sprite spriteBuilt;       // sprite quando construída
    public Sprite spriteBuilding;    // sprite durante construção (andaime)
    public Sprite spriteSlot;        // sprite do slot vazio

    [Header("Custo")]
    public int coinCost = 10;

    [Header("Construção")]
    public bool needsBuilder = true;   // false = fogueira→tenda
    public float buildTime = 10f;    // segundos para construir

    [Header("Evolução")]
    public BuildingData nextLevel;           // próximo nível (null = máximo)

    [Header("Slots filhos gerados ao construir")]
    public List<BuildingSlotConfig> childSlots = new();
}

[System.Serializable]
public class BuildingSlotConfig
{
    public Vector2 localOffset;    // posição relativa à construção
    public BuildingData defaultData;    // construção disponível neste slot
}
