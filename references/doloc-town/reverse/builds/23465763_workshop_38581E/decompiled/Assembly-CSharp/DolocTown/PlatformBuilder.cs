using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Platform;
using UnityEngine;

namespace DolocTown;

public class PlatformBuilder : BuilderState<ItemPlatform, Platform>
{
	private bool isLocked;

	private bool canBuildNow;

	private PlatformInfo platformProto;

	private PlatformBuilderRenderer indicatorRenderer;

	private PlatformBuilderHelper builderHelper;

	private List<Equipment> supportEquipments = new List<Equipment>();

	public override bool Construct => base.CurrentRoom.RoomConstructInfo.AllowBuildPlatform;

	private IPlatformHost host => base.CurrentRoom;

	protected override ItemPlatform SelectedItem
	{
		get
		{
			return CurrentItem;
		}
		set
		{
			CurrentItem = value;
			if (value == null)
			{
				platformProto = null;
				builderHelper = null;
				RecycleIndicator();
			}
			else
			{
				platformProto = CurrentItem.PlatformProto;
				CreateIndicator();
				builderHelper = new PlatformBuilderHelper(base.CurrentRoom, indicatorRenderer, platformProto);
			}
		}
	}

	protected override Platform SelectedContent { get; set; }

	public override void RunBuilder(Item item)
	{
		base.RunBuilder(item);
		SelectedItem = null;
		if (ItemFilter(item))
		{
			SelectedItem = (ItemPlatform)item;
			RestoreSupportEquipmentsMaterial();
			OnPosMoved(CurrentCellPosition);
		}
	}

	public override void ExitBuilder()
	{
		base.ExitBuilder();
		Revocation();
		RestoreSupportEquipmentsMaterial();
		SelectedItem = null;
	}

	public override bool OnUpdate(float deltaTime)
	{
		Vector2Int lastCellPosition = LastCellPosition;
		base.positionUpdateFunc(deltaTime);
		if (ContentCheckedRenderer != null)
		{
			ShowTerrainContentInfo(CurrentCellPosition);
		}
		if (base.userInput.BuilderSelected)
		{
			if (SelectedItem == null)
			{
				return false;
			}
			if (isLocked)
			{
				ConfirmBuild();
				return true;
			}
			if (canBuildNow)
			{
				isLocked = true;
				builderHelper.LockedPosition = CurrentCellPosition;
				OnPosMoved(CurrentCellPosition);
			}
			else
			{
				ShowErrorMessage(base.StaticTexts.FarmbuilderErrInvalidPosition);
			}
			return true;
		}
		if (base.userInput.BuilderRevocationPressed)
		{
			if (isLocked)
			{
				Revocation();
				return true;
			}
			if (SelectedItem != null)
			{
				SelectedItem = null;
				return true;
			}
		}
		if (base.userInput.BuilderDismantlePressed)
		{
			Dismantle();
			return true;
		}
		if (base.userInput.BuilderDismantleInProgress && lastCellPosition != CurrentCellPosition)
		{
			Dismantle(showMessage: false);
			return true;
		}
		return false;
	}

	public override void OnUpdateMoveCamera(Vector2 delta)
	{
		if (indicatorRenderer != null)
		{
			indicatorRenderer.BorderPosition = BuilderUtils.GetScreenLatticePosition(CurrentCellPosition, base.CurrentRoom.RoomPosition);
			if (isLocked)
			{
				PlatformGeometry platformGeometry = builderHelper.platformGeometry;
				int y = platformGeometry.Height - Mathf.Max(platformGeometry.columnHeight.x, platformGeometry.columnHeight.y);
				indicatorRenderer.AreaPosition = BuilderUtils.GetScreenLatticePosition(new Vector2Int(platformGeometry.Left, y), base.CurrentRoom.RoomPosition);
				indicatorRenderer.ItemCostTipPosition = BuilderUtils.GetScreenLatticePosition(new Vector2Int(platformGeometry.Left, platformGeometry.Height + 1), base.CurrentRoom.RoomPosition);
			}
		}
	}

	protected override void OnPosMoved(Vector2Int pos)
	{
		if (platformProto == null)
		{
			RestoreSupportEquipmentsMaterial();
			if (CheckedContent == null)
			{
				return;
			}
			supportEquipments.AddRange(CheckedContent.OccupyEquipments);
			{
				foreach (Equipment supportEquipment in supportEquipments)
				{
					supportEquipment.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_HOLOGRAM;
				}
				return;
			}
		}
		if (isLocked)
		{
			builderHelper.RaycastGround(pos);
			canBuildNow = builderHelper.canBuildNow;
			indicatorRenderer.BorderPosition = BuilderUtils.GetScreenLatticePosition(pos, base.CurrentRoom.RoomPosition);
			indicatorRenderer.BorderValid = canBuildNow;
		}
		else
		{
			canBuildNow = base.CurrentRoom.DM_terrain.IsStructureConstructable(pos);
			indicatorRenderer.BorderPosition = BuilderUtils.GetScreenLatticePosition(pos, base.CurrentRoom.RoomPosition);
			indicatorRenderer.BorderValid = canBuildNow;
			indicatorRenderer.SetDraft(pos, platformProto.ColumnTile);
		}
	}

