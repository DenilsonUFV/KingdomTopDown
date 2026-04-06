using System;
using UnityEngine;

public class CoinFlyEffect : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [SerializeField] private float flySpeed = 4f;
    [SerializeField] private float arcHeight = 0.8f;

    // Tempo que a moeda fica visível ao chegar antes de sumir
    [SerializeField] private float lingerTime = 0.3f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private float _progress = 0f;
    private Action _onArrive;
    private bool _flying = false;

    private SpriteRenderer _sr;

    #endregion

    // ─────────────────────────────────────────
    #region API

    /// <summary>
    /// Spawna uma moeda voando de from até to.
    /// Requer o sprite da moeda para exibir.
    /// </summary>
    public static CoinFlyEffect Spawn(
        Vector3 from,
        Vector3 to,
        Sprite coinSprite,
        Action onArrive = null,
        float flySpeed = 4f,
        float arcHeight = 0.8f)
    {
        GameObject go = new GameObject("CoinFly");
        go.transform.position = from;

        // SpriteRenderer com o sprite da moeda
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite;
        sr.sortingLayerName = "Dynamic";
        sr.sortingOrder = 10;

        CoinFlyEffect fly = go.AddComponent<CoinFlyEffect>();
        fly._sr = sr;
        fly.flySpeed = flySpeed;
        fly.arcHeight = arcHeight;
        fly.Launch(from, to, onArrive);

        return fly;
    }

    private void Launch(Vector3 from, Vector3 to, Action onArrive)
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

        // Posição linear interpolada
        Vector3 linear = Vector3.Lerp(_startPos, _targetPos, _progress);

        // Arco parabólico
        float arc = Mathf.Sin(Mathf.Clamp01(_progress) * Mathf.PI) * arcHeight;
        transform.position = linear + Vector3.up * arc;

        // Escala levemente para dar sensação de profundidade
        float scale = Mathf.Lerp(1f, 0.6f, _progress);
        transform.localScale = Vector3.one * scale;

        if (_progress >= 1f)
        {
            _flying = false;
            _onArrive?.Invoke();

            // Fica visível por lingerTime antes de sumir
            Destroy(gameObject, lingerTime);
        }
    }

    #endregion
}
