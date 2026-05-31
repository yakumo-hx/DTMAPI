namespace DolocTown;

public class GameStatePluginStopAgentWhileModalState : GameStatePlugin
{
	public override void BeforeSwitch(IDolocGameState lst, IDolocGameState cur)
	{
		if (cur == null)
		{
			return;
		}
		if (cur.ForceShowOperationTip)
		{
			DolocAPI.uiSystem.sceneOperationTipManager.SetEnabled(value: true);
			DolocAPI.uiSystem.sceneOperationTipMultiManager.SetVisible(value: true);
		}
		if (cur.ForceHideOperationTip)
		{
			DolocAPI.uiSystem.sceneOperationTipManager.SetEnabled(value: false);
			DolocAPI.uiSystem.sceneOperationTipMultiManager.SetVisible(value: false);
		}
		DolocAPI.SetResidentUiInteractable(cur.EnableBasicTipInteract);
		if (!DolocAPI.DisableOverlayUi)
		{
			if (cur.ForceShowBasicTip)
			{
				DolocAPI.SetBasicTipVisible(value: true);
			}
			if (cur.ForceShowQuickInventory)
			{
				DolocAPI.SetQuickInventoryVisible(value: true);
			}
		}
		if (cur.ForceHideBasicTip)
		{
			DolocAPI.SetBasicTipVisible(value: false);
		}
		if (cur.ForceHideQuickInventory)
		{
			DolocAPI.SetQuickInventoryVisible(value: false);
		}
		if (lst is NormalGameState normalGameState)
		{
			if (cur is DolocGameUiState)
			{
				normalGameState.AgentController.ClearPhysicalStatus();
			}
			if (DolocAPI.archiveHandle?.MainFarm != null)
			{
				foreach (Building building in DolocAPI.archiveHandle.MainFarm.DM_building.Buildings)
				{
					building.SetNumberTipVisibleIfTouching(value: false);
				}
			}
			DolocAPI.uiSystem.GetContainerFromPool<AnimalStatusTip>(inScene: true)?.gameObject.SetActive(value: false);
		}
		else if (cur is NormalGameState)
		{
			if (DolocAPI.archiveHandle?.MainFarm != null)
			{
				foreach (Building building2 in DolocAPI.archiveHandle.MainFarm.DM_building.Buildings)
				{
					building2.SetNumberTipVisibleIfTouching(value: true);
				}
			}
			DolocAPI.uiSystem.GetContainerFromPool<AnimalStatusTip>(inScene: true)?.gameObject.SetActive(value: true);
		}
		if (lst != null && lst.ShouldPauseGame != cur.ShouldPauseGame)
		{
			if (cur.ShouldPauseGame)
			{
				DolocAPI.archiveHandle?.cityData.npcManager.PauseNpcMovers();
			}
			else
			{
				DolocAPI.archiveHandle?.cityData.npcManager.ResumeNpcMovers();
			}
		}
	}
}
