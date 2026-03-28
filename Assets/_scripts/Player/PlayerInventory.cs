using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly List<ItemData> _items = new();

    // Evento para UI reagir
    public event Action OnItemAdded;

    public void AddItem(ItemData item)
    {
        if (item == null) return;
        _items.Add(item);
        OnItemAdded?.Invoke();
        Debug.Log($"[Inventory] {item.itemName} adicionado.");
    }

    public void RemoveItem(ItemData item)
    {
        _items.Remove(item);
    }

    public bool HasTool(ToolType toolType)
    {
        foreach (ItemData item in _items)
            if (item.itemType == ItemType.Tool && item.toolType == toolType)
                return true;

        return false;
    }

    public bool HasItem(ItemData item) => _items.Contains(item);
}
