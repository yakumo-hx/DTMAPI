namespace DolocTown;

public class GameStatePluginUiBackground : GameStatePlugin
{
	private bool isShowingPostProcessing;

	private void SetEnabled(bool value)
	{
		if (value != isShowingPostProcessing)
		{
			isShowingPostProcessing = value;
			DolocAPI.SetPPM_FlowPoints(isShowingPostProcessing);
			DolocAPI.SetSceneOperationTipEnabled(!isShowingPostProcessing);
		}
	}

	public override void AfterSwitch(IDolocGameState lst, IDolocGameState cur)
	{
		SetEnabled(cur is DolocGameUiState dolocGameUiState && dolocGameUiState.ShowFlowPoints);
	}
}
