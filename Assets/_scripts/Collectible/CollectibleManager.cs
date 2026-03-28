using System.Collections.Generic;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static CollectibleManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Registro

    // Listas separadas por estado — só processa quem precisa
    private readonly List<CollectibleBounce> _bouncing = new();
    private readonly List<CollectibleBounce> _landed = new();

    public void RegisterBouncing(CollectibleBounce item)
    {
        if (!_bouncing.Contains(item)) _bouncing.Add(item);
        _landed.Remove(item);
    }

    public void RegisterLanded(CollectibleBounce item)
    {
        if (!_landed.Contains(item)) _landed.Add(item);
        _bouncing.Remove(item);
    }

    public void Unregister(CollectibleBounce item)
    {
        _bouncing.Remove(item);
        _landed.Remove(item);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Update Central

    private void Update()
    {
        // Processa bouncing
        for (int i = _bouncing.Count - 1; i >= 0; i--)
        {
            if (_bouncing[i] == null) { _bouncing.RemoveAt(i); continue; }
            _bouncing[i].TickBounce();
        }

        // Processa landed — magnet e separação
        for (int i = _landed.Count - 1; i >= 0; i--)
        {
            if (_landed[i] == null) { _landed.RemoveAt(i); continue; }
            _landed[i].TickLanded();
        }
    }

    private void LateUpdate()
    {
        // Visuals separados do logic
        for (int i = _bouncing.Count - 1; i >= 0; i--)
        {
            if (_bouncing[i] == null) continue;
            _bouncing[i].TickVisuals();
        }
    }

    #endregion
}
