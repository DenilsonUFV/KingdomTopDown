using UnityEngine;

[CreateAssetMenu(fileName = "Item_New", menuName = "Collectibles/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identificação")]
    public string itemName = "Novo Item";
    public ItemType itemType = ItemType.Misc;

    [TextArea(2, 4)]
    public string description = "";

    [Header("Ferramenta (se for Tool)")]
    public ToolType toolType = ToolType.None;

    [Header("Visual")]
    public Sprite sprite = null;
    public Color tintColor = Color.white;

    [Header("Coleta")]
    public bool autoCollect = true;   // coleta ao tocar
    public float collectRadius = 0.5f;   // raio se autoCollect = false
    public AudioClip collectSound = null;

    [Header("Valor")]
    public int value = 1;      // moedas, cura, dano, etc.
    public float weight = 0f;     // para sistemas de inventário
    public bool isStackable = true;
    public int maxStack = 99;
}