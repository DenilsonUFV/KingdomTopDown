using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    public static event Action<PlayerController> OnPlayerJoined;
    public static event Action<PlayerController> OnPlayerLeft;

    #endregion

    // ─────────────────────────────────────────
    #region Registro

    private readonly List<PlayerController> _players = new();

    public static IReadOnlyList<PlayerController> Players => Instance._players;

    public static void Register(PlayerController player)
    {
        if (player == null) return;
        if (Instance == null)
        {
            Debug.LogError("[PlayerManager] Instance é null ao registrar!");
            return;
        }
        if (Instance._players.Contains(player)) return;

        Instance._players.Add(player);
        OnPlayerJoined?.Invoke(player);

        //Debug.Log($"[PlayerManager] {player.playerName} entrou. Total: {Instance._players.Count}");
    }

    public static void Unregister(PlayerController player)
    {
        if (Instance == null) return;
        if (!Instance._players.Contains(player)) return;

        Instance._players.Remove(player);
        OnPlayerLeft?.Invoke(player);

        Debug.Log($"[PlayerManager] {player.playerName} saiu. Total: {Instance._players.Count}");
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    public static PlayerController GetNearest(Vector2 position)
    {
        if (Instance == null) return null;

        PlayerController nearest = null;
        float bestDist = float.MaxValue;

        foreach (PlayerController player in Instance._players)
        {
            if (player == null) continue;
            float dist = Vector2.Distance(position, player.transform.position);
            if (dist < bestDist) { bestDist = dist; nearest = player; }
        }

        return nearest;
    }

    public static PlayerController GetNearestInRadius(Vector2 position, float radius)
    {
        if (Instance == null) return null;

        PlayerController nearest = null;
        float bestDist = float.MaxValue;

        foreach (PlayerController player in Instance._players)
        {
            if (player == null) continue;
            float dist = Vector2.Distance(position, player.transform.position);
            if (dist <= radius && dist < bestDist) { bestDist = dist; nearest = player; }
        }

        return nearest;
    }

    #endregion
}
