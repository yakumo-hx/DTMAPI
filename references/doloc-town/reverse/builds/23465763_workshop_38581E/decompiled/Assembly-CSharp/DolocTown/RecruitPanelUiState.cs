using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Settings;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class RecruitPanelUiState : DolocUiState<TreatyPortPanel>
{
	private string[] factionList;

	private bool inside;

	private TbTreatyPortFaction Config => DolocConfig.Tables.TbTreatyPortFaction;

	private TreatyPortFactionManager factionManager => DolocAPI.archiveHandle.cityData.treatyPortFactionManager;

	private GlobalParameterInfo GlobalParameter => DolocAPI.GlobalParameter;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override void Register()
	{
		base.panel.factionListViewer.SetClickCallbacks(OnRecruitSlotClick);
		base.panel.factionListViewer.BindFactionSlotClickEvent(RecruitInvite);
	}

	protected override void Unregister()
	{
		base.panel.factionListViewer.RemoveCallbacks();
		base.panel.factionListViewer.BindFactionSlotClickEvent(null);
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			if (inside)
			{
				inside = false;
				int selectedIndex = base.panel.factionListViewer.selectedIndex;
				base.panel.factionListViewer.Select(selectedIndex);
				base.panel.factionListViewer.GetSlot(selectedIndex).ResetPanel();
				base.panel.operationTip.SetTextKey(base.staticTexts.UiTipBroadcast);
			}
			else
			{
				gameController.PopState();
			}
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		RefreshPanel();
		base.panel.factionListViewer.Select(0);
		base.panel.operationTip.SetTextKey(base.staticTexts.UiTipBroadcast);
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
	}

	private void RefreshPanel()
	{
		base.panel.factionStats.text = DolocUtils.Format(base.staticTexts.TreatyPortRecruitStats, $"{factionManager.GetJoinFactionCount()}/{factionManager.GetAllFactionCount()}");
		base.panel.dolocFactionViewer.Render(new TreatyPortFactionData(FactionType.Doloc));
		IEnumerable<TreatyPortFactionData> source = from x in factionManager.GetJoinFactions()
			select new TreatyPortFactionData(x);
		base.panel.factionListViewer.Render(source.ToArray());
	}

	private void OnRecruitSlotClick(int index)
	{
		CandidateFactionViewer slot = base.panel.factionListViewer.GetSlot(index);
		factionList = factionManager.GetInviteFactions();
		slot.ShowFactionList(new RecruitData(factionList));
		slot.SelectFactionSlot(0);
		inside = true;
		base.panel.operationTip.SetTextKey(new string[2]
		{
			base.staticTexts.UiTipQuit,
			base.staticTexts.UiTipBroadcast
		});
	}

	private void RecruitInvite(int index)
	{
		if (index < 0 || index >= factionList.Length)
		{
			return;
		}
		TreatyPortFactionInfo byId = Config.GetById(factionList[index]);
		if (!byId.UnlockInDemo)
		{
			return;
		}
		DolocAPI.QueryNpc(byId.Contact, out var npc);
		if (!npc.hasKnownName)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.TreatyPortNotContactedTip);
			return;
		}
		int hour = DolocAPI.archiveHandle.timeData.dateNow.Hour;
		if (GlobalParameter.FactionWorkingHours.x > hour || GlobalParameter.FactionWorkingHours.y <= hour)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.TreatyPortOffDutyHoursTip);
		}
		else if (!string.IsNullOrEmpty(npc.sceneName))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.staticTexts.TreatyPortInterviewTip, npc.GetCurrentTitle(ignoreUnknown: false)));
		}
		else if (!byId.Dialogue.IsNullOrEmpty())
		{
			DolocAPI.StartDialogueNode(byId.Dialogue);
			gameController.PopState();
		}
	}
}
