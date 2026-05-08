using UnityEngine;

/// <summary>
/// Ataque corpo a corpo do inimigo.
/// Aplica dano diretamente ao IDamageable do alvo.
/// Adicione este componente a inimigos melee (sem projétil).
/// </summary>
public class EnemyMeleeAttack : MonoBehaviour, IAttack
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Efeito Visual (opcional)")]
    [Tooltip("Prefab de partícula/efeito spawnado no ponto de impacto.")]
    [SerializeField] private GameObject hitEffectPrefab;

    #endregion

    // ─────────────────────────────────────────
    #region IAttack

    public void PerformAttack(Transform target, int damage)
    {
        if (target == null) return;

        IDamageable dmg = target.GetComponent<IDamageable>()
                       ?? target.GetComponentInParent<IDamageable>();

        if (dmg == null || dmg.IsDead) return;

        dmg.TakeDamage(damage);

        HitFeedback feedback = target.GetComponent<HitFeedback>()
                            ?? target.GetComponentInParent<HitFeedback>();
        feedback?.ApplyHit(transform.position);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, target.position, Quaternion.identity);
    }

    #endregion
}
