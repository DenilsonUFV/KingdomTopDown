using Unity.Cinemachine;
using UnityEngine;

public class CinemachineMultiplayerSetup : MonoBehaviour
{
    // ─────────────────────────────────────────
    #region Configuração

    [SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float targetRadius = 1f;

    #endregion

    // ─────────────────────────────────────────
    #region Unity Callbacks

    private void OnEnable()
    {
        PlayerManager.OnPlayerJoined += AddTarget;
        PlayerManager.OnPlayerLeft += RemoveTarget;
    }

    private void OnDisable()
    {
        PlayerManager.OnPlayerJoined -= AddTarget;
        PlayerManager.OnPlayerLeft -= RemoveTarget;
    }

    private void Start()
    {
        // Registra jogadores já existentes na cena
        foreach (PlayerController player in PlayerManager.Players)
            AddTarget(player);
    }

    #endregion

    // ─────────────────────────────────────────
    #region Targets

    private void AddTarget(PlayerController player)
    {
        if (targetGroup == null) return;

        targetGroup.Targets.Add(new CinemachineTargetGroup.Target
        {
            Object = player.transform,
            Weight = targetWeight,
            Radius = targetRadius
        });

        Debug.Log($"[CinemachineSetup] {player.playerName} adicionado ao TargetGroup.");
    }

    private void RemoveTarget(PlayerController player)
    {
        if (targetGroup == null) return;

        for (int i = 0; i < targetGroup.Targets.Count; i++)
        {
            if (targetGroup.Targets[i].Object == player.transform)
            {
                targetGroup.Targets.RemoveAt(i);
                Debug.Log($"[CinemachineSetup] {player.playerName} removido do TargetGroup.");
                return;
            }
        }
    }

    #endregion
}