/// <summary>
/// Tipos de alvo para o sistema de prioridade de inimigos e defensores.
/// A ordem na lista de prioridade do EnemyData determina quem é atacado primeiro.
/// </summary>
public enum TargetType
{
    Jogador,        // PlayerController
    BotDefensor,    // DefenderBotBrain
    BotConstrutor,  // BotBrain (construtor)
    Construcao      // BuildingHealth (construções)
}
