public enum BuildingState
{
    Destroyed,          // HP = 0 ou nunca construída — precisa ser (re)construída
    WaitingFunds,       // recebendo moedas para construir ou reparar
    WaitingBuilder,     // financiada, aguardando BOT construtor
    UnderConstruction,  // BOT trabalhando (construção ou reparo)
    Built               // operacional
}
