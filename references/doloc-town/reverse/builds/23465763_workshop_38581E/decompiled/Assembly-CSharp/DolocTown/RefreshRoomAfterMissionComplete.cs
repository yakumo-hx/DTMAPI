namespace DolocTown;

public class RefreshRoomAfterMissionComplete : IMissionManagerComponent
{
	public void OnMissionRemoved(IMission mission)
	{
	}

	public void OnMissionStart(IMission mission)
	{
	}

	public void OnMissionCompleted(IMission mission)
	{
		DolocAPI.CurrentRoom?.SceneHandle?.RefreshCondition();
	}

	public void OnMissionChanged(GameEventType type, GameEventArgs args, IMission mission)
	{
	}
}
