using System.Collections.Generic;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class DecalEquipmentItemBuilderTip : BuilderTipBase
{
	private class CachedHostData
	{
		public Vector3 SlotPosition;

		public Vector2Int CenterCellPosition;

		public Vector2Int Anchor;

		public (int, int, int, int) LBRT;

		public IDecalHost DecalHost;

		public int DecalSlotIndex;

		public bool LayerEmpty;

		public GridRenderer GridRenderer;
	}

	private EquipmentInfo proto;

	private EquipmentBuilderRenderer renderer;

	private Vector2Int lastPos;

	private CachedHostData currentData;

	private Dictionary<Vector2, CachedHostData> cachedDatas = new Dictionary<Vector2, CachedHostData>();

	protected override bool IsValid => proto != null;

	private IEquipmentHost equipmentHost => base.CurrentRoom;

	private bool canBuildNow => currentData != null;

	protected override void OnPosMoved(Vector2Int pos)
	{
		lastPos = pos;
		Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
		Vector2 agentWorldCellPosition = DolocAPI.AgentWorldCellPosition;
		float num = float.MaxValue;
		CachedHostData cachedHostData = currentData;
		currentData = null;
		foreach (Vector2 key in cachedDatas.Keys)
		{
			CachedHostData cachedHostData2 = cachedDatas[key];
			if (!cachedHostData2.LayerEmpty)
			{
				continue;
			}
			Vector2Int centerCellPosition = cachedHostData2.CenterCellPosition;
			if (centerCellPosition.y == agentRoomCellPosition.y && (centerCellPosition.x == agentRoomCellPosition.x || centerCellPosition.x == agentRoomCellPosition.x + (DolocAPI.AgentFaceRight ? 1 : (-1))))
			{
				currentData = cachedHostData2;
				break;
			}
			var (num2, num3, num4, num5) = cachedHostData2.LBRT;
			if (agentRoomCellPosition.x <= num4 && agentRoomCellPosition.x >= num2 && agentRoomCellPosition.y <= num5 && agentRoomCellPosition.y >= num3 - 3)
			{
				float magnitude = (agentWorldCellPosition - key).magnitude;
				if (magnitude < num)
				{
					num = magnitude;
					currentData = cachedHostData2;
				}
			}
		}
		if (cachedHostData != currentData)
		{
			cachedHostData?.GridRenderer.SetGridRendererColor(cachedHostData.LayerEmpty ? DolocAPI.eftConfig.terrainWarningColor : DolocAPI.eftConfig.terrainInvalidColor);
			currentData?.GridRenderer.SetGridRendererColor(DolocAPI.eftConfig.terrainValidColor);
		}
		if (currentData != null)
		{
			renderer.indicatorPos = currentData.SlotPosition;
			renderer.isIndicatorValid = true;
		}
	}

	private Vector2Int GetAgentAnchor()
	{
		Vector2Int agentRoomCellPosition = DolocAPI.AgentRoomCellPosition;
		if (!DolocAPI.AgentFaceRight)
		{
			agentRoomCellPosition.x -= proto.CoverSize.x;
		}
		agentRoomCellPosition.y++;
		return agentRoomCellPosition;
	}

	public override void OnUpdate(float deltaTime)
	{
		if (DolocAPI.IsGameInitialized)
		{
			base.__positionUpdateFunc(deltaTime);
		}
	}

	private void UpdateIndicatorPos()
	{
		Vector2Int agentAnchor = GetAgentAnchor();
		if (lastPos != agentAnchor)
		{
			OnPosMoved(agentAnchor);
		}
		if (currentData == null)
		{
			renderer.indicatorPos = (agentAnchor + new Vector2((float)proto.CoverSize.x / 2f, 0f)) * 1.5f + base.RoomPosition;
			renderer.isIndicatorValid = false;
		}
		else
		{
			renderer.indicatorPos = currentData.SlotPosition;
			renderer.isIndicatorValid = true;
		}
		renderer.isIndicatorVisible = true;
	}

	protected override void __UpdateFunc_JoyStick(float dt)
	{
		UpdateIndicatorPos();
	}

	protected override void __UpdateFunc_KM(float dt)
	{
		UpdateIndicatorPos();
	}

	private void CacheSlotInfo()
	{
		cachedDatas.Clear();
		DecalSlot[] fitSlots = proto.FitSlots;
		for (int i = 0; i < fitSlots.Length; i++)
		{
			DecalSlotType slotType = fitSlots[i].SlotType;
			IDecalHost[] cachedDecalHosts = base.CurrentRoom.DM_terrain.GetCachedDecalHosts(slotType);
			foreach (IDecalHost decalHost in cachedDecalHosts)
			{
				if (EquipmentManager.CreateDisposeEquipment(equipmentHost, proto, turn: false).HostFilter(decalHost))
				{
					(int, Vector3)[] slotWorldPos = decalHost.GetSlotWorldPos(slotType);
					for (int k = 0; k < slotWorldPos.Length; k++)
					{
						(int, Vector3) tuple = slotWorldPos[k];
						int item = tuple.Item1;
						Vector3 item2 = tuple.Item2;
						(int, int, int, int) coveredTileLBRT = proto.GetCoveredTileLBRT(base.RoomPosition, item2);
						Vector2Int anchorTile = proto.GetAnchorTile(coveredTileLBRT);
						Vector2Int coveredSizeBySprite = proto.GetCoveredSizeBySprite(coveredTileLBRT);
						Vector2Int centerCellPosition = new Vector2Int(anchorTile.x + coveredSizeBySprite.x / 2, anchorTile.y);
						Vector2Int[] coveredPositionsBySprite = proto.GetCoveredPositionsBySprite(coveredTileLBRT);
						bool flag = equipmentHost.IsDecalLayerEmpty(coveredPositionsBySprite);
						Vector2Int realGridPos = BuilderUtils.GetRealGridPos(anchorTile, base.RoomPosition);
						GridRenderer gridRenderer = DolocAPI.EntitySystem.Next<GridRenderer>();
						cachedDatas[item2] = new CachedHostData
						{
							SlotPosition = item2,
							CenterCellPosition = centerCellPosition,
							Anchor = anchorTile,
							LBRT = coveredTileLBRT,
							DecalHost = decalHost,
							DecalSlotIndex = item,
							LayerEmpty = flag,
							GridRenderer = gridRenderer
						};
						Color color = (flag ? DolocAPI.eftConfig.terrainWarningColor : DolocAPI.eftConfig.terrainInvalidColor);
						gridRenderer.SetGridRendererState(realGridPos, coveredSizeBySprite, color);
					}
				}
			}
		}
	}

	protected override void OnEnter()
	{
		base.OnEnter();
		CacheSlotInfo();
		renderer = new EquipmentBuilderRenderer();
		renderer.isIndicatorVisible = false;
		renderer.indicator.SetSprite(proto.Sprite, reversible: false);
		renderer.isIndicatorVisible = true;
		renderer.SetGridSize(Vector2Int.zero);
		OnPosMoved(GetAgentAnchor());
	}

	public override void RunBuilder(Item item)
	{
		if (base.CurrentRoom != null && item is ItemEquipment { EquipmentProto: { isDecal: not false } } itemEquipment)
		{
			proto = itemEquipment.EquipmentProto;
			if (base.CurrentRoom.RoomConstructInfo.AllowBuildDecal && (proto.EnvType == EquipmentEnvType.UNIVERSAL || base.CurrentRoom.IsInHouse == (proto.EnvType == EquipmentEnvType.INDOOR)))
			{
				currentData = null;
				OnEnter();
			}
		}
	}

	public override void ExitBuilder()
	{
		proto = null;
		cachedDatas.Clear();
		renderer?.Dispose();
		equipmentHost.RecycleAllGridRenderer();
		base.ExitBuilder();
	}

	public override bool ConfirmBuild()
	{
		if (!IsValid)
		{
			return false;
		}
		if (!base.CurrentRoom.RoomConstructInfo.AllowBuildDecal)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrBuildSystemNotSupport);
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
		if (!canBuildNow)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrInvalidPosition);
			return false;
		}
		renderer.isIndicatorValid = false;
		Vector3 slotPosition = currentData.SlotPosition;
		if (equipmentHost.CreateEquipment(slotPosition, currentData.Anchor, proto, turn: false, currentData.DecalHost, currentData.DecalSlotIndex) == null)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.StaticTexts.FarmbuilderErrInvalidPosition);
			return false;
		}
		DolocAPI.BroadcastString(GameEventType.BUILD_EQUIPMENT, proto.Id);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_PLACE_EQUIPMENT_SMALL);
		currentData = null;
		DolocAPI.CostSelectedItem();
		return true;
	}
}
