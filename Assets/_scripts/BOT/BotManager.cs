using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BotManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static BotManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Registro

    private readonly List<BotBrain> _bots = new();

    public void Register(BotBrain bot)
    {
        if (!_bots.Contains(bot)) _bots.Add(bot);
    }

    public void Unregister(BotBrain bot)
    {
        _bots.Remove(bot);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Distribuição de Tarefas

    private const int MaxBotsPerBuilding = 3;

    /// <summary>
    /// Chamado pelo Building quando está WaitingBuilder.
    /// Envia até 3 BOTs disponíveis para a construção.
    /// </summary>
    public void RequestBuilders(Building building)
    {
        // BOTs já assignados a esta construção
        int assigned = _bots.Count(b => b.TargetBuilding == building);
        int needed = MaxBotsPerBuilding - assigned;

        if (needed <= 0) return;

        // Pega BOTs disponíveis ordenados por distância
        List<BotBrain> available = _bots
            .Where(b => b.IsAvailable)
            .OrderBy(b => Vector2.Distance(b.transform.position, building.transform.position))
            .Take(needed)
            .ToList();

        foreach (BotBrain bot in available)
            bot.AssignBuilding(building);

        Debug.Log($"[BotManager] {available.Count} BOTs enviados para {building.Data?.buildingName}");
    }

    /// <summary>
    /// Chamado quando uma construção termina — libera os BOTs.
    /// </summary>
    public void ReleaseBuilders(Building building)
    {
        foreach (BotBrain bot in _bots)
            if (bot.TargetBuilding == building)
                bot.ReleaseBuilding();
    }

    /// <summary>
    /// Verifica se há construções aguardando um BOT e atribui este BOT à mais próxima.
    /// Chamado quando um novo BOT spawna ou termina uma obra.
    /// Retorna true se uma construção foi encontrada e atribuída.
    /// </summary>
    public bool TryAssignToPendingBuilding(BotBrain bot)
    {
        Building[] all     = FindObjectsByType<Building>(FindObjectsSortMode.None);
        Building   nearest = null;
        float      bestDist = float.MaxValue;

        foreach (Building b in all)
        {
            if (b.State != BuildingState.WaitingBuilder) continue;

            int assigned = _bots.Count(other => other.TargetBuilding == b);
            if (assigned >= MaxBotsPerBuilding) continue;

            float dist = Vector2.Distance(bot.transform.position, b.transform.position);
            if (dist < bestDist) { bestDist = dist; nearest = b; }
        }

        if (nearest == null) return false;
        bot.AssignBuilding(nearest);
        return true;
    }

    #endregion
}
