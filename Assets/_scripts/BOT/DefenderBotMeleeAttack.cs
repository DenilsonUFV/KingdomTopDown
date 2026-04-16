using UnityEngine;

/// <summary>
/// Ataque corpo a corpo do BOT defensor.
/// Causa dano diretamente ao IDamageable do inimigo.
/// </summary>
public class DefenderBotMeleeAttack : MonoBehaviour, IAttack
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Efeito Visual (opcional)")]
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

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, target.position, Quaternion.identity);

        Debug.Log($"[DefenderMelee] {gameObject.name} golpeou {target.name} por {damage}.");
    }

    #endregion
}
