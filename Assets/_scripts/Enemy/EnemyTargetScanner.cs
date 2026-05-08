using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escaneia e prioriza alvos para um inimigo.
///
/// Regras:
///   - Lista de prioridade VAZIA → ataca o mais próximo de qualquer tipo.
///   - Lista com itens → percorre em ordem; dentro de cada tipo, ataca o mais próximo.
///   - Alvos mortos ou sem IDamageable são ignorados.
/// </summary>
public class EnemyTargetScanner : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Estado Interno

    private float            _detectionRadius;
    private List<TargetType> _priority;

    #endregion

    // ─────────────────────────────────────────
    #region Inicialização

    /// <summary>Chamado pelo EnemyBrain com os dados do EnemyData.</summary>
    public void Init(EnemyData data)
    {
        _detectionRadius = data.detectionRadius;
        _priority        = data.targetPriority;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Scan

    /// <summary>
    /// Retorna o Transform do melhor alvo no raio de detecção, ou null.
    /// </summary>
    public Transform FindBestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _detectionRadius);

        var candidates = new List<(Transform t, TargetType type)>();

        foreach (Collider2D col in hits)
        {
            if (col.gameObject == gameObject) continue;

            TargetType? type = ClassifyTarget(col.gameObject);
            if (type == null) continue;

            // Ignora alvos já mortos (só verifica IDamageable se ele existir —
            // alvos sem IDamageable ainda são válidos para perseguição)
            IDamageable dmg = col.GetComponent<IDamageable>()
                           ?? col.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsDead) continue;

            candidates.Add((col.transform, type.Value));
        }

        if (candidates.Count == 0) return null;

        // Sem lista de prioridade → mais próximo de qualquer tipo
        if (_priority == null || _priority.Count == 0)
            return GetNearest(candidates);

        // Com lista → percorre na ordem de prioridade
        foreach (TargetType priority in _priority)
        {
            var filtered = candidates.FindAll(c => c.type == priority);
            if (filtered.Count > 0)
                return GetNearest(filtered);
        }

        // Fallback: mais próximo de qualquer tipo não listado
        return GetNearest(candidates);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Helpers

    private TargetType? ClassifyTarget(GameObject go)
    {
        // GetComponentInParent sobe a hierarquia — cobre o caso do collider estar em filho do root.
        // DefenderBotHealth serve de fallback caso DefenderBotBrain não esteja no prefab ainda.
        if (go.GetComponentInParent<PlayerController>()  != null) return TargetType.Jogador;
        if (go.GetComponentInParent<DefenderBotBrain>()  != null) return TargetType.BotDefensor;
        if (go.GetComponentInParent<DefenderBotHealth>() != null) return TargetType.BotDefensor;
        if (go.GetComponentInParent<BotBrain>()          != null) return TargetType.BotConstrutor;
        if (go.GetComponentInParent<BuildingHealth>()    != null) return TargetType.Construcao;
        return null;
    }

    private Transform GetNearest(List<(Transform t, TargetType type)> list)
    {
        Transform best     = null;
        float     bestDist = float.MaxValue;

        foreach (var (t, _) in list)
        {
            float dist = Vector2.Distance(transform.position, t.position);
            if (dist < bestDist) { bestDist = dist; best = t; }
        }

        return best;
    }

    #endregion

    // ─────────────────────────────────────────
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }

    #endregion
}
