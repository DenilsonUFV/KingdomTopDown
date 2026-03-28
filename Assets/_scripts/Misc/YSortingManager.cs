using System.Collections.Generic;
using UnityEngine;

public class YSortingManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Singleton

    public static YSortingManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Registro

    private readonly List<YSorting> _sortingObjects = new List<YSorting>();

    public void Register(YSorting obj)
    {
        if (!_sortingObjects.Contains(obj))
            _sortingObjects.Add(obj);
    }

    public void Unregister(YSorting obj)
    {
        _sortingObjects.Remove(obj);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Update

    private void LateUpdate()
    {
        // Atualiza todos os objetos registrados em um único loop
        for (int i = 0; i < _sortingObjects.Count; i++)
        {
            if (_sortingObjects[i] != null)
                _sortingObjects[i].ApplySorting();
        }
    }

    #endregion
}
