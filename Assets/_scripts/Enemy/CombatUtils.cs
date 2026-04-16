using UnityEngine;

/// <summary>
/// Utilitários compartilhados pelo sistema de combate.
/// </summary>
public static class CombatUtils
{
    /// <summary>
    /// Retorna o centro do Collider2D do alvo (bounds.center).
    /// Se não houver collider, usa transform.position como fallback.
    /// Isso garante mira e cálculo de distância corretos quando o collider
    /// não está no pivot do sprite.
    /// </summary>
    public static Vector2 GetCenter(Transform target)
    {
        if (target == null) return Vector2.zero;

        // Busca collider no próprio objeto ou em qualquer filho
        Collider2D col = target.GetComponent<Collider2D>()
                      ?? target.GetComponentInChildren<Collider2D>();

        return col != null ? (Vector2)col.bounds.center : (Vector2)target.position;
    }
}
