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

    #endregion
}
