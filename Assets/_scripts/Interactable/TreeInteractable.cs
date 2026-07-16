using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ItemSpawner))]
public class TreeInteractable : MonoBehaviour, IInteractable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Árvore")]
    [SerializeField] private int hitsToFall = 3;      // golpes para cair
    [SerializeField] private float hitCooldown = 0.5f;   // tempo entre golpes
    [SerializeField] private float regrowTime = 30f;    // tempo para renascer (0 = não renasce)
    [Tooltip("Se true, ao ser cortada dropa os itens e o GameObject é destruído permanentemente.")]
    [SerializeField] private bool destroyOnCut = false;

    [Header("Feedback Visual")]
    [SerializeField] private SpriteRenderer treeSR;
    [SerializeField] private Sprite spriteAlive;
    [SerializeField] private Sprite spriteStump;   // toco após cortar
    [SerializeField] private float shakeIntensity = 0.05f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private int _currentHits = 0;
    private bool _isFalling = false;
    private bool _isCut = false;
    private float _lastHitTime = -999f;

    private ItemSpawner _spawner;
    private Vector3 _originalPosition;

    // IInteractable
    public bool CanInteract => !_isFalling && !_isCut;
    public ToolType RequiredTool => ToolType.Axe;
    public string InteractionHint => "Cortar Árvore";

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Awake()
    {
        _spawner = GetComponent<ItemSpawner>();
        _originalPosition = transform.position;

        if (treeSR != null && spriteAlive != null)
            treeSR.sprite = spriteAlive;
    }

    #endregion

    // ─────────────────────────────────────────
    #region IInteractable

    public bool Interact(GameObject interactor)
    {
        if (!CanInteract) return false;

        // Bloqueado pelo cooldown — animação NÃO deve tocar
        if (Time.time - _lastHitTime < hitCooldown) return false;

        _lastHitTime = Time.time;
        _currentHits++;

        StartCoroutine(ShakeRoutine());

        Debug.Log($"[Tree] Golpe {_currentHits}/{hitsToFall}");

        if (_currentHits >= hitsToFall){
            StartCoroutine(FallRoutine());
        }

        // Golpe aceito — animação DEVE tocar
        return true;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Fall

    private IEnumerator FallRoutine()
    {
        _isFalling = true;

        // Animação de queda (shake final)
        yield return StartCoroutine(ShakeRoutine(intense: true));

        // Troca para sprite de toco
        if (treeSR != null && spriteStump != null)
            treeSR.sprite = spriteStump;

        _isCut = true;
        _isFalling = false;

        // Spawna madeira e moeda
        _spawner.Spawn();

        Debug.Log("[Tree] Árvore cortada!");

        if (destroyOnCut)
        {
            Destroy(gameObject);
            yield break;
        }

        // Renasce após tempo (se configurado)
        if (regrowTime > 0f)
            StartCoroutine(RegrowRoutine());
    }

    private IEnumerator RegrowRoutine()
    {
        yield return new WaitForSeconds(regrowTime);

        _currentHits = 0;
        _isCut = false;

        if (treeSR != null && spriteAlive != null)
            treeSR.sprite = spriteAlive;

        Debug.Log("[Tree] Árvore renasceu!");
    }

    #endregion

    // ─────────────────────────────────────────
    #region Feedback Visual

    private IEnumerator ShakeRoutine(bool intense = false)
    {
        float duration = intense ? 0.4f : 0.15f;
        float intensity = intense ? shakeIntensity * 3f : shakeIntensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = _originalPosition.x + Random.Range(-intensity, intensity);
            float y = _originalPosition.y + Random.Range(-intensity, intensity);
            transform.position = new Vector3(x, y, _originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = _originalPosition;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    #endregion
}
