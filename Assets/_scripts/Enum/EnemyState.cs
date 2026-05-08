/// <summary>
/// Estados da máquina de estados dos inimigos.
/// </summary>
public enum EnemyState
{
    Idle,               // Parado, aguardando
    Patrulhando,        // Andando aleatoriamente na área do spawn
    Perseguindo,        // Seguindo um alvo detectado
    Atacando,           // Em range de ataque, executando ataques
    Recuando,           // Voltando ao ponto de spawn (amanheceu)
    BuscandoEstrela,    // Indo pegar a Estrela caída no chão
    CarregandoEstrela,  // Carregando a Estrela de volta ao SpawnPoint
    Morto               // Morto — aguardando destruição
}
