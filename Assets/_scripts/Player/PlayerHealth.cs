using System;
using UnityEngine;

/// <summary>
/// Sistema de vida do jogador. Implementa IDamageable para que
/// inimigos possam atacá-lo via EnemyTargetScanner.
///
/// Adicione este componente ao prefab do PlayerController.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Vida")]
    [SerializeField] private int maxHealth = 10;

    [Header("Invencibilidade")]
    [Tooltip("Tempo de invencibilidade (segundos) após receber dano.")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    #endregion

    // ─────────────────────────────────────────
    #region Estado

    public int   _currentHealth;
    private bool  _isDead;
    private float _invincibilityTimer;

    public int  CurrentHealth => _currentHealth;
    public bool IsDead        => _isDead;

    #endregion

    // ─────────────────────────────────────────
    #region Eventos

    /// <summary>Disparado ao receber dano. Parâmetro: vida atual.</summary>
    public event Action<int> OnDamaged;

    /// <summary>Disparado ao morrer.</summary>
    public event Action OnDeath;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private PlayerHitEffect _hitEffect;

    private void Awake()
    {
        _currentHealth = maxHealth;
        _hitEffect     = GetComponent<PlayerHitEffect>();
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    #endregion

    // ─────────────────────────────────────────
    #region IDamageable

    public void TakeDamage(int damage)
    {
        if (_isDead || _invincibilityTimer > 0f) return;

        _invincibilityTimer = invincibilityDuration;

        // O player não tem HP — o dano faz perder a Estrela
        if (Star.Instance != null)
            Star.Instance.Drop(transform.position);

        _hitEffect?.TriggerHit();
        OnDamaged?.Invoke(_currentHealth);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Morte

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"[PlayerHealth] {gameObject.name} morreu!");
        OnDeath?.Invoke();
    }

    #endregion

    // ─────────────────────────────────────────
    #region API Pública

    /// <summary>Restaura a vida (cura).</summary>
    public void Heal(int amount)
    {
        if (_isDead) return;
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
    }

    #endregion
}
