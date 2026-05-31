using System.Linq;
using DolocTown.Config.Building;
using UnityEngine;

namespace DolocTown;

public class BuildingItemBuilderTip : BuilderTipBase
{
	private ItemBuilding currentItem;

	private BuildingBuilderRenderer indicaterRenderer;

	private Vector3 posWS;

	private bool areaEmpty;

	private bool doorEmpty;

	private bool heightValid;

	private float coveredRatio;

	private bool groundCheckValid;

	private Vector2Int lastPos;

	private Vector2Int _anchor;

	private BuildingSupport buildingSupport;

	protected override bool IsValid => proto != null;

	private IBuildingHost host => base.CurrentRoom;

	private BuildingInfo proto => currentItem?.BuildingProto;

	private bool coveredRatioValid => coveredRatio >= DolocAPI.GlobalParameter.MinFloorCoveredRatio;

	protected override void OnPosMoved(Vector2Int pos)
	{
		lastPos = pos;
		Vector2Int realGridPos = BuilderUtils.GetRealGridPos(pos, base.RoomPosition);
		areaEmpty = true;
		areaEmpty &= base.CurrentRoom.DM_terrain.AllEmpty(proto.GetCoverPositions(pos), TerrainLayerName.Ground);
		areaEmpty &= base.CurrentRoom.DM_terrain.AllEmpty(proto.GetPositionsOfType(pos, BuildingTileType.NoOverlay), TerrainLayerName.Ground | TerrainLayerName.BuildingNoOverlay);
		doorEmpty = base.CurrentRoom.DM_terrain.AllEmpty(proto.GetPositionsOfType(pos, BuildingTileType.Door), TerrainLayerName.Equipment);
		groundCheckValid = proto.GroundDepth <= 0 || (base.CurrentRoom.DM_terrain.AllFilled(proto.GetGroundPositions(pos, proto.GroundDepth), TerrainLayerName.Ground) && base.CurrentRoom.DM_terrain.AllEmpty(proto.GetGroundPositions(pos, proto.GroundDepth), TerrainLayerName.ExtraObstacles));
		posWS = (pos + new Vector2((float)proto.CoverSize.x / 2f, 0f)) * 1.5f + base.RoomPosition;
		posWS.y -= (float)pos.x * 1E-06f;
		coveredRatio = 0f;
		heightValid = true;
		if (areaEmpty && doorEmpty)
		{
			buildingSupport = host.GenerateBuildingSupport(proto, pos);
			if (buildingSupport != null)
			{
				float num = proto.GetPositionsOfType(pos, BuildingTileType.Floor, BuildingTileType.Side).Length;
				coveredRatio = (float)buildingSupport.ContactPositions.Length / num;
				heightValid = buildingSupport.ColumnHeights.Max() <= DolocAPI.GlobalParameter.MaxSupportHeight;
			}
		}
		indicaterRenderer.ShowBuildingDraft(realGridPos, posWS, areaEmpty && doorEmpty && coveredRatioValid && groundCheckValid);
		if (areaEmpty && groundCheckValid)
		{
			float num2 = coveredRatio;
			if (num2 > 0f && num2 < 1f)
			{
				indicaterRenderer.ShowSupportDraft(buildingSupport, heightValid);
				return;
			}
		}
		indicaterRenderer.HideSupportDraft();
	}

	public override void OnUpdate(float deltaTime)
	{
		if (DolocAPI.IsGameInitialized)
		{
			base.__positionUpdateFunc(deltaTime);
			if (lastPos != _anchor)
			{
				OnPosMoved(_anchor);
			}
		}
	}

	protected override void __UpdateFunc_JoyStick(float dt)
	{
		if (_timer.Tick(dt))
		{
			JoyStickMoveOffset += DolocAPI.UserInput.AssistMove * 12f;
			if (JoyStickMoveOffset == Vector2.zero)
			{
				_anchor = GetFrontPosition(proto.CoverSize.x);
				return;
			}
			Vector2Int vector2Int = Vector2Int.FloorToInt(JoyStickMoveOffset / 12f);
			_anchor = DolocAPI.AgentRoomCellPosition + vector2Int;
		}
	}

