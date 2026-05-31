using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class BoardMissionUiState : DolocUiState<BoardMissionPanel>
{
	private BoardMissionManager mgr => DolocAPI.archiveHandle.cityData.boardMissionManager;

	private BoardMission[] _boardMissions => mgr.IssueAllMissions;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void Register()
	{
		base.panel.SetClickCallbacks(OnDataClick);
		base.panel.SlotClick = OnConfirm;
	}

	protected override void Unregister()
	{
		base.panel.RemoveCallbacks();
	}

	private void OnConfirm(int index)
	{
		if (_boardMissions[index] != null)
		{
			base.panel.Select(index);
			if (!DolocAPI.archiveHandle.AcceptBoardMission(_boardMissions[index].Id))
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.BoardMissionAcceptFail);
			}
			else
			{
				base.panel.Refresh(index, new BoardMissionData(_boardMissions[index]));
			}
		}
	}

	private void OnDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			OnConfirm(index);
		}
	}

	protected override void Show()
	{
		base.Show();
		RefreshPanel();
		base.panel.Show();
		base.panel.Select(0);
		mgr.HasNewMission = false;
		base.panel.SetLvInfo(mgr.GetCurrentBattleLv());
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	private void RefreshPanel()
	{
		base.panel.SetCapacity(mgr.Parameter.MissionUpperLimit);
		for (int i = 0; i < _boardMissions.Length; i++)
		{
			base.panel.Render(i, new BoardMissionData(_boardMissions[i]));
		}
	}
}
