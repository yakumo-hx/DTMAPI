namespace DolocTown;

public interface IMissionManagerComponent
{
	void OnMissionStart(IMission mission);

	void OnMissionCompleted(IMission mission);

	void OnMissionRemoved(IMission mission);

	void OnMissionChanged(GameEventType type, GameEventArgs args, IMission mission);
}
