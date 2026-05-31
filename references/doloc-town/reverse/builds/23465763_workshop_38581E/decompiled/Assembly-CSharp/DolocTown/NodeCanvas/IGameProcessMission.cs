namespace DolocTown.NodeCanvas;

public interface IGameProcessMission
{
	bool SendMessage(GameMessage message, bool shouldUseMessage = true);
}
