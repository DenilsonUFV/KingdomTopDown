using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container de dados da sessão atual.
/// Não é singleton — crie uma instância e passe para quem precisar.
///
/// Fase 0: apenas estrutura em memória.
/// Fase 1: adicionar serialização JSON e persistência em disco.
///
/// Nota: Dictionary não é serializado pelo JsonUtility do Unity.
/// Para serialização futura, converter para List de pares chave/valor
/// ou usar um serializador externo (Newtonsoft.Json, etc.).
/// </summary>
[Serializable]
public class SessionData
{
    // ─────────────────────────────────────────
    #region Campos

    public int                         diaAtual        = 0;
    public Dictionary<ResourceType, int> recursosAtuais = new();
    public List<BuildingSnapshot>      construcoes     = new();

    #endregion

    // ─────────────────────────────────────────
    #region Snapshot de Construção

    /// <summary>
    /// Registro mínimo de uma construção — suficiente para recriar a cena futuramente.
    /// Campos adicionais (nível, HP, etc.) podem ser expandidos na Fase 1.
    /// </summary>
    [Serializable]
    public class BuildingSnapshot
    {
        public string        buildingName;   // nome definido em BuildingData
        public BuildingState estado;         // estado no momento da captura
        public Vector3       posicao;        // posição no mundo
        // nivelIndex reservado para Fase 1 — exige ID persistente por BuildingData
    }

    #endregion

    // ─────────────────────────────────────────
    #region API

    /// <summary>
    /// Lê o estado atual da sessão a partir dos singletons e da cena.
    /// Pode ser chamado a qualquer momento durante o jogo.
    /// </summary>
    public void CapturarEstadoAtual()
    {
        // Dia atual — fonte: DayNightCycle
        diaAtual = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayCount : 0;

        // Recursos — fonte: ResourceManager
        recursosAtuais.Clear();
        foreach (ResourceType tipo in Enum.GetValues(typeof(ResourceType)))
            recursosAtuais[tipo] = ResourceManager.Get(tipo);

        // Construções — fonte: cena (todas as instâncias de Building)
        construcoes.Clear();
        Building[] buildings = UnityEngine.Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (Building b in buildings)
        {
            construcoes.Add(new BuildingSnapshot
            {
                buildingName = b.Data?.buildingName ?? "(sem dados)",
                estado       = b.State,
                posicao      = b.transform.position
            });
        }

        Debug.Log($"[SessionData] Capturado — Dia: {diaAtual} | " +
                  $"Moedas: {recursosAtuais.GetValueOrDefault(ResourceType.Coin)} | " +
                  $"Madeira: {recursosAtuais.GetValueOrDefault(ResourceType.Wood)} | " +
                  $"Construções: {construcoes.Count}");
    }

    /// <summary>
    /// Aplica os dados desta sessão de volta ao jogo.
    /// Placeholder para o sistema de save/load da Fase 1.
    /// Implementação real exigirá: recriar construções, setar recursos, restaurar dia.
    /// </summary>
    public void Aplicar()
    {
        Debug.Log("[SessionData] Aplicar() não implementado — aguardando Fase 1 (save/load em disco).");
    }

    #endregion
}
