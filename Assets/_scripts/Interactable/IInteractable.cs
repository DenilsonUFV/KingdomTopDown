using UnityEngine;

public interface IInteractable
{
    bool CanInteract { get; }
    ToolType RequiredTool { get; }
    string InteractionHint { get; }

    // Retorna true se a ação foi executada de fato
    bool Interact(GameObject interactor);
}