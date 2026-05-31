using DolocTown.Config.Room;

namespace DolocTown;

public interface IGate
{
	bool AvailableToMotor { get; }

	bool NeedInteract { get; }

	bool KeepHorizontalSpeed { get; }

	bool KeepVerticalSpeed { get; }

	PortalInteractKey InteractKey { get; }

	void OnTouch();

	void OnDisTouch();

	void OnInteract();

	string GetInteractKeyName()
	{
		return InteractKey switch
		{
			PortalInteractKey.Enter => DolocAPI.UserInput.NormalRoomInteractActionName, 
			PortalInteractKey.Exit => DolocAPI.UserInput.NormalRoomInteractQuitActionName, 
			_ => DolocAPI.UserInput.GlobalInteractActionName, 
		};
	}
}
