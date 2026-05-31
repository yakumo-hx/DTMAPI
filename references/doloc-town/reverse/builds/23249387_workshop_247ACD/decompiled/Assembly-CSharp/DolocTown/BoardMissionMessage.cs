using Cysharp.Threading.Tasks;

namespace DolocTown;

public class BoardMissionMessage : IMissionManagerComponent
{
	public void OnMissionStart(IMission mission)
	{
	}

	public void OnMissionCompleted(IMission mission)
	{
		MissionWithBoard boardMission = mission as MissionWithBoard;
		if (boardMission != null)
		{
			UniTask.DelayFrame(1).ContinueWith(delegate
			{
				DolocAPI.BroadcastString(GameEventType.BOARD_MISSION_FINISH, boardMission.Id);
			}).Forget();
		}
	}

	public void OnMissionRemoved(IMission mission)
	{
	}

	public void OnMissionChanged(GameEventType type, GameEventArgs args, IMission mission)
	{
	}
}
