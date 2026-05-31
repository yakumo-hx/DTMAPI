using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemBuildingExterior : Item
{
	public ItemFunctionBuildingExterior func => base.proto.Function as ItemFunctionBuildingExterior;

	public override bool invalid
	{
		get
		{
			if (!base.invalid)
			{
				return func == null;
			}
			return true;
		}
	}

	public string exteriorName => func.ExteriorId;

	public ItemBuildingExterior(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemBuildingExterior(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		if (CheckExteriorValid())
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format(DolocConfig.StaticTexts.ItemConfirmUse, title), SetExterior);
		}
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		if (CheckExteriorValid())
		{
			SetExterior();
		}
	}

	private bool CheckExteriorValid(bool useLog = true)
	{
		if (DolocAPI.CurrentRoom == null || func.ExteriorId_Ref?.ExteriorDatas_Index == null)
		{
			return false;
		}
		if (DolocAPI.CurrentRoom.IsInHouse)
		{
			if (useLog)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrShouldOutside);
			}
			return false;
		}
		Building selectedBuilding = base.SelectedBuilding;
		if (selectedBuilding == null)
		{
			if (useLog)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrNoBuildingHere);
			}
			return false;
		}
		func.ExteriorId_Ref.ExteriorDatas_Index.TryGetValue(selectedBuilding.BuildingName, out var value);
		if (value == null)
		{
			if (useLog)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrInvalidWallpaper, title, selectedBuilding.Title));
			}
			return false;
		}
		if (func.ExteriorId == selectedBuilding.exteriorId)
		{
			if (useLog)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrSameWallpaper, title));
			}
			return false;
		}
		return true;
	}

	private void SetExterior()
	{
		DolocAPI.agent._Interact(delegate
		{
			CostSelf();
			base.SelectedBuilding?.SetExterior(func.ExteriorId_Ref);
		});
	}

	public override string GetExtraInfo1()
	{
		BuildingExteriorInfo buildingExteriorInfo = func?.ExteriorId_Ref;
		if (buildingExteriorInfo == null)
		{
			return base.GetExtraInfo1();
		}
		if (buildingExteriorInfo.ExteriorDatas.Count == DolocConfig.Tables.TbBuilding.DataList.Count)
		{
			return DolocConfig.StaticTexts.ItemWpAvailableAll;
		}
		string arg = (from x in buildingExteriorInfo.ExteriorDatas
			select x.BuildingId_Ref?.Title into x
			where !string.IsNullOrEmpty(x)
			select x).HandleJoinString();
		return string.Format(DolocConfig.StaticTexts.ItemWpIsAvailableFor, arg);
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		ShowCellTip(Vector2Int.zero, Vector2Int.one, flipWhenFaceLeft: true);
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = CheckExteriorValid(useLog: false);
	}
}