	protected override void ShowTerrainContentInfo(Vector2Int pos)
	{
		bool flag = SelectedItem == null;
		ContentCheckedRenderer.SetVisible(flag);
		if (CheckedContent == null)
		{
			ContentCheckedRenderer.OnContentChoose(string.Empty, BuilderUtils.GetScreenLatticePosition(pos, base.CurrentRoom.RoomPosition), Vector2.one);
			return;
		}
		int y = CheckedContent.geometry.Height - Mathf.Max(CheckedContent.geometry.columnHeight.x, CheckedContent.geometry.columnHeight.y);
		ContentCheckedRenderer.OnContentChoose(flag ? CheckedContent.proto.Title : string.Empty, BuilderUtils.GetScreenLatticePosition(new Vector2Int(CheckedContent.geometry.Left, y), base.CurrentRoom.RoomPosition), CheckedContent.geometry.CoveredSize);
	}

	protected override void ConfirmBuild(bool showMessage = true)
	{
		if (!builderHelper.IsPlatformConstructable())
		{
			return;
		}
		ResetBuilder();
		PlatformGeometry platformGeometry = builderHelper.platformGeometry;
		if (builderHelper.isReplace)
		{
			host.ReplacePlatform(platformGeometry, platformProto, out var oldPlatformProtoName);
			foreach (Vector2Int allPosition in platformGeometry.AllPositions)
			{
				Vector2 pos = base.CurrentRoom.Geometry.CalcWorldPositionCenter(allPosition);
				PlaceItemInBagOrCreateDropItem(oldPlatformProtoName, 1, pos, sendMessage: false);
			}
		}
		else
		{
			host.CreatePlatform(platformProto, platformGeometry, builderHelper.platformCutInfos, out var returnCost);
			foreach (var item in returnCost)
			{
				PlaceItemInBagOrCreateDropItem(item.Item1.itemName, item.Item1.itemCount, item.Item2, sendMessage: false);
			}
		}
		GlobalBuilderState.ValidOperation = true;
		DolocAPI.cameraController.ShakeScreen(0.2f, DolocAPI.GlobalParameter.PlatformShakeIntensity);
		DolocAPI.BroadcastString(GameEventType.BUILD_PLATFORM, platformProto.Id);
		DolocAPI.CostItemAt(DolocAPI.SelectedItemIndex, platformGeometry.TileCount);
		if (base.backpack.Read(DolocAPI.SelectedItemIndex) == null)
		{
			SelectedItem = null;
		}
		OnPosMoved(CurrentCellPosition);
	}

	protected override void Revocation()
	{
		ResetBuilder();
		OnPosMoved(CurrentCellPosition);
	}

	protected override void Dismantle(bool showMessage = true)
	{
		if (SelectedItem != null || CheckedContent == null)
		{
			return;
		}
		if (!host.CanRemovePlatform(CheckedContent))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrPlatformOccupied, showMessage);
			return;
		}
		List<Equipment> occupyEquipments = CheckedContent.OccupyEquipments;
		if (occupyEquipments.Any((Equipment equipment) => equipment.IsOccupy))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrPlatformRemove, showMessage);
			return;
		}
		PlatformGeometry geometry = CheckedContent.geometry;
		host.RemovePlatform(CheckedContent);
		Vector2 vector = base.CurrentRoom.Geometry.CalcWorldPosition(geometry.CenterTop);
		PlaceItemInBagOrCreateDropItem(CheckedContent.proto.Id, geometry.TileCount, vector, sendMessage: false);
		foreach (Equipment item in occupyEquipments)
		{
			((IEquipmentHost)host).RemoveEquipment(item, base.PutInBackpack);
		}
		supportEquipments.Clear();
		GlobalBuilderState.ValidOperation = true;
		DolocAPI.Broadcast(OperationEventType.FELL_PLATFORM);
		if (DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
		{
			vector = base.CurrentRoom.Geometry.CalcWorldPosition(CurrentCellPosition);
		}
		DolocAPI.effectProvider.RaiseInstPS(vector, InstantParticleEffectsType.SAWDUST);
		CheckedContent = null;
	}

	protected override void CreateIndicator()
	{
		indicatorRenderer = new PlatformBuilderRenderer();
		indicatorRenderer.DraftPosition = new Vector3(base.CurrentRoom.RoomPosition.x, base.CurrentRoom.RoomPosition.y, -10f);
	}

	protected override void RecycleIndicator()
	{
		indicatorRenderer?.Dispose();
		indicatorRenderer = null;
	}

	private void RestoreSupportEquipmentsMaterial()
	{
		foreach (Equipment supportEquipment in supportEquipments)
		{
			supportEquipment.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
		supportEquipments.Clear();
	}

	private void ResetBuilder()
	{
		isLocked = false;
		canBuildNow = false;
		if (indicatorRenderer != null)
		{
			indicatorRenderer.ClearDraft();
			indicatorRenderer.DraftValid = true;
		}
	}

	private void PlaceItemInBagOrCreateDropItem(string itemName, int count, Vector2 pos, bool sendMessage)
	{
		for (int i = 0; i < count; i++)
		{
			Item item = DolocAPI.GenerateItem(itemName);
			DolocAPI.RaiseSpriteFadeUp(pos, item.uiSprite);
			DolocAPI.RaiseItemObtainTip(item.name, item.uiSprite, item.title);
			item = DolocAPI.PlaceItem(item, DolocAPI.userSettings.autoUseBox, useFade: true);
			if (item != null)
			{
				DolocAPI.GenerateDropItem(base.CurrentRoom, item, pos, sendMessage);
			}
		}
	}
}
