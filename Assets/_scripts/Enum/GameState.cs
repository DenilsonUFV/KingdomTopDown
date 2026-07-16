/// <summary>
/// Estados globais da sessão de jogo.
/// GameStateManager é o único ponto de verdade — consulte-o em vez de DayNightCycle.IsDay.
/// DayNightCycle continua sendo o driver de tempo (cronômetro); GameStateManager é quem expõe "em que fase estamos".
/// </summary>
public enum GameState
{
    MenuInicial,    // tela inicial, antes de qualquer gameplay
    Dia,            // fase diurna — construção e preparação
    Noite,          // fase noturna — defesa e sobrevivência
    Pausado,        // jogo congelado (timeScale = 0), aguardando retomada
    GameOver        // Estrela entregue ao inimigo — sessão encerrada
}
