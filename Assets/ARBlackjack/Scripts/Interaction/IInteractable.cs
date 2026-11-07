namespace ARBlackjack.Interaction
{
    public interface IInteractable
    {
        bool IsActive { get; }
        bool RepeatsWhileHeld { get; }
        void OnHoverEnter();
        void OnHoverExit();
        void OnSelect(int playerIndex);
    }

    public interface ICustomDwellInteractable
    {
        float DwellDuration { get; }
        void OnDwellProgress(float normalizedProgress);
        void OnDwellCancelled();
    }
}
