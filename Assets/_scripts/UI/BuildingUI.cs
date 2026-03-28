using UnityEngine;
using TMPro;

public class BuildingUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI progressText;

    public void Refresh(Building building)
    {
        if (building.Data?.nextLevel == null)
        {
            root?.SetActive(false);
            return;
        }

        root?.SetActive(true);

        int cost = building.Data.nextLevel.coinCost;
        int invested = building.CoinsInvested;
        int remaining = building.CoinsRemaining;

        // Mostra custo total ou progresso parcial
        if (invested <= 0)
        {
            if (costText) costText.text = $"{cost}🪙";
            if (progressText) progressText.text = "";
        }
        else
        {
            if (costText) costText.text = $"{remaining} restantes";
            if (progressText) progressText.text = $"{invested}/{cost}";
        }
    }
}
