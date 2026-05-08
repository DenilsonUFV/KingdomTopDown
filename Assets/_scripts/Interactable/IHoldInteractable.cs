/// <summary>
/// Interface para objetos que respondem a "segurar" o botão de interação.
/// O InteractionSystem dispara OnHoldTick() a intervalos regulares enquanto o botão é mantido.
/// </summary>
public interface IHoldInteractable
{
    bool CanHoldInteract { get; }
    void OnHoldTick(UnityEngine.GameObject interactor);
}
