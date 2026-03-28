using UnityEngine;

public class BuildingSlot : MonoBehaviour
{
    [SerializeField] private BuildingData _availableBuilding;
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private BuildingUI _ui;

    public static BuildingSlot Spawn(Vector3 position, BuildingData data)
    {
        GameObject go = new GameObject($"Slot_{data?.buildingName ?? "Empty"}");
        go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Interactable");

        BuildingSlot slot = go.AddComponent<BuildingSlot>();
        slot._availableBuilding = data;

        // Collider para interação
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 1f;
        col.isTrigger = true;

        // Converte o slot em Building quando financiado
        slot.Initialize();
        return slot;
    }

    private void Initialize()
    {
        // Troca o slot por uma Building real ao ser totalmente financiado
        Building building = gameObject.AddComponent<Building>();
        Destroy(this);
    }
}
