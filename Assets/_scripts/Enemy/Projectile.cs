using UnityEngine;

/// <summary>
/// Projétil genérico (flecha, pedra, bola de fogo, etc.).
/// Usado tanto por inimigos ranged quanto por BOTs arqueiros.
///
/// Configuração do prefab:
///   - Rigidbody2D (gravityScale = 0 é setado pelo Init)
///   - Collider2D com Is Trigger = true
///   - Sprite no sprite renderer (opcionalmente com rotação)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Alcance e Colisão")]
    [SerializeField] private float maxRange = 15f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    private Rigidbody2D _rb;
    private int         _damage;
    private GameObject  _shooter;   // referência direta — mais confiável que tag
    private Vector3     _startPosition;
    private bool        _hasHit;

    #endregion

    // ─────────────────────────────────────────
    #region Inicialização

    /// <summary>
    /// Configura e dispara o projétil.
    /// Chamado imediatamente após o Instantiate.
    /// </summary>
    private void Awake()
    {
        // Garante que o collider é sempre trigger — independente da config do prefab
        if (TryGetComponent(out Collider2D col))
            col.isTrigger = true;
    }

    /// <param name="shooter">GameObject do atirador — ignorado na colisão.</param>
    public void Init(Vector2 direction, float speed, int damage, GameObject shooter)
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
        _rb.linearVelocity = direction * speed;

        _damage        = damage;
        _shooter       = shooter;
        _startPosition = transform.position;

        // Rotaciona o sprite na direção do disparo
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void Update()
    {
        if (_hasHit) return;

        // Autodestrói ao ultrapassar o alcance máximo
        if (Vector3.Distance(_startPosition, transform.position) >= maxRange)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;

        // Não colide com o próprio atirador nem com seus filhos
        if (_shooter != null &&
            (other.gameObject == _shooter || other.transform.IsChildOf(_shooter.transform))) return;

        IDamageable dmg = other.GetComponent<IDamageable>()
                       ?? other.GetComponentInParent<IDamageable>();

        if (dmg == null || dmg.IsDead) return;

        _hasHit = true;
        dmg.TakeDamage(_damage);

        HitFeedback feedback = other.GetComponent<HitFeedback>()
                            ?? other.GetComponentInParent<HitFeedback>();
        feedback?.ApplyHit(transform.position);

        Destroy(gameObject);
    }

    #endregion
}
