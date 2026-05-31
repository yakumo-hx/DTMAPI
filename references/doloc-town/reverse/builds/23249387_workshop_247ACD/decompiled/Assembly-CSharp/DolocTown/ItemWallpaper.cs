using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemWallpaper : Item
{
	public ItemFunctionWallpaper func => base.proto.Function as ItemFunctionWallpaper;

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

	public string wallpaperName => func.WallpaperId;

	public ItemWallpaper(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemWallpaper(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		if (CheckWallpaperValid())
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format(DolocConfig.StaticTexts.ItemConfirmUse, title), SetWallpaper);
		}
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		if (CheckWallpaperValid())
		{
			SetWallpaper();
		}
	}

	private bool CheckWallpaperValid()
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null || func.WallpaperId_Ref?.WallpaperDatas_Index == null)
		{
			return false;
		}
		if (!(currentRoom is TemplateRoomInHouse { Building: var building }))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrShouldInhouse);
			return false;
		}
		func.WallpaperId_Ref.WallpaperDatas_Index.TryGetValue(building.BuildingName, out var value);
		if (value == null)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrInvalidWallpaper, title));
			return false;
		}
		if (func.WallpaperId == building.wallpaperId)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrSameWallpaper, title));
			return false;
		}
		return true;
	}

	private void SetWallpaper()
	{
		DolocAPI.agent._Interact(delegate
		{
			CostSelf();
			(DolocAPI.CurrentRoom as TemplateRoomInHouse)?.Building.SetWallpaper(func.WallpaperId_Ref);
		});
	}

	public override string GetExtraInfo1()
	{
		BuildingWallpaperInfo buildingWallpaperInfo = func?.WallpaperId_Ref;
		if (buildingWallpaperInfo == null)
		{
			return base.GetExtraInfo1();
		}
		if (buildingWallpaperInfo.WallpaperDatas.Count == DolocConfig.Tables.TbBuilding.DataList.Count)
		{
			return DolocConfig.StaticTexts.ItemWpAvailableAll;
		}
		string arg = (from x in buildingWallpaperInfo.WallpaperDatas
			select x.BuildingId_Ref?.Title into x
			where !string.IsNullOrEmpty(x)
			select x).HandleJoinString();
		return string.Format(DolocConfig.StaticTexts.ItemWpIsAvailableFor, arg);
	}
}
