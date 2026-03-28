using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Collectible : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Dados do Item")]
    [SerializeField] protected ItemData itemData;

    [Header("Animação de Bob (flutuação)")]
    [SerializeField] private bool useBobAnimation = true;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Sorting")]
    [SerializeField] private float yOffset = 0f;

    #endregion

    // ─────────────────────────────────────────
    #region Referências e Estado

    protected SpriteRenderer _spriteRenderer;
    protected Collider2D _collider;
    protected AudioSource _audioSource;

    private Vector3 _startPosition;
    private bool _isCollected = false;

    // Evento disparado ao coletar — qualquer sistema pode ouvir
    public static event Action<Collectible, GameObject> OnCollected;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    protected virtual void Awake()
    {
        // Busca no filho ItemVisual
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _audioSource = GetComponent<AudioSource>();
        _startPosition = transform.position;
    }

    protected virtual void Start()
    {
        ApplyItemData();
    }

    protected virtual void Update()
    {
        if (useBobAnimation)
            BobAnimation();
    }

    // Coleta automática ao tocar
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isCollected) return;
        if (!other.CompareTag("Player")) return;
        if (itemData == null) return;
        if (!itemData.autoCollect) return;

        Collect(other.gameObject);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Setup

    /// <summary>
    /// Aplica os dados do ScriptableObject ao GameObject.
    /// </summary>
    private void ApplyItemData()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[Collectible] ItemData não atribuído em {gameObject.name}");
            return;
        }

        // Aplica sprite e cor
        _spriteRenderer.sprite = itemData.sprite;
        _spriteRenderer.color = itemData.tintColor;

        // Ajusta o collider como trigger
        _collider.isTrigger = true;

        // YSorting automático
        _spriteRenderer.sortingOrder = Mathf.RoundToInt(
            -(transform.position.y + yOffset) * 100
        );

        // Nome do GameObject no Editor (facilita debug)
        gameObject.name = $"Collectible_{itemData.itemName}";
    }

    #endregion

    // ─────────────────────────────────────────
    #region Collect

    /// <summary>
    /// Inicia o fluxo de coleta. Pode ser chamado externamente
    /// para coleta manual (ex: pressionar tecla perto do item).
    /// </summary>
    public void Collect(GameObject collector)
    {
        if (_isCollected) return;

        _isCollected = true;
        _collider.enabled = false;

        // Som de coleta
        PlayCollectSound();

        // Lógica específica de cada subclasse
        OnCollect(collector);

        // Dispara evento global
        OnCollected?.Invoke(this, collector);

        // Destrói o GameObject
        Destroy(gameObject);
    }

    /// <summary>
    /// Verifica se um jogador específico pode coletar este item.
    /// Subclasses sobrescrevem para adicionar restrições.
    /// </summary>
    public virtual bool CanPlayerCollect(GameObject collector) => true;

    /// <summary>
    /// Implementado por cada subclasse com a lógica específica.
    /// Ex: CoinCollectible adiciona moedas, HealthCollectible cura o player.
    /// </summary>
    protected abstract void OnCollect(GameObject collector);

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private void BobAnimation()
    {
        float newY = _startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void PlayCollectSound()
    {
        if (itemData.collectSound == null) return;

        // Cria um AudioSource temporário que não morre com o objeto
        AudioSource.PlayClipAtPoint(itemData.collectSound, transform.position);
    }

    /// <summary>
    /// Permite trocar o ItemData em runtime (ex: loot gerado proceduralmente).
    /// </summary>
    public void SetItemData(ItemData data)
    {
        itemData = data;
        ApplyItemData();
    }

    public ItemData GetItemData() => itemData;

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (itemData == null) return;

        // Mostra o raio de coleta manual no Editor
        if (!itemData.autoCollect)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, itemData.collectRadius);
        }
    }

    #endregion
}