using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Plant;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class SeedUnLockUiState : PageUiStateBase<SeedUnLockPanel, SeedUnLockData>
{
	private List<SeedUnlockInfo> seedList;

	protected override int totalCapacity => seedList.Count;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	public static List<string> latestUnlockSeed { get; private set; } = new List<string>();


	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		seedList = DolocConfig.Tables.TbSeedUnlock.DataList.Where((SeedUnlockInfo x) => DolocAPI.archiveHandle.IsSeedNodeAvailableToUnlock(x.Id) || DolocAPI.archiveHandle.IsSeedNodeUnlocked(x.Id)).ToList();
	}

	protected override void Register()
	{
		base.Register();
		base.panel.onDataSelect.AddListener(OnDataSelect);
		base.panel.onDataClick.AddListener(OnDataClick);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.panel.onDataSelect.RemoveListener(OnDataSelect);
		base.panel.onDataClick.RemoveListener(OnDataClick);
	}

	protected override SeedUnLockData[] DataGetter(int start, int end)
	{
		List<SeedUnLockData> list = new List<SeedUnLockData>();
		int count = seedList.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			SeedUnlockInfo proto = seedList[i];
			list.Add(new SeedUnLockData(proto));
		}
		return list.ToArray();
	}

	private void OnDataSelect(int index)
	{
		DolocAPI.UIRaiseRoll();
	}

	private void OnDataClick(int index)
	{
		UnLockSeed(index);
	}

	private void UnLockSeed(int index)
	{
		if (index < 0 || index >= seedList.Count)
		{
			return;
		}
		SeedUnlockInfo node = seedList[index];
		if (DolocAPI.archiveHandle.IsSeedNodeUnlocked(node.Id))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.SeedNodeAlreadyUnlock);
			return;
		}
		if (!DolocAPI.gameManager.gameInitConfig.skipMoneyVerifyInShop && !DolocAPI.CanAfford(node.Costs, checkBox: true))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiOperationErrLackOfAsset);
			return;
		}
		DolocAPI.ShowQuestionBox(base.staticTexts.UiUnlockSeed, delegate
		{
			DolocAPI.ShowMessageBoxLarge(LocSprites.UI_INFOICON_STAR, string.Format(base.staticTexts.SeedNodeSucceedUnlock, DolocAPI.GetItemTitle(node.Id)));
			DolocAPI.CostItemNoCheck(node.Costs, checkBox: true);
			DolocAPI.archiveHandle.UnlockSeedNode(node.Id);
			latestUnlockSeed.Add(node.Id);
			base.panel.RefreshView();
		});
	}

	protected override void Show()
	{
		base.Show();
		latestUnlockSeed.Clear();
		base.panel.SetTitle(base.staticTexts.SeedPanelTitle);
		base.panel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
		base.panel.operationTip.SetTextKey(base.staticTexts.UiTipUnlockSeed);
	}
}
