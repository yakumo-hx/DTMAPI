using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Festival;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FestivalState : NormalGameState
{
	private FestivalInfo currentFestivalInfo;

	private RSTimer cancleNpcInvokeTimer = new RSTimer();

	public override bool ForceHideBasicTip => true;

	public override bool ForceHideQuickInventory => true;

	public override bool ForceShowOperationTip => true;

	public override bool DisableUseItem => true;

	public FestivalState(AgentControllerState agentController, Transform dungeonContainer, GameStateMachine userInput)
		: base(agentController, dungeonContainer, userInput, shouldPauseGame: true, shouldLateUpdate: true, supportCutscenes: true)
	{
	}

	protected override void UpdateAgent(float deltaTime)
	{
		base.AgentController.OnUpdateInFestivalState(deltaTime);
	}

	public override void OnUpdate(float deltaTime)
	{
		base.OnUpdate(deltaTime);
		FestivalInfo festivalInfo = currentFestivalInfo;
		if (festivalInfo == null || !festivalInfo.DisableNpcActing || !cancleNpcInvokeTimer.Tick(deltaTime))
		{
			return;
		}
		foreach (Npc allNpc in DolocAPI.archiveHandle.cityData.npcManager.AllNpcs)
		{
			allNpc.CancelInvoke();
		}
	}

	public override void OnResume()
	{
		base.OnResume();
		cancleNpcInvokeTimer.Reset();
	}

	public override void OnExit()
	{
		base.OnExit();
		ClearFestivalState();
		currentFestivalInfo = null;
	}

	public void InitNpcInFestival(string npcName)
	{
		if (currentFestivalInfo != null && currentFestivalInfo.NpcInfos.TryGetValue(npcName, out var value) && DolocAPI.QueryNpc(npcName, out var npc))
		{
			InitNpcInFestival(npc, value);
		}
	}

	private void InitNpcInFestival(Npc npc, NpcFestivalInfo npcFestivalInfo)
	{
		if (npc != null && npcFestivalInfo != null)
		{
			npc.overrideVisible = true;
			npc.overrideFaceLeft = npcFestivalInfo.FaceLeft;
			npc.ManualSetToMarkPoint(npcFestivalInfo.MarkPoint);
			DolocAPI.archiveHandle.cityData.dialogueManager.SetOverrideEntrance(npcFestivalInfo.DialogueNode, npc.NpcName);
			npc.RefreshNpcEventStatus();
		}
	}

	private void InitFestivalState(FestivalInfo festivalInfo)
	{
		if (festivalInfo == null)
		{
			return;
		}
		DolocAPI.gameStateManager.agentController.droneController.ForceHide = festivalInfo.HideDrone;
		foreach (Npc allNpc in DolocAPI.archiveHandle.cityData.npcManager.AllNpcs)
		{
			allNpc.disableFreeActing = festivalInfo.DisableNpcActing;
			if (festivalInfo.NpcInfos.TryGetValue(allNpc.NpcName, out var value) && value.AutoInit)
			{
				InitNpcInFestival(allNpc, value);
				continue;
			}
			allNpc.overrideVisible = false;
			allNpc.disableEventFlag = false;
			allNpc.Renderer = null;
		}
	}

	private void ClearFestivalState()
	{
		DolocAPI.gameStateManager.agentController.droneController.ForceHide = false;
		if (!DolocAPI.IsDataLoaded)
		{
			return;
		}
		foreach (Npc allNpc in DolocAPI.archiveHandle.cityData.npcManager.AllNpcs)
		{
			allNpc.overrideVisible = null;
			allNpc.overrideFaceLeft = null;
			allNpc.disableFreeActing = false;
			allNpc.disableEventFlag = false;
			allNpc.InvokeSchedule();
			DolocAPI.archiveHandle.cityData.dialogueManager.RemoveOverrideEntrance(allNpc.NpcName);
			allNpc.RefreshNpcEventStatus();
		}
	}

	public bool AppendFestival(string festivalName)
	{
		FestivalInfo orDefault = DolocConfig.Tables.TbFestival.GetOrDefault(festivalName ?? "");
		if (orDefault == null)
		{
			return false;
		}
		currentFestivalInfo = orDefault;
		InitFestivalState(orDefault);
		if (gameController.CurrentState != this)
		{
			DolocAPI.WaitUntil(() => DolocAPI.IsCurrentStateSupportFestival, Startup);
		}
		return true;
	}

	public bool CheckGateAvailable(string gateId, bool showMessage = true)
	{
		FestivalInfo festivalInfo = currentFestivalInfo;
		if (festivalInfo == null || !festivalInfo.UseGateWhitelist)
		{
			return true;
		}
		if (!currentFestivalInfo.GateWhitelist.Contains(gateId ?? ""))
		{
			if (showMessage)
			{
				DolocAPI.ShowMessageBoxSmallErr(currentFestivalInfo.DisableGateTip);
			}
			return false;
		}
		return true;
	}
}
