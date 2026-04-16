using UnityEngine;

/// <summary>
/// Ataque à distância (arco) do BOT defensor arqueiro.
/// Instancia um Projectile que viaja em direção ao inimigo.
/// </summary>
public class DefenderBotArcherAttack : MonoBehaviour, IAttack
{
    // ─────────────────────────────────────────
    #region Configuração

    [Header("Projétil")]
    [Tooltip("Prefab com o componente Projectile.")]
    [SerializeField] private GameObject arrowPrefab;

    [SerializeField] private float arrowSpeed = 10f;

    [Header("Ponto de Disparo")]
    [Tooltip("Se vazio, usa a posição do próprio GameObject.")]
    [SerializeField] private Transform firePoint;

    #endregion

    // ─────────────────────────────────────────
    #region IAttack

    public void PerformAttack(Transform target, int damage)
    {
        if (target == null || arrowPrefab == null) return;

        Vector3 origin       = firePoint != null ? firePoint.position : transform.position;
        Vector2 targetCenter = CombatUtils.GetCenter(target);
        Vector2 dir          = (targetCenter - (Vector2)origin).normalized;

        GameObject arrow = Instantiate(arrowPrefab, origin, Quaternion.identity);
        Projectile projectile = arrow.GetComponent<Projectile>();

        if (projectile != null)
            projectile.Init(dir, arrowSpeed, damage, gameObject);

        Debug.Log($"[DefenderArcher] {gameObject.name} disparou flecha em {target.name}.");
    }

    #endregion
}