	protected override void __UpdateFunc_KM(float dt)
	{
		Vector2 positionWS = DolocAPI.ScreenToWorld(DolocAPI.UserInput.MousePosition);
		_anchor = base.CurrentRoom.Geometry.CalcMinCellPosition(positionWS);
	}

	protected override void OnEnter()
	{
		base.OnEnter();
		indicaterRenderer = new BuildingBuilderRenderer(base.RoomPosition, proto.CoverSize);
		Sprite sprite = currentItem.buildingEntity?.SceneSprite ?? proto.DefaultSceneSprite;
		indicaterRenderer.buildingIndicator.indicator.SetSprite(sprite, reversible: false);
		_anchor = GetFrontPosition(proto.CoverSize.x);
		OnPosMoved(_anchor);
		host.RenderAllBuildingsOccupied();
	}

	public override void RunBuilder(Item item)
	{
		if (base.CurrentRoom != null && item is ItemBuilding itemBuilding)
		{
			currentItem = itemBuilding;
			if (base.CurrentRoom.RoomConstructInfo.AllowBuildBuilding && !base.CurrentRoom.IsInHouse)
			{
				OnEnter();
			}
		}
	}

	public override void ExitBuilder()
	{
		currentItem = null;
		host.RecycleOccupiedBuildingsIndicator();
		indicaterRenderer?.Dispose();
		base.ExitBuilder();
	}

	public override bool ConfirmBuild()
	{
		if (!IsValid)
		{
			return false;
		}
		if (!base.CurrentRoom.IsInHouse && posWS.y + (float)proto.CoverSize.y * 1.5f > base.CurrentRoom.RoomSize.y)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrOutOfRange);
			return false;
		}
		if (!base.CurrentRoom.RoomConstructInfo.AllowBuildBuilding)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildSystemNotSupport);
			return false;
		}
		if (base.CurrentRoom.IsInHouse)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrInvalidPosition);
			return false;
		}
		if (!areaEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildingAreaNotEmpty);
			return false;
		}
		if (!doorEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildingInvalidDoorBeHidden);
			return false;
		}
		if (!heightValid)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildingInvalidHeight);
			return false;
		}
		if (!coveredRatioValid)
		{
			DolocAPI.ShowMessageBoxSmallErr((coveredRatio == 0f) ? base.StaticTexts.FarmbuilderErrInvalidPosition : base.StaticTexts.FarmbuilderErrBuildingInvalidCoveredRatio);
			return false;
		}
		if (!groundCheckValid)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildingGroundInvalid);
			return false;
		}
		indicaterRenderer.HideAll();
		Building building = host.CreateBuilding(proto, _anchor, posWS);
		if (building == null)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.ItemBuildingProtoError);
			return false;
		}
		((IDungeonResourceHost)base.CurrentRoom)?.ClearResourceInPositions(building.CoveredPositions);
		DolocAPI.BroadcastString(GameEventType.BUILD_BUILDING, proto.Id);
		InstAnimEffectType type = ((proto.CoverSize.x <= DolocAPI.GlobalParameter.EquipmentSmallThreshold) ? InstAnimEffectType.PLAYER_LAND_SMOKE : InstAnimEffectType.LARGE_SMOKE);
		DolocAPI.RaiseInstantAnimEffects(building.PositionBottom, type, "Ground");
		DolocAPI.cameraController.ShakeScreen(0.3f, (float)proto.CoverSize.x * DolocAPI.GlobalParameter.EquipmentShakeIntensity);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_PLACE_BUILDING_SUCCESS);
		DolocAPI.CostSelectedItem();
		host.CurrentRoom.OnBuildingChanged(building, isRemoved: false);
		return true;
	}
}
