public enum BuildingState
{
    Slot,           // espaço vazio disponível
    WaitingFunds,   // aguardando moedas (parcialmente financiado)
    WaitingBuilder, // financiado, aguardando BOT construtor
    UnderConstruction, // BOT construindo
    Built           // construção completa
}