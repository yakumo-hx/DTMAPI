using System.Collections.Generic;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class EquipmentItemBuilderTip : BuilderTipBase
{
	private ItemEquipment item;

	private EquipmentBuilderRenderer renderer;

	private Vector3 posWS;

	private bool canBuildNow;

	private bool areaEmpty;

	private bool doorEmpty;

	private Vector2Int lastPos;

	private Vector2Int _anchor;

	private Vector2 indicatorOffset;

	private readonly Dictionary<string, bool> limitationCache = new Dictionary<string, bool>();

	private readonly Dictionary<Vector2Int, bool> buildableCache = new Dictionary<Vector2Int, bool>();

	protected override bool IsValid
	{
		get
		{
			if (item != null)
			{
				return proto != null;
			}
			return false;
		}
	}

	private IEquipmentHost host => base.CurrentRoom;

	private EquipmentInfo proto => item.EquipmentProto;

	private Vector2Int BuilderArea => DolocAPI.GlobalParameter.BuilderAroundArea;

	private void ClearCache()
	{
		limitationCache.Clear();
		buildableCache.Clear();
	}

	private bool IsEquipmentBuildable(Vector2Int pos, IEnumerable<Vector2Int> cvPositions)
	{
		if (buildableCache.TryGetValue(pos, out var value))
		{
			return value;
		}
		bool flag = host.IsEquipmentBuildable(cvPositions, proto.GroundPositions(pos), proto.FitType == EquipmentFitType.GROUND);
		buildableCache[pos] = flag;
		return flag;
	}

	private bool IsLimit(EquipmentInfo proto, bool showMessage)
	{
		string id = proto.Id;
		if (limitationCache.TryGetValue(id, out var value) && !showMessage)
		{
			return value;
		}
		int equipmentLimitation = DolocAPI.GetEquipmentLimitation(id);
		int num = DolocAPI.archiveHandle.CountMainFarmEquipment(id);
		bool flag = equipmentLimitation > 0 && num >= equipmentLimitation;
		limitationCache[id] = flag;
		if (flag && showMessage)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(base.StaticTexts.FarmbuilderErrEquipmentLimited, proto.Title, equipmentLimitation));
		}
		return value;
	}

	protected override void OnPosMoved(Vector2Int pos)
	{
		lastPos = pos;
		Vector2Int[] array = proto.CoveredPositions(pos);
		renderer.GridIndicatorPosition = BuilderUtils.GetRealGridPos(pos, base.RoomPosition);
		canBuildNow = !IsLimit(proto, showMessage: false) && IsEquipmentBuildable(pos, array);
		doorEmpty = !base.CurrentRoom.DM_terrain.AnyFilledIgnoreTerrain(array, TerrainLayerName.Door);
		areaEmpty = !base.CurrentRoom.DM_terrain.AnyFilledIgnoreTerrain(array, TerrainLayerName.Resource);
		posWS = (pos + new Vector2((float)proto.CoverSize.x / 2f, 0f)) * 1.5f + base.RoomPosition;
		posWS.y -= (float)pos.x * 1E-06f;
		renderer.indicatorPos = posWS;
		renderer.isIndicatorValid = canBuildNow;
	}

	public override void OnUpdate(float deltaTime)
	{
		if (DolocAPI.IsGameInitialized)
		{
			base.__positionUpdateFunc(deltaTime);
			if (!(lastPos == _anchor))
			{
				OnPosMoved(_anchor);
				UpdateAffectedEquipment(_anchor);
			}
		}
	}

	protected override void __UpdateFunc_JoyStick(float dt)
	{
		if (!_timer.Tick(dt))
		{
			return;
		}
		Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
		JoyStickMoveOffset += DolocAPI.UserInput.AssistMove * 12f;
		if (JoyStickMoveOffset == Vector2.zero)
		{
			_anchor = GetFrontPosition(proto.CoverSize.x);
			return;
		}
		DolocAPI.GetAgentCellAreaAnchor(new Vector2Int(-1, 0), BuilderArea, flipWhenFaceLeft: false, out var anchor);
		Vector2Int vector2Int = Vector2Int.FloorToInt(JoyStickMoveOffset / 12f);
		_anchor = agentRoomCellPosition + vector2Int;
		Vector2Int vector2Int2 = anchor - proto.CoverSize;
		Vector2Int vector2Int3 = anchor + BuilderArea;
		if (_anchor.x <= vector2Int2.x || _anchor.x >= vector2Int3.x || _anchor.y <= vector2Int2.y || _anchor.y >= vector2Int3.y)
		{
			renderer.border.position = BuilderUtils.GetScreenLatticePosition(anchor, base.RoomPosition);
			renderer.border.SetVisible(value: true);
			_anchor.x = Mathf.Clamp(_anchor.x, vector2Int2.x, vector2Int3.x);
			_anchor.y = Mathf.Clamp(_anchor.y, vector2Int2.y, vector2Int3.y);
			JoyStickMoveOffset = new Vector2(Mathf.Clamp(JoyStickMoveOffset.x, (vector2Int2.x - agentRoomCellPosition.x) * 12, (vector2Int3.x - agentRoomCellPosition.x) * 12), Mathf.Clamp(JoyStickMoveOffset.y, (vector2Int2.y - agentRoomCellPosition.y) * 12, (vector2Int3.y - agentRoomCellPosition.y) * 12));
		}
		else
		{
			renderer.border.SetVisible(value: false);
		}
	}

	protected override void __UpdateFunc_KM(float dt)
	{
		Vector2 positionWS = DolocAPI.ScreenToWorld(DolocAPI.UserInput.MousePosition);
		DolocAPI.GetAgentCellAreaAnchor(new Vector2Int(-1, 0), BuilderArea, flipWhenFaceLeft: false, out var anchor);
		Vector2Int vector2Int = anchor - Vector2Int.CeilToInt((Vector2)proto.CoverSize / 2f);
		Vector2Int vector2Int2 = anchor + BuilderArea + proto.CoverSize / 2;
		Vector2Int vector2Int3 = base.CurrentRoom.Geometry.CalcMinCellPosition(positionWS);
		renderer.border.position = BuilderUtils.GetScreenLatticePosition(anchor, base.RoomPosition);
		renderer.border.SetVisible(value: true);
		if (vector2Int3.x < vector2Int.x || vector2Int3.x > vector2Int2.x || vector2Int3.y < vector2Int.y || vector2Int3.y > vector2Int2.y)
		{
			_anchor = GetFrontPosition(proto.CoverSize.x);
		}
		else
		{
			_anchor = vector2Int3 - proto.CoverSize / 2;
		}
	}

	private void DrawOccupied()
	{
		host.RenderAllEquipmentsOccupied();
		((IBuildingHost)base.CurrentRoom).RenderAllDoorOccupied();
	}

	private void UpdateAffectedEquipment(Vector2Int anchor)
	{
		if (proto.Function is EquipmentFuncAffector equipmentFuncAffector)
		{
			host.UpdateAffectedEquipment(equipmentFuncAffector.GetAffectedPositions(anchor, proto.CoverSize));
		}
	}

	public override void TurnIndicator()
	{
		if (IsValid && proto.Reversible)
		{
			Turn = !Turn;
			renderer.indicator.SetSprite(Turn ? proto.TurnSprite : proto.Sprite, reversible: true);
		}
	}

	protected override void OnEnter()
	{
		base.OnEnter();
		renderer = new EquipmentBuilderRenderer
		{
			isIndicatorVisible = true
		};
		renderer.indicator.SetSprite(proto.Sprite, proto.Reversible);
		renderer.SetGridSize(proto.CoverSize, BuilderUtils.GetEquipmentAffectorArea(proto));
		renderer.BoarderSizeDelta = DolocAPI.GlobalParameter.BuilderAroundArea;
		_anchor = base.CurrentRoom.Geometry.CalcCellPosition(DolocAPI.AgentPosition);
		OnPosMoved(_anchor);
		DrawOccupied();
		UpdateAffectedEquipment(_anchor);
	}

	public override void RunBuilder(Item item)
	{
		if (base.CurrentRoom != null && item is ItemEquipment itemEquipment)
		{
			ClearCache();
			this.item = itemEquipment;
			if (base.CurrentRoom.RoomConstructInfo.AllowBuildEquipment && (proto.EnvType == EquipmentEnvType.UNIVERSAL || base.CurrentRoom.IsInHouse == (proto.EnvType == EquipmentEnvType.INDOOR)))
			{
				OnEnter();
			}
		}
	}

	public override void ExitBuilder()
	{
		ClearCache();
		item = null;
		renderer?.Dispose();
		host.RecycleAllGridRenderer();
		base.ExitBuilder();
	}

	public override bool ConfirmBuild()
	{
		if (!IsValid)
		{
			return false;
		}
		if (!base.CurrentRoom.RoomConstructInfo.AllowBuildEquipment)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildSystemNotSupport);
			return false;
		}
		if (!base.CurrentRoom.IsInHouse && posWS.y + (float)proto.CoverSize.y * 1.5f > base.CurrentRoom.RoomSize.y)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrOutOfRange);
			return false;
		}
		if (IsLimit(proto, showMessage: true))
		{
			return false;
		}
		if (base.CurrentRoom.IsInHouse)
		{
			if (proto.EnvType == EquipmentEnvType.OUTDOOR)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrOutdoorEquipment);
				return false;
			}
		}
		else if (proto.EnvType == EquipmentEnvType.INDOOR)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrIndoorEquipment);
			return false;
		}
		if (!doorEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrEquipmentHideDoor);
			return false;
		}
		if (!areaEmpty)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrEquipmentAreaNotEmpty);
			return false;
		}
		if (!canBuildNow)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrInvalidPosition);
			return false;
		}
		canBuildNow = false;
		renderer.isIndicatorValid = false;
		Equipment equipment;
		if (item.IsDirty && item.equipmentEntity != null)
		{
			Debug.Log("从脏设备道具中创建设备实例");
			equipment = host.CreateEquipmentFromDirty(posWS, _anchor, item.equipmentEntity, Turn);
		}
		else
		{
			equipment = host.CreateEquipment(posWS, _anchor, proto, Turn);
		}
		if (equipment == null)
		{
			Debug.LogError("创建设备\"" + item.proto.Id + "\"失败");
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.ItemEquipmentProtoError);
			return false;
		}
		if (!item.IsDirty)
		{
			host.AddOccupiedGrid(equipment);
		}
		DolocAPI.BroadcastString(GameEventType.BUILD_EQUIPMENT, proto.Id);
		bool flag = proto.CoverSize.x <= DolocAPI.GlobalParameter.EquipmentSmallThreshold;
		InstAnimEffectType type = (flag ? InstAnimEffectType.SMALL_SMOKE : InstAnimEffectType.LARGE_SMOKE);
		DolocAPI.RaiseInstantAnimEffects(equipment.PositionBottom, type, "Ground");
		DolocAPI.cameraController.ShakeScreen(0.3f, (float)proto.CoverSize.x * DolocAPI.GlobalParameter.EquipmentShakeIntensity);
		DolocAPI.Sound.PostSoundEvent(flag ? SoundEvents.PLAY_PLACE_EQUIPMENT_SMALL : SoundEvents.PLAY_PLACE_EQUIPMENT_BIG);
		DolocAPI.CostSelectedItem(1, showFadeUpIcon: true);
		return true;
	}
}
