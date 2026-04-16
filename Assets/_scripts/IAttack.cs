using UnityEngine;

/// <summary>
/// Interface de ataque implementada tanto por inimigos (melee/ranged)
/// quanto por BOTs defensores (melee/arqueiro).
/// </summary>
public interface IAttack
{
    /// <summary>Executa um ataque contra o alvo com o dano especificado.</summary>
    void PerformAttack(Transform target, int damage);
}
