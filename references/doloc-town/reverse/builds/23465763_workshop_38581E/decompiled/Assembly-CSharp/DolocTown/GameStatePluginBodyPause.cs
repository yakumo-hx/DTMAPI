namespace DolocTown;

public class GameStatePluginBodyPause : GameStatePlugin
{
	public override void AfterSwitch(IDolocGameState lst, IDolocGameState cur)
	{
		if (lst is NormalGameState && cur is DolocGameUiState)
		{
			DolocAPI.agent.Pause = true;
		}
		else if (cur is NormalGameState && DolocAPI.agent.Pause)
		{
			DolocAPI.agent.Pause = false;
		}
	}
}
