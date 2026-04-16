using UnityEngine;

/// <summary>
/// Ataque à distância do inimigo.
/// Instancia um Projectile que viaja em direção ao alvo.
/// Adicione este componente a inimigos ranged (arqueiros, magos, etc.).
/// </summary>
public class EnemyRangedAttack : MonoBehaviour, IAttack
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Projétil")]
    [Tooltip("Prefab com o componente Projectile.")]
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private float projectileSpeed = 8f;

    [Header("Ponto de Disparo")]
    [Tooltip("Se vazio, usa a posição do próprio GameObject.")]
    [SerializeField] private Transform firePoint;

    #endregion

    // ─────────────────────────────────────────
    #region IAttack

    public void PerformAttack(Transform target, int damage)
    {
        if (target == null || projectilePrefab == null) return;

        Vector3 origin     = firePoint != null ? firePoint.position : transform.position;
        Vector2 targetCenter = CombatUtils.GetCenter(target);
        Vector2 dir          = (targetCenter - (Vector2)origin).normalized;

        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        Projectile projectile = proj.GetComponent<Projectile>();

        if (projectile != null)
            projectile.Init(dir, projectileSpeed, damage, gameObject);

        Debug.Log($"[EnemyRanged] {gameObject.name} disparou em {target.name}.");
    }

    #endregion
}
