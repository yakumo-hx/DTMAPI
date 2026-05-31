namespace DolocTown;

public interface IInteractable
{
	int Priority => 0;

	int InteractableLayer => 0;

	bool OnlyTouch { get; }

	bool CanInteractContinues { get; }

	void OnTouch();

	void OnDisTouch();

	void OnInteract();
}
