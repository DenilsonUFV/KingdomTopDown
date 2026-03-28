using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static ResourceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    [Header("Recursos Iniciais")]
    [SerializeField] private int startCoins = 0;
    [SerializeField] private int startWood = 0;
    [SerializeField] private int startOre = 0;
    [SerializeField] private int startFood = 0;

    private int _coins;
    private int _wood;
    private int _ore;
    private int _food;

    // Eventos — UI e sistemas ouvem daqui
    public static event Action<ResourceType, int> OnResourceChanged;  // tipo + valor atual
    public static event Action<ResourceType, int> OnResourceAdded;    // tipo + valor adicionado

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Start()
    {
        _coins = startCoins;
        _wood = startWood;
        _ore = startOre;
        _food = startFood;
    }

    #endregion

    // ─────────────────────────────────────────
    #region API

    public static void Add(ResourceType type, int amount)
    {
        if (Instance == null) return;
        if (amount <= 0) return;

        Instance.AddInternal(type, amount);
    }

    public static bool Spend(ResourceType type, int amount)
    {
        if (Instance == null) return false;
        return Instance.SpendInternal(type, amount);
    }

    public static int Get(ResourceType type)
    {
        if (Instance == null) return 0;
        return Instance.GetInternal(type);
    }

    public static bool Has(ResourceType type, int amount)
    {
        return Get(type) >= amount;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Internals

    private void AddInternal(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Coin: _coins += amount; break;
            case ResourceType.Wood: _wood += amount; break;
            case ResourceType.Ore: _ore += amount; break;
            case ResourceType.Food: _food += amount; break;
        }

        OnResourceAdded?.Invoke(type, amount);
        OnResourceChanged?.Invoke(type, GetInternal(type));

        Debug.Log($"[Resources] +{amount} {type} | Total: {GetInternal(type)}");
    }

    private bool SpendInternal(ResourceType type, int amount)
    {
        if (GetInternal(type) < amount)
        {
            Debug.Log($"[Resources] {type} insuficiente. Tem: {GetInternal(type)} | Precisa: {amount}");
            return false;
        }

        switch (type)
        {
            case ResourceType.Coin: _coins -= amount; break;
            case ResourceType.Wood: _wood -= amount; break;
            case ResourceType.Ore: _ore -= amount; break;
            case ResourceType.Food: _food -= amount; break;
        }

        OnResourceChanged?.Invoke(type, GetInternal(type));
        return true;
    }

    private int GetInternal(ResourceType type) => type switch
    {
        ResourceType.Coin => _coins,
        ResourceType.Wood => _wood,
        ResourceType.Ore => _ore,
        ResourceType.Food => _food,
        _ => 0
    };

    #endregion
}
