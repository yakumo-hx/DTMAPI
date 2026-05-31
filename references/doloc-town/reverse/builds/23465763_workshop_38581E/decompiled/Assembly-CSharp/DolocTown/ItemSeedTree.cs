using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemSeedTree : Item
{
	private ItemFunctionSeedTree func => (ItemFunctionSeedTree)base.proto.Function;

	public bool CanPlantOnGround => ResourceProto != null;

	public TreeSeedInfo TreeSeedProto => func.TreeSeedId_Ref;

	public ResourceInfo ResourceProto => func.TreeSeedId_Ref?.ResourceId_Ref;

	private int width => ResourceProto?.Width ?? 1;

	public ItemSeedTree(ItemInfo item, int count)
		: base(item, count)
	{
	}

	[JsonConstructor]
	protected ItemSeedTree(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		if (!_TryPlantInBasin(base.cellTip.CellAnchor))
		{
			_PlantTreeOnGround(ResourceProto, base.cellTip.CellAnchor);
		}
	}

	private bool _TryPlantInBasin(Vector2Int cellpos)
	{
		if (TryGetSelectedEquipment(out PlantBasinTree equipment))
		{
			return equipment.TryPlantCrop();
		}
		return false;
	}

	private void _PlantTreeOnGround(ResourceInfo proto, Vector2Int cellpos)
	{
		if (!CanPlantOnGround)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotPlantOnGround);
		}
		else
		{
			if (proto.ResourceType != 0)
			{
				return;
			}
			Room currentRoom = DolocAPI.archiveHandle.currentRoom;
			IDungeonResourceHost host = currentRoom;
			if (host == null)
			{
				return;
			}
			if (!host.CanPlantTree(cellpos, proto.Width))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotPlantTree);
				return;
			}
			DolocAPI.agent._Interact(delegate
			{
				if (CostSelf(out var _) && host._PlantTree(proto, cellpos))
				{
					DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_PLANT);
				}
			});
		}
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		if (base.allowBuildResource)
		{
			ShowCellTip(new Vector2Int(0, 0), new Vector2Int(width, 1), flipWhenFaceLeft: true);
			RefreshCellTip();
		}
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		bool flag = false;
		if (CanPlantOnGround)
		{
			IDungeonResourceHost currentRoom = DolocAPI.archiveHandle.currentRoom;
			if (currentRoom != null)
			{
				flag = currentRoom.CanPlantTree(base.cellTip.CellAnchor, width);
			}
		}
		if (!flag)
		{
			IEquipmentHost currentRoom2 = DolocAPI.archiveHandle.currentRoom;
			if (currentRoom2 != null)
			{
				flag = currentRoom2.GetEquipment(base.cellTip.CellAnchor) is PlantBasinTree plantBasinTree && plantBasinTree.Crop == null;
			}
		}
		base.cellTip.CellTipValid = flag;
	}
}
