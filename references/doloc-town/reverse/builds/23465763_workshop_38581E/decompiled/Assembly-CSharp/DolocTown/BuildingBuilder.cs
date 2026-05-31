using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Building;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class BuildingBuilder : BuilderState<ItemBuilding, Building>
{
	private Vector3 posWS;

	private bool areaEmpty;

	private bool heightValid;

	private float coveredRatio;

	private bool groundCheckValid;

	private bool isTemporaryItem;

	private Building currentBuilding;

	private BuildingInfo buildingProto;

	private BuildingBuilderRenderer indicatorRenderer;

	private BuildingSupport buildingSupport;

	private List<Equipment> ceilingEquipments = new List<Equipment>();

	private List<Equipment> doorEquipments = new List<Equipment>();

	private readonly GameEntitySlot<SingleSpriteRender> buildingSlot = new GameEntitySlot<SingleSpriteRender>();

	private readonly TempBuildingViewer _buildingViewer;

	private float holdStartTime = -1f;

	private const float HoldThreshold = 0.5f;

	public override bool Construct => base.CurrentRoom.RoomConstructInfo.AllowBuildBuilding;

	private IBuildingHost host => base.CurrentRoom;

	private bool coveredRatioValid => coveredRatio >= DolocAPI.GlobalParameter.MinFloorCoveredRatio;

	public LinearInventory TempBuildingList { get; private set; }

	protected override ItemBuilding SelectedItem
	{
		get
		{
			return CurrentItem;
		}
		set
		{
			CurrentItem = value;
			RecycleIndicator();
			RecycleOccupiedGrid();
			if (value == null)
			{
				buildingProto = null;
				currentBuilding = null;
				UpdateInvalidEquipmentRenderer();
			}
			else
			{
				buildingProto = CurrentItem.BuildingProto;
				currentBuilding = CurrentItem.buildingEntity;
				CreateIndicator();
				DrawOccupied();
			}
		}
	}

	protected override Building SelectedContent
	{
		get
		{
			return CurrentContent;
		}
		set
		{
			CurrentContent = value;
			if (value == null)
			{
				buildingProto = null;
				currentBuilding = null;
				RecycleIndicator();
				RecycleOccupiedGrid();
				UpdateInvalidEquipmentRenderer();
			}
			else
			{
				buildingProto = CurrentContent.proto;
				currentBuilding = CurrentContent;
				CreateIndicator();
				DrawOccupied();
			}
		}
	}

	public BuildingBuilder(TempBuildingViewer buildingViewer)
	{
		_buildingViewer = buildingViewer;
		TempBuildingList = new LinearInventory(DolocAPI.GlobalParameter.TemporaryBuildingCount);
		_buildingViewer.BindBuilderInventory(TempBuildingList, delegate(int i)
		{
			isTemporaryItem = true;
			base.RunBuilder(null);
			SelectedItem = (ItemBuilding)TempBuildingList.Read(i);
			OnPosMoved(CurrentCellPosition);
		});
	}

	public override void RunBuilder(Item item)
	{
		isTemporaryItem = false;
		base.RunBuilder(item);
		SelectedItem = null;
		if (ItemFilter(item))
		{
			SelectedItem = (ItemBuilding)item;
			OnPosMoved(CurrentCellPosition);
		}
	}

	public override void ExitBuilder()
	{
		base.ExitBuilder();
		if (SelectedItem != null)
		{
			SelectedItem = null;
		}
		if (SelectedContent != null)
		{
			Revocation();
		}
	}

	public override bool OnUpdate(float deltaTime)
	{
		base.positionUpdateFunc(deltaTime);
		if (ContentCheckedRenderer != null)
		{
			ShowTerrainContentInfo(CurrentCellPosition);
		}
		if (base.userInput.BuilderSelected)
		{
			if (SelectedItem != null)
			{
				ConfirmBuild();
				return true;
			}
			if (CheckedContent != null && SelectedContent == null)
			{
				WaitMove();
				return true;
			}
			if (SelectedContent != null)
			{
				ConfirmBuild();
				return true;
			}
		}
		return HandleInput();
	}

	private bool HandleInput()
	{
		if (base.userInput.BuilderRevocationDisplayName == base.userInput.BuilderDismantleDisplayName)
		{
			return HandleSingleKeyMode();
		}
		return HandleDualKeyMode();
	}

	private bool HandleSingleKeyMode()
	{
		if (base.userInput.BuilderRevocationPressed)
		{
			holdStartTime = Time.time;
		}
		if (base.userInput.BuilderDismantleInProgress && Time.time - holdStartTime >= 0.5f && SelectedItem == null && SelectedContent == null && CheckedContent != null)
		{
			Dismantle();
			return true;
		}
		if (base.userInput.BuilderDismantleReleased)
		{
			float num = Time.time - holdStartTime;
			holdStartTime = -1f;
			if (num < 0.5f)
			{
				if (SelectedItem != null)
				{
					SelectedItem = null;
					return true;
				}
				if (SelectedContent != null)
				{
					Revocation();
					return true;
				}
				if (CheckedContent != null)
				{
					TemporaryStorage();
					return true;
				}
			}
		}
		return false;
	}

	private bool HandleDualKeyMode()
	{
		if (base.userInput.BuilderRevocationPressed)
		{
			if (SelectedItem != null)
			{
				SelectedItem = null;
				return true;
			}
			if (SelectedContent != null)
			{
				Revocation();
				return true;
			}
		}
		if (base.userInput.BuilderDismantlePressed)
		{
			holdStartTime = Time.time;
		}
		if (base.userInput.BuilderDismantleInProgress && Time.time - holdStartTime >= 0.5f && CheckedContent != null)
		{
			Dismantle();
			return true;
		}
		if (base.userInput.BuilderDismantleReleased)
		{
			float num = Time.time - holdStartTime;
			holdStartTime = -1f;
			if (num < 0.5f && CheckedContent != null)
			{
				TemporaryStorage();
				return true;
			}
		}
		return false;
	}

	public override string[] GetOperateTip(DolocInputDeviceType type)
	{
		if (type == DolocInputDeviceType.KeyboardMouse)
		{
			return new string[10]
			{
				base.StaticTexts.BuilderActionMoveCamera,
				base.StaticTexts.BuilderPanelSwitchTerrainLayer,
				base.StaticTexts.BuilderActionSelectedContent,
				base.StaticTexts.BuilderActionUndo,
				base.StaticTexts.BuilderActionBuildingStorage,
				base.StaticTexts.BuilderActionBuildingDismantle,
				base.StaticTexts.BuilderActionSelectedItem,
				base.StaticTexts.BuilderActionRollingBackpack,
				base.StaticTexts.BuilderActionToggleBackpack,
				base.StaticTexts.BuilderPanelExit
			};
		}
		return new string[10]
		{
			base.StaticTexts.BuilderActionMoveCamera,
			base.StaticTexts.BuilderPanelSwitchTerrainLayer,
			base.StaticTexts.BuilderActionMove,
			base.StaticTexts.BuilderActionSelectedContent,
			base.StaticTexts.BuilderPanelUndoGamepad,
			base.StaticTexts.BuilderActionBuildingStorage,
			base.StaticTexts.BuilderActionBuildingDismantle,
			base.StaticTexts.BuilderActionSelectedItem,
			base.StaticTexts.BuilderActionRollingBackpack,
			base.StaticTexts.BuilderActionToggleBackpackGamepad
		};
	}

	protected override void OnPosMoved(Vector2Int pos)
	{
		if (buildingProto == null)
		{
			return;
		}
		areaEmpty = true;
		areaEmpty &= base.CurrentRoom.DM_terrain.AllEmpty(buildingProto.GetCoverPositions(pos), TerrainLayerName.Ground);
		areaEmpty &= base.CurrentRoom.DM_terrain.AllEmpty(buildingProto.GetPositionsOfType(pos, BuildingTileType.NoOverlay), TerrainLayerName.Ground | TerrainLayerName.BuildingNoOverlay);
		groundCheckValid = buildingProto.GroundDepth <= 0 || (base.CurrentRoom.DM_terrain.AllFilled(buildingProto.GetGroundPositions(pos, buildingProto.GroundDepth), TerrainLayerName.Ground) && base.CurrentRoom.DM_terrain.AllEmpty(buildingProto.GetGroundPositions(pos, buildingProto.GroundDepth), TerrainLayerName.ExtraObstacles));
		foreach (Equipment doorEquipment in doorEquipments)
		{
			doorEquipment.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
		doorEquipments.Clear();
		doorEquipments.AddRange(base.CurrentRoom.DM_terrain.GetContentsFromPositions<Equipment>(buildingProto.GetPositionsOfType(pos, BuildingTileType.Door)));
		foreach (Equipment doorEquipment2 in doorEquipments)
		{
			doorEquipment2.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_HOLOGRAM;
		}
		posWS = (pos + new Vector2((float)buildingProto.CoverSize.x / 2f, 0f)) * 1.5f + base.CurrentRoom.RoomPosition;
		posWS.y -= (float)pos.x * 1E-06f;
		coveredRatio = 0f;
		heightValid = true;
		if (areaEmpty)
		{
			buildingSupport = host.GenerateBuildingSupport(buildingProto, pos);
			if (buildingSupport != null)
			{
				float num = buildingProto.GetPositionsOfType(pos, BuildingTileType.Floor, BuildingTileType.Side).Length;
				coveredRatio = (float)buildingSupport.ContactPositions.Length / num;
				heightValid = buildingSupport.ColumnHeights.Max() <= DolocAPI.GlobalParameter.MaxSupportHeight;
			}
		}
		bool flag = doorEquipments.Any((Equipment equipment) => equipment.IsOccupy);
		indicatorRenderer.ShowBuildingDraft(BuilderUtils.GetRealGridPos(pos, base.CurrentRoom.RoomPosition), posWS, areaEmpty && !flag && coveredRatioValid && groundCheckValid);
		if (areaEmpty && groundCheckValid)
		{
			float num2 = coveredRatio;
			if (num2 > 0f && num2 < 1f)
			{
				indicatorRenderer.ShowSupportDraft(buildingSupport, heightValid);
				return;
			}
		}
		indicatorRenderer.HideSupportDraft();
	}

	protected override void ShowTerrainContentInfo(Vector2Int pos)
	{
		bool flag = buildingProto == null;
		ContentCheckedRenderer.SetVisible(flag);
		if (CheckedContent == null)
		{
			ContentCheckedRenderer.OnContentChoose(string.Empty, BuilderUtils.GetScreenLatticePosition(pos, base.CurrentRoom.RoomPosition), Vector2.one);
		}
		else
		{
			ContentCheckedRenderer.OnContentChoose(flag ? CheckedContent.Title : string.Empty, BuilderUtils.GetScreenLatticePosition(CheckedContent.Anchor, base.CurrentRoom.RoomPosition), CheckedContent.proto.CoverSize);
		}
	}

	protected override void ConfirmBuild(bool showMessage = true)
	{
		if (!base.CurrentRoom.IsInHouse && posWS.y + (float)buildingProto.CoverSize.y * 1.5f > base.CurrentRoom.RoomSize.y)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrOutOfRange, showMessage);
			return;
		}
		if (!areaEmpty)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrInvalidPosition, showMessage);
			return;
		}
		if (!heightValid)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingInvalidHeight, showMessage);
			return;
		}
		if (!coveredRatioValid)
		{
			ShowErrorMessage((coveredRatio == 0f) ? base.StaticTexts.FarmbuilderErrInvalidPosition : base.StaticTexts.FarmbuilderErrBuildingInvalidCoveredRatio, showMessage);
			return;
		}
		List<Equipment> list = base.CurrentRoom.DM_terrain.GetContentsFromPositions<Equipment>(buildingProto.GetPositionsOfType(CurrentCellPosition, BuildingTileType.Door)).ToList();
		if (list.Any((Equipment equipment) => equipment.IsOccupy))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedDoor, showMessage);
			return;
		}
		if (!groundCheckValid)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingGroundInvalid, showMessage);
			return;
		}
		Building building;
		if (SelectedContent != null)
		{
			Equipment[] array = SelectedContent.GetBuildingCeilingEquipments().ToArray();
			host.MoveBuilding(SelectedContent, CurrentCellPosition, posWS);
			building = SelectedContent;
			SelectedContent = null;
			Equipment[] array2 = array;
			foreach (Equipment equipment2 in array2)
			{
				if (host.DM_terrain.AnyEmpty(equipment2.proto.GroundPositions(equipment2.Anchor), TerrainLayerName.CeilingFront))
				{
					((IEquipmentHost)base.CurrentRoom).RemoveEquipment(equipment2, base.PutInBackpack, retrieveItem: true, includeTerrain: true);
				}
			}
		}
		else
		{
			building = ((SelectedItem.buildingEntity != null) ? host.CreateBuildingFromDirty(SelectedItem.buildingEntity, CurrentCellPosition, posWS) : host.CreateBuilding(buildingProto, CurrentCellPosition, posWS));
			if (building == null)
			{
				ShowErrorMessage(base.StaticTexts.ItemBuildingProtoError, showMessage);
				return;
			}
			if (isTemporaryItem)
			{
				SelectedItem = CostItemFromInventory(TempBuildingList, SelectedItem);
			}
			else
			{
				SelectedItem = CostItemFromInventory(base.backpack, SelectedItem);
				DolocAPI.BroadcastString(GameEventType.BUILD_BUILDING, building.BuildingName);
			}
		}
		((IDungeonResourceHost)base.CurrentRoom)?.ClearResourceInPositions(building.CoveredPositions);
		foreach (Equipment item in list)
		{
			((IEquipmentHost)base.CurrentRoom).RemoveEquipment(item, base.PutInBackpack, retrieveItem: true, includeTerrain: true);
		}
		GlobalBuilderState.ValidOperation = true;
		InstAnimEffectType type = ((building.proto.CoverSize.x <= DolocAPI.GlobalParameter.EquipmentSmallThreshold) ? InstAnimEffectType.PLAYER_LAND_SMOKE : InstAnimEffectType.LARGE_SMOKE);
		DolocAPI.RaiseInstantAnimEffects(building.PositionBottom, type, "Ground");
		DolocAPI.cameraController.ShakeScreen(0.3f, (float)building.proto.CoverSize.x * DolocAPI.GlobalParameter.EquipmentShakeIntensity);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_PLACE_BUILDING_SUCCESS);
	}

	protected override void Revocation()
	{
		if (SelectedContent != null)
		{
			host.MoveBuilding(SelectedContent, SelectedContent.Anchor, SelectedContent.Position);
			InstAnimEffectType type = ((SelectedContent.proto.CoverSize.x <= DolocAPI.GlobalParameter.EquipmentSmallThreshold) ? InstAnimEffectType.PLAYER_LAND_SMOKE : InstAnimEffectType.LARGE_SMOKE);
			DolocAPI.RaiseInstantAnimEffects(SelectedContent.PositionBottom, type, "Ground");
			DolocAPI.cameraController.ShakeScreen(0.3f, (float)SelectedContent.proto.CoverSize.x * DolocAPI.GlobalParameter.EquipmentShakeIntensity);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_PLACE_BUILDING_SUCCESS);
			SelectedContent = null;
		}
	}

	protected override void Dismantle(bool showMessage = true)
	{
		if (SelectedItem != null || SelectedContent != null || CheckedContent == null)
		{
			return;
		}
		if (CheckedContent.proto.IsUnique && host.CountBuilding(CheckedContent.proto.Id) <= 1)
		{
			ShowErrorMessage(DolocUtils.Format(base.StaticTexts.FarmbuilderErrBuildingCanNotRemove, CheckedContent.Title), showMessage);
			return;
		}
		if (CheckedContent.room.DM_animal.AllAnimals.Any())
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedAnimal, showMessage);
			return;
		}
		if (CheckedContent.room.DM_equipment.AllEquipments.Any((Equipment equipment) => equipment.IsOccupy))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupied, showMessage);
			return;
		}
		if (!host.CanRemoveBuilding(CheckedContent))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingInvalidOtherNoSupport, showMessage);
			return;
		}
		Equipment[] equipments = CheckedContent.GetBuildingCeilingEquipments().ToArray();
		if (equipments.Any((Equipment equipment) => equipment.IsOccupy))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedCeiling, showMessage);
			return;
		}
		DolocAPI.ShowQuestionBox(base.StaticTexts.FarmbuilderQuestionRemove, delegate
		{
			GlobalBuilderState.ValidOperation = true;
			Equipment[] array = equipments;
			foreach (Equipment equipment2 in array)
			{
				((IEquipmentHost)base.CurrentRoom).RemoveEquipment(equipment2, base.PutInBackpack, retrieveItem: true, includeTerrain: true);
			}
			host.RemoveBuilding(CheckedContent, retrieveItem: true, base.PutInBackpack);
			CheckedContent = null;
		}, null, firstSelectConfirm: false);
	}

	protected override void DrawOccupied()
	{
		host.RenderAllBuildingsOccupied();
	}

	protected override void RecycleOccupiedGrid()
	{
		host.RecycleOccupiedBuildingsIndicator();
	}

	protected override void CreateIndicator()
	{
		indicatorRenderer = new BuildingBuilderRenderer(base.CurrentRoom.RoomPosition, buildingProto.CoverSize);
		Sprite sprite = currentBuilding?.SceneSprite ?? buildingProto.DefaultSceneSprite;
		indicatorRenderer.buildingIndicator.indicator.SetSprite(sprite, reversible: false);
	}

	protected override void RecycleIndicator()
	{
		indicatorRenderer?.Dispose();
		indicatorRenderer = null;
	}

	protected override void WaitMove()
	{
		if (!host.CanRemoveBuilding(CheckedContent))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedBuilding);
			return;
		}
		Equipment[] array = CheckedContent.GetBuildingCeilingEquipments().ToArray();
		if (array.Any((Equipment equipment) => equipment.IsOccupy))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedCeiling);
			return;
		}
		host.DM_terrain.RemoveContent(CheckedContent);
		SelectedContent = CheckedContent;
		ceilingEquipments.Clear();
		ceilingEquipments.AddRange(array);
		host.DM_terrain.RemoveContent(SelectedContent);
		foreach (Equipment ceilingEquipment in ceilingEquipments)
		{
			ceilingEquipment.Renderer.Alpha = 0.3f;
		}
		buildingSlot.Entity.sprite = SelectedContent.Renderer.spriteRenderer.sprite;
		buildingSlot.Entity.position = SelectedContent.Renderer.position;
		buildingSlot.Entity.Alpha = 0.3f;
		SelectedContent.Renderer.SetVisible(value: false);
		host.UpdateSupportAroundBuilding(SelectedContent);
		if (base.userInput.DeviceType != 0)
		{
			CurrentCellPosition = SelectedContent.Anchor;
		}
		OnPosMoved(CurrentCellPosition);
		CheckedContent = null;
	}

	private void UpdateInvalidEquipmentRenderer()
	{
		foreach (Equipment ceilingEquipment in ceilingEquipments)
		{
			ceilingEquipment.Renderer.Alpha = 1f;
		}
		ceilingEquipments.Clear();
		buildingSlot.Release();
		foreach (Equipment doorEquipment in doorEquipments)
		{
			doorEquipment.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
		doorEquipments.Clear();
	}

	private void TemporaryStorage()
	{
		if (SelectedItem != null || SelectedContent != null || CheckedContent == null)
		{
			return;
		}
		if (TempBuildingList.isFull)
		{
			ShowErrorMessage(base.StaticTexts.BuilderPanelTempBuildingFull);
			return;
		}
		if (CheckedContent.proto.IsUnique && host.CountBuilding(CheckedContent.proto.Id) <= 1)
		{
			ShowErrorMessage(DolocUtils.Format(base.StaticTexts.FarmbuilderErrBuildingCanNotRemove, CheckedContent.Title));
			return;
		}
		if (!host.CanRemoveBuilding(CheckedContent))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedBuilding);
			return;
		}
		Equipment[] array = CheckedContent.GetBuildingCeilingEquipments().ToArray();
		if (array.Any((Equipment equipment) => equipment.IsOccupy))
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrBuildingOccupiedCeiling);
			return;
		}
		Equipment[] array2 = array;
		foreach (Equipment equipment2 in array2)
		{
			((IEquipmentHost)base.CurrentRoom).RemoveEquipment(equipment2, base.PutInBackpack, retrieveItem: true, includeTerrain: true);
		}
		host.RemoveBuilding(CheckedContent, retrieveItem: false);
		Item item = DolocAPI.GenerateItem(CheckedContent.proto.BlueprintItem);
		if (item is ItemBuilding itemBuilding)
		{
			itemBuilding.buildingEntity = CheckedContent;
		}
		DolocAPI.RaiseSpriteFadeUp(CheckedContent.PositionCenter, item.uiSprite);
		int firstEmptyIndex = TempBuildingList.FirstEmptyIndex;
		_buildingViewer.RaiseSpriteFadeDown(firstEmptyIndex, item.uiSprite);
		TempBuildingList.PlaceItem(item);
		CheckedContent = null;
	}

	public void TidyBuildingInventory()
	{
		Item[] array = TempBuildingList.ReadAll();
		TempBuildingList.Clear();
		Item[] array2 = array;
		foreach (Item item in array2)
		{
			TempBuildingList.PlaceItem(item);
		}
	}

	public void DismantleAllTempBuilding()
	{
		Item[] array = TempBuildingList.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemBuilding { buildingEntity: not null } itemBuilding)
			{
				itemBuilding.buildingEntity.MoveTerrainContent(DolocAPI.AgentRealRoomCellPosition, DolocAPI.AgentPosition);
				itemBuilding.buildingEntity.RetrieveItemOnRemoval(base.PutInBackpack);
			}
		}
		TempBuildingList.Clear();
	}
}
