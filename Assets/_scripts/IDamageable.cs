/// <summary>
/// Interface implementada por qualquer entidade que pode receber dano.
/// Usada pelo sistema de combate para que inimigos e defensores
/// possam atacar jogadores, bots e construções de forma genérica.
/// </summary>
public interface IDamageable
{
    /// <summary>Aplica dano à entidade.</summary>
    void TakeDamage(int damage);

    /// <summary>Vida atual.</summary>
    int CurrentHealth { get; }

    /// <summary>True se a entidade está morta.</summary>
    bool IsDead { get; }
}
