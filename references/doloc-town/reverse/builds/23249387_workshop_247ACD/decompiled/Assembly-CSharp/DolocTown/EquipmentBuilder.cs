using DolocTown.Config.Equipment;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class EquipmentBuilder : BuilderState<ItemEquipment, Equipment>
{
	private bool doorEmpty;

	private bool canBuildNow;

	private EquipmentInfo equipmentProto;

	private EquipmentBuilderRenderer indicatorRenderer;

	private readonly GameEntitySlot<SingleSpriteRender> equipmentSlot = new GameEntitySlot<SingleSpriteRender>();

	public override bool Construct => base.CurrentRoom.RoomConstructInfo.AllowBuildEquipment;

	private IEquipmentHost host => base.CurrentRoom;

	protected override ItemEquipment SelectedItem
	{
		get
		{
			return CurrentItem;
		}
		set
		{
			Turn = false;
			CurrentItem = value;
			RecycleIndicator();
			RecycleOccupiedGrid();
			if (value == null)
			{
				equipmentProto = null;
				return;
			}
			equipmentProto = CurrentItem.EquipmentProto;
			CreateIndicator();
			DrawOccupied();
		}
	}

	protected override Equipment SelectedContent
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
				Turn = false;
				equipmentProto = null;
				RecycleIndicator();
				RecycleOccupiedGrid();
				equipmentSlot.Release();
			}
			else
			{
				equipmentProto = CurrentContent.proto;
				Turn = CurrentContent.Turn;
				CreateIndicator();
				DrawOccupied();
			}
		}
	}

	public override bool ItemFilter(Item item)
	{
		if (!base.ItemFilter(item))
		{
			return false;
		}
		EquipmentInfo equipmentInfo = ((ItemEquipment)item).EquipmentProto;
		if (equipmentInfo.isDecal)
		{
			return false;
		}
		if (equipmentInfo.EnvType != 0)
		{
			return base.CurrentRoom.IsInHouse == (equipmentInfo.EnvType == EquipmentEnvType.INDOOR);
		}
		return true;
	}

	public override void RunBuilder(Item item)
	{
		base.RunBuilder(item);
		SelectedItem = null;
		if (ItemFilter(item))
		{
			SelectedItem = (ItemEquipment)item;
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

	protected override void OnPosMoved(Vector2Int pos)
	{
		if (equipmentProto != null)
		{
			canBuildNow = IsEquipmentBuildable(pos);
			doorEmpty = !host.DM_terrain.AnyFilledIgnoreTerrain(equipmentProto.CoveredPositions(pos), TerrainLayerName.Door);
			indicatorRenderer.GridIndicatorPosition = BuilderUtils.GetRealGridPos(pos, base.CurrentRoom.RoomPosition);
			indicatorRenderer.indicatorPos = GetWorldPosition(pos);
			indicatorRenderer.isIndicatorValid = canBuildNow;
			if (equipmentProto.Function is EquipmentFuncAffector equipmentFuncAffector)
			{
				host.UpdateAffectedEquipment(equipmentFuncAffector.GetAffectedPositions(pos, equipmentProto.CoverSize));
			}
		}
	}

	protected override void ShowTerrainContentInfo(Vector2Int pos)
	{
		bool flag = equipmentProto == null;
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

	protected override bool IsExistsContent(Vector2Int pos)
	{
		bool result = base.IsExistsContent(pos);
		if (CheckedContent != null && CheckedContent.proto.isDecal)
		{
			CheckedContent = null;
		}
		return result;
	}

	protected override void ConfirmBuild(bool showMessage = true)
	{
		if (equipmentProto == null)
		{
			return;
		}
		Vector3 worldPosition = GetWorldPosition(CurrentCellPosition);
		if (!base.CurrentRoom.IsInHouse && worldPosition.y + (float)equipmentProto.CoverSize.y * 1.5f > base.CurrentRoom.RoomSize.y)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrOutOfRange, showMessage);
			return;
		}
		if (!doorEmpty)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrEquipmentHideDoor, showMessage);
			return;
		}
		if (!canBuildNow)
		{
			ShowErrorMessage(base.StaticTexts.FarmbuilderErrInvalidPosition, showMessage);
			return;
		}
		canBuildNow = false;
		indicatorRenderer.isIndicatorValid = false;
		Equipment equipment;
		if (SelectedContent != null)
		{
			host.MoveEquipment(SelectedContent, CurrentCellPosition, worldPosition);
			equipment = SelectedContent;
			SelectedContent = null;
		}
		else
		{
			if (SelectedItem == null)
			{
				return;
			}
			string id = SelectedItem.EquipmentProto.Id;
			int equipmentLimitation = DolocAPI.GetEquipmentLimitation(id);
			if (equipmentLimitation > 0 && DolocAPI.archiveHandle.CountMainFarmEquipment(id) >= equipmentLimitation)
			{
				ShowErrorMessage(DolocUtils.Format(base.StaticTexts.FarmbuilderErrEquipmentLimited, equipmentProto.Title, equipmentLimitation), showMessage);
				return;
			}
			equipment = ((!SelectedItem.IsDirty || SelectedItem.equipmentEntity == null) ? host.CreateEquipment(worldPosition, CurrentCellPosition, SelectedItem.EquipmentProto, Turn) : host.CreateEquipmentFromDirty(worldPosition, CurrentCellPosition, SelectedItem.equipmentEntity, Turn));
			if (equipment == null)
			{
				ShowErrorMessage(base.StaticTexts.ItemEquipmentProtoError, showMessage);
				return;
			}
			SelectedItem = CostItemFromInventory(base.backpack, SelectedItem);
			DolocAPI.BroadcastString(GameEventType.BUILD_EQUIPMENT, equipment.Name);
		}
		((IDungeonResourceHost)base.CurrentRoom).ClearResourceInPositions(equipment.CoveredPositions);
		GlobalBuilderState.ValidOperation = true;
		PlaceEffect(equipment.CoveredSize.x, equipment.PositionBottom);
	}

	protected override void Revocation()
	{
		if (SelectedContent != null)
		{
			host.MoveEquipment(SelectedContent, SelectedContent.Anchor, SelectedContent.Position);
			PlaceEffect(SelectedContent.CoveredSize.x, SelectedContent.PositionBottom);
			SelectedContent = null;
		}
	}

	protected override void Dismantle(bool showMessage = true)
	{
		if (SelectedItem == null && SelectedContent == null && CheckedContent != null)
		{
			if (CheckedContent.IsOccupy)
			{
				ShowErrorMessage(CheckedContent.GetOccupyInfo(), showMessage);
				return;
			}
			GlobalBuilderState.ValidOperation = true;
			host.RemoveEquipment(CheckedContent, base.PutInBackpack);
			CheckedContent = null;
		}
	}

	protected override void DrawOccupied()
	{
		host.RenderAllEquipmentsOccupied();
		((IBuildingHost)base.CurrentRoom).RenderAllDoorOccupied();
	}

	protected override void RecycleOccupiedGrid()
	{
		host.RecycleAllGridRenderer();
	}

	protected override void CreateIndicator()
	{
		indicatorRenderer = new EquipmentBuilderRenderer
		{
			isIndicatorVisible = true
		};
		indicatorRenderer.indicator.SetSprite(equipmentProto.Sprite, equipmentProto.Reversible);
		indicatorRenderer.SetGridSize(equipmentProto.CoverSize, BuilderUtils.GetEquipmentAffectorArea(equipmentProto));
		indicatorRenderer.GridIndicatorPosition = BuilderUtils.GetRealGridPos(CurrentCellPosition, base.CurrentRoom.RoomPosition);
		indicatorRenderer.indicatorPos = GetWorldPosition(CurrentCellPosition);
		indicatorRenderer.isIndicatorValid = IsEquipmentBuildable(CurrentCellPosition);
	}

	protected override void RecycleIndicator()
	{
		indicatorRenderer?.Dispose();
		indicatorRenderer = null;
	}

	protected override void TurnIndicator()
	{
		if (equipmentProto != null && equipmentProto.Reversible)
		{
			Turn = !Turn;
			indicatorRenderer.indicator.SetSprite(Turn ? equipmentProto.TurnSprite : equipmentProto.Sprite, reversible: true);
		}
	}

	protected override void WaitMove()
	{
		SelectedContent = CheckedContent;
		equipmentSlot.Entity.sprite = SelectedContent.Renderer.Sprite;
		equipmentSlot.Entity.position = SelectedContent.Renderer.position;
		equipmentSlot.Entity.Alpha = 0.3f;
		SelectedContent.Renderer.SetVisible(value: false);
		host.DM_terrain.RemoveContent(SelectedContent);
		host.RecycleOccupiedGridRenderer(SelectedContent);
		if (base.userInput.DeviceType != 0)
		{
			CurrentCellPosition = SelectedContent.Anchor;
		}
		OnPosMoved(CurrentCellPosition);
		CheckedContent = null;
	}

	private Vector3 GetWorldPosition(Vector2Int pos)
	{
		Vector2 vector = (pos + new Vector2((float)equipmentProto.CoverSize.x / 2f, 0f)) * 1.5f + base.CurrentRoom.RoomPosition;
		vector.y -= (float)pos.x * 1E-06f;
		return vector;
	}

	private void PlaceEffect(int width, Vector2 worldPos)
	{
		bool flag = width <= DolocAPI.GlobalParameter.EquipmentSmallThreshold;
		InstAnimEffectType type = (flag ? InstAnimEffectType.SMALL_SMOKE : InstAnimEffectType.LARGE_SMOKE);
		DolocAPI.RaiseInstantAnimEffects(worldPos, type, "Ground");
		DolocAPI.cameraController.ShakeScreen(0.3f, (float)width * DolocAPI.GlobalParameter.EquipmentShakeIntensity);
		DolocAPI.Sound.PostSoundEvent(flag ? SoundEvents.PLAY_PLACE_EQUIPMENT_SMALL : SoundEvents.PLAY_PLACE_EQUIPMENT_BIG);
	}

	private bool IsEquipmentBuildable(Vector2Int pos)
	{
		bool flag = true;
		flag &= host.DM_terrain.AllEmpty(equipmentProto.CoveredPositions(pos), TerrainLayerName.Ground | TerrainLayerName.Equipment | TerrainLayerName.Door);
		if (equipmentProto.FitType == EquipmentFitType.GROUND)
		{
			return flag & host.DM_terrain.AllFilled(equipmentProto.GroundPositions(pos), TerrainLayerName.Ground);
		}
		return flag & host.DM_terrain.AllPositionsFilledInAnyLayer(equipmentProto.GroundPositions(pos), TerrainLayerName.Structure | TerrainLayerName.CeilingFront);
	}
}
