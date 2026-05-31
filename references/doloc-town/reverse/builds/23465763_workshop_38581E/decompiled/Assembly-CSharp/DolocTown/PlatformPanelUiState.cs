using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Platform;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class PlatformPanelUiState : PageUiStateBase<PlatformPanel, PlatformData>
{
	private PlatformInfo currentPlatformProto;

	private int currentIndex;

	private int prevIndex;

	private List<PlatformInfo> unlockedPlatforms
	{
		get
		{
			List<PlatformInfo> list = new List<PlatformInfo>();
			foreach (PlatformInfo data in DolocConfig.Tables.TbPlatform.DataList)
			{
				if (DolocAPI.archiveHandle.farmData.recipeManager.CheckRecipeUnlocked(data.Id))
				{
					list.Add(data);
				}
			}
			return list;
		}
	}

	protected override int totalCapacity => unlockedPlatforms.Count;

	protected override PlatformData[] DataGetter(int start, int end)
	{
		List<PlatformData> list = new List<PlatformData>();
		int count = unlockedPlatforms.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			PlatformInfo proto = unlockedPlatforms[i];
			list.Add(new PlatformData(proto, DolocAPI.archiveHandle.InventorySystem.inventory));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		currentIndex = 0;
		currentPlatformProto = null;
	}

	protected override void Register()
	{
		base.Register();
		base.panel.onDataSelect.AddListener(OnDataSelect);
		base.panel.onDataClick.AddListener(OnDataClick);
		base.panel.BtnCraft.onClick.AddListener(OnStartButtonClick);
	}

	protected override void Unregister()
	{
		currentIndex = 0;
		base.panel.onDataSelect.RemoveListener(OnDataSelect);
		base.panel.onDataClick.RemoveListener(OnDataClick);
		base.panel.BtnCraft.onClick.RemoveListener(OnStartButtonClick);
		base.Unregister();
	}

	private void OnDataSelect(int index)
	{
		if (index >= 0 && index < unlockedPlatforms.Count)
		{
			currentPlatformProto = unlockedPlatforms[index];
			currentIndex = index;
			DolocAPI.UIRaiseRoll();
		}
	}

	private void OnDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			base.panel.BtnCraft.FireClick();
		}
	}

	private void OnStartButtonClick(int _)
	{
		if (base.panel.BtnCraft.grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrMaterialNotEnough);
			base.panel.Select(currentIndex);
			if (DolocAPI.gameManager.shouldBuilderCostAssets)
			{
				return;
			}
		}
		currentPlatformProto = null;
	}

	protected override void Show()
	{
		base.Show();
		if (totalCapacity > 0)
		{
			currentPlatformProto = unlockedPlatforms[0];
		}
		base.panel.SetTitle(base.staticTexts.PlatformPanelTitle);
		base.panel.SetEmptyInfo(base.staticTexts.UiTipEmptyList);
	}

	public override void OnPause()
	{
		base.OnPause();
		prevIndex = currentIndex;
	}

	public override void OnResume()
	{
		base.OnResume();
		base.panel.Select(prevIndex);
	}
}
