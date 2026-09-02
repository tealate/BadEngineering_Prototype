using UnityEngine;

namespace BadEngineering.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(GameObject interactor);
        bool TryInteract(GameObject interactor);
    }
}
