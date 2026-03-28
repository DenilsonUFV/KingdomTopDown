using System;
using UnityEngine;

public class CoinFlyEffect : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [SerializeField] private float flySpeed = 5f;
    [SerializeField] private float arcHeight = 1f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _progress = 0f;
    private Action _onArrive;
    private bool _flying = false;

    #endregion

    // ─────────────────────────────────────────
    #region API

    public static CoinFlyEffect Spawn(Vector3 from, Vector3 to, Action onArrive = null)
    {
        // Usa o prefab de moeda existente — só o visual, sem Collectible
        GameObject go = new GameObject("CoinFly");
        go.transform.position = from;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Dynamic";
        sr.sortingOrder = 10;

        CoinFlyEffect fly = go.AddComponent<CoinFlyEffect>();
        fly.Launch(from, to, onArrive);
        return fly;
    }

    public void Launch(Vector3 from, Vector3 to, Action onArrive)
    {
        _startPos = from;
        _targetPos = to;
        _onArrive = onArrive;
        _progress = 0f;
        _flying = true;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Update

    private void Update()
    {
        if (!_flying) return;

        _progress += flySpeed * Time.deltaTime;

        // Arco parabólico entre start e target
        Vector3 linear = Vector3.Lerp(_startPos, _targetPos, _progress);
        float arc = Mathf.Sin(_progress * Mathf.PI) * arcHeight;

        transform.position = linear + Vector3.up * arc;

        if (_progress >= 1f)
        {
            _flying = false;
            _onArrive?.Invoke();
            Destroy(gameObject);
        }
    }

    #endregion
}
