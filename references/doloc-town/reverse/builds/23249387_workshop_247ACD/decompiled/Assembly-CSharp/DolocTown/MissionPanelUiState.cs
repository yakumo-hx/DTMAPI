using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class MissionPanelUiState : PageUiStateBase<MissionPanel, MissionData>
{
	private string firstSelectMissionId;

	private List<MissionData> allMissionDatas = new List<MissionData>();

	private IMission currentMission;

	public override bool PermanentState => true;

	protected override bool hideOnPause => true;

	private IScrollContentRect _contentRect => base.panel.contentRect;

	protected override int totalCapacity => allMissionDatas.Count;

	public virtual bool HandleStartUpArgs(string firstSelectMissionId)
	{
		this.firstSelectMissionId = firstSelectMissionId;
		return true;
	}

	protected override MissionData[] DataGetter(int start, int end)
	{
		List<MissionData> list = new List<MissionData>();
		int count = allMissionDatas.Count;
		for (int i = start; i < Mathf.Min(count, end); i++)
		{
			list.Add(allMissionDatas[i]);
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		allMissionDatas.Clear();
		IMission[] missions = DolocAPI.archiveHandle.GetMissions();
		foreach (IMission mission in missions)
		{
			allMissionDatas.Add(new MissionData(mission, isChainComplete: false));
		}
		string[] completedOrEndedMissionChains = DolocAPI.archiveHandle.farmData.missionChainManager.CompletedOrEndedMissionChains;
		foreach (string key in completedOrEndedMissionChains)
		{
			MissionInfo orDefault = DolocConfig.Tables.TbMission.GetOrDefault(key);
			if (orDefault != null)
			{
				allMissionDatas.Add(new MissionData(orDefault));
			}
		}
		allMissionDatas = (from x in allMissionDatas
			where x.notEmpty
			orderby (!x.isComplete) ? 1 : 0 descending, x.typeOrder descending, x.orderInType descending
			select x).ToList();
		DolocAPI.Broadcast(OperationEventType.OPEN_MISSION_PANEL);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		base.OnUiUpdate(deltaTime);
		if (userInput.BaseSubmitItem)
		{
			base.panel.missionViewer.ClickMapButton();
		}
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
		if (userInput.GlobalToggleMissionPanel && userInput.BaseNotFixedOperationExcludeHorizontalNavigation)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		DolocAPI.UIRaisePopUp();
		int num = ((totalCapacity <= 0) ? (-1) : 0);
		if (num >= 0 && !firstSelectMissionId.IsNullOrEmpty())
		{
			for (int i = 0; i < allMissionDatas.Count; i++)
			{
				if (allMissionDatas[i].missionId == firstSelectMissionId)
				{
					num = i;
					break;
				}
			}
		}
		base.panel.Show();
		base.panel.RebuildLayout();
		base.panel.SetTitle(base.staticTexts.MissionPanelTitle);
		base.panel.SetEmptyInfo(base.staticTexts.MissionPanelEmpty);
		if (num >= 0)
		{
			base.panel.Select(num);
		}
	}

	protected override void Hide()
	{
		base.Hide();
		firstSelectMissionId = string.Empty;
	}

	public override void OnResume()
	{
		base.OnResume();
		base.panel.Select(base.panel.selectedIndex);
	}
}
