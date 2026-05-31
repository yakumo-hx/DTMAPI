using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.UI;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class EmailPanelUiState : PageUiStateBase<EmailPanel, EmailData>
{
	private int currentTypeIndex;

	private Email currentEmail;

	private List<Email> currentEmailList = new List<Email>();

	protected override int totalCapacity => currentEmailList.Count;

	private EmailManager emailMgr => DolocAPI.archiveHandle.farmData.emailManager;

	private TbEmailMenu MenuConfig => DolocConfig.Tables.TbEmailMenu;

	private EmailViewer emailViewer => base.panel.emailViewer;

	private IScrollContentRect _contentRect => base.panel.contentRect;

	protected override EmailData[] DataGetter(int start, int end)
	{
		List<EmailData> list = new List<EmailData>();
		int count = currentEmailList.Count;
		for (int i = start; i < Mathf.Min(count, end); i++)
		{
			list.Add(new EmailData(currentEmailList[i]));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		base.panel.subMenu.Render(MenuConfig.DataMap.Values.Select((EmailMenuInfo x) => x.IconAsset.Asset).ToArray(), MenuConfig.DataMap.Values.Select((EmailMenuInfo x) => x.MenuTitle).ToArray());
	}

	protected override void Register()
	{
		base.Register();
		base.panel.onDataSelect.AddListener(OnEmailSelect);
		base.panel.onDataClick.AddListener(OnEmailClick);
		emailViewer.confirmButton.onClick.AddListener(OnAcceptButtonClick);
		base.panel.subMenu.SetSelectCallbacks(OnMenuIconSelect);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.panel.onDataSelect.RemoveListener(OnEmailSelect);
		base.panel.onDataClick.RemoveListener(OnEmailClick);
		emailViewer.confirmButton.onClick.RemoveListener(OnAcceptButtonClick);
		base.panel.subMenu.RemoveCallbacks();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		base.OnUiUpdate(deltaTime);
		if (userInput.BasePageUpPressed)
		{
			base.panel.subMenu.FireClickLeft();
		}
		else if (userInput.BasePageDownPressed)
		{
			base.panel.subMenu.FireClickRight();
		}
		else if (userInput.BaseIsSplitPressed)
		{
			UpdateCollectState();
		}
		else if (userInput.BaseSortItem)
		{
			UpdateRecycleState();
		}
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.SetEmptyInfo(base.staticTexts.UiEmailEmpty);
		base.panel.subMenu.FireClick(currentTypeIndex, fireSelect: true);
	}

	protected override void Hide()
	{
		base.Hide();
		emailMgr.RefreshMailBoxTip();
		currentTypeIndex = 0;
		currentEmail = null;
		currentEmailList.Clear();
	}

	private void OnEmailSelect(int index)
	{
		currentEmail = null;
		if (index >= 0 && index < totalCapacity)
		{
			currentEmail = currentEmailList[index];
			if (currentEmail.Read())
			{
				base.panel.RefreshView();
			}
			emailViewer.Render(new EmailData(currentEmail));
			DolocAPI.UIRaiseRoll();
		}
	}

	private void OnEmailClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse && emailViewer.confirmButton.gameObject.activeSelf)
		{
			emailViewer.confirmButton.FireClick(fireSelect: false);
		}
	}

	private void OnAcceptButtonClick()
	{
		if (currentEmail.Accept())
		{
			base.panel.RefreshView();
			emailViewer.Render(new EmailData(currentEmail));
		}
		base.panel.Select(base.panel.selectedIndex);
	}

	private void OnMenuIconSelect(int index)
	{
		currentTypeIndex = index;
		RefreshView(refreshData: true);
		DolocAPI.DelayFrame(delegate
		{
			base.panel.Select(0);
		});
		base.panel.SetTitle(MenuConfig.Get((EmailMenuType)currentTypeIndex).Title);
		base.panel.operationTip.SetTextKey((currentTypeIndex != 2) ? new string[2]
		{
			base.staticTexts.UiTipCollection,
			base.staticTexts.UiTipEmailRecycle
		} : new string[1] { base.staticTexts.UiTipEmailDefault });
		DolocAPI.UIRaisePage();
	}

	private void UpdateCollectState()
	{
		if (currentEmail != null && currentTypeIndex != 2)
		{
			currentEmail.Collected = !currentEmail.Collected;
			RefreshView(currentTypeIndex == 1);
			base.panel.Select(base.panel.selectedIndex);
		}
	}

	private void UpdateRecycleState()
	{
		if (currentEmail != null)
		{
			if (currentEmail.HasUnReceivedReward || currentEmail.HasUnReceivedMission)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiEmailRecycleErr);
				return;
			}
			currentEmail.Recycled = !currentEmail.Recycled;
			currentEmail.Collected = false;
			RefreshView(refreshData: true);
			base.panel.Select(base.panel.selectedIndex);
		}
	}

	private void RefreshView(bool refreshData)
	{
		if (refreshData)
		{
			currentEmailList = (EmailMenuType)currentTypeIndex switch
			{
				EmailMenuType.All => emailMgr.emails.Where((Email email) => !email.Recycled).ToList(), 
				EmailMenuType.Collect => emailMgr.emails.Where((Email email) => email.Collected).ToList(), 
				EmailMenuType.RecycleBin => emailMgr.emails.Where((Email email) => email.Recycled).ToList(), 
				_ => new List<Email>(), 
			};
			base.panel.SetTotalCapacity(totalCapacity);
		}
		base.panel.RefreshView();
	}
}
