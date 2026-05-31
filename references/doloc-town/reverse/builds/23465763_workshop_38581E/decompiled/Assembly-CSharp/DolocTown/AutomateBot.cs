using System;
using System.Linq;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Automate;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class AutomateBot
{
	public readonly AutomateBotInfo proto;

	private AutomateBotDecisionMaker decisionMaker;

	[JsonProperty]
	private readonly AutomateParam param;

	[JsonProperty]
	[DebugInfo("房间Id")]
	private string roomGuid;

	[JsonProperty]
	[DebugInfo("锚点")]
	protected Vector2Int anchor;

	[JsonProperty]
	[DebugInfo("电量")]
	protected int power;

	[JsonProperty]
	[DebugInfo("是否在充电")]
	protected bool isCharging;

	private bool isMoving;

	private Vector2 velocityDir;

	private AutomateBotRenderer renderer;

	public bool IsInventoryFull => inventory.realItemCount >= proto.inventorySize;

	public int LeftInventorySpace => proto.inventorySize - inventory.realItemCount;

	public Vector3 ChargePosition
	{
		get
		{
			Vector2 chargePosition = Station.GetChargePosition(this);
			return new Vector3(chargePosition.x, chargePosition.y, Station.WorldPosition.y - chargePosition.y - 0.0001f);
		}
	}

	public string BotType => decisionMaker.GetType().Name;

	public bool IsDeseralizeValid => proto != null;

	[JsonProperty]
	[DebugInfo("无人机型号")]
	private string botName => proto.Id;

	public AutomateBotStation Station { get; private set; }

	[DebugInfo("是否处于等待状态(有工作但因缺少材料无法进行)")]
	public bool IsWaiting { get; set; }

	[JsonProperty]
	[DebugInfo("是否处于暂停状态")]
	public bool IsPause { get; set; }

	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	public Vector2Int PositionCell => anchor;

	public Vector2 Position => CurrentRoom?.Geometry.CalcWorldPositionCenter(anchor) ?? ((Vector2)anchor * 1.5f);

	[DebugInfo("决策系统")]
	public AutomateBotDecisionMaker DecisionMaker => decisionMaker;

	[DebugInfo("无人机参数")]
	public AutomateParam Param => param;

	public AutomateSystemLocker locker => decisionMaker.locker;

	public string CurrentRoomGuid => roomGuid;

	public bool IsCharging => isCharging;

	public bool IsLowPower => power <= 0;

	public bool IsRenderNow => renderer != null;

	public bool IsNotRenderNow => renderer == null;

	public bool RoomChanging { get; set; }

	public AutomateBotRenderer Renderer
	{
		get
		{
			return renderer;
		}
		set
		{
			if ((object)value == null)
			{
				OnUnRenderBot();
				if ((object)renderer != null)
				{
					renderer.Bot = null;
					renderer = null;
				}
			}
			else
			{
				renderer = value;
				renderer.Bot = this;
				OnRenderBot();
			}
		}
	}

	public Room CurrentRoom
	{
		get
		{
			if (roomGuid == string.Empty)
			{
				return Station.CurrentRootRoom;
			}
			if (Station.CurrentRoom.Title == roomGuid)
			{
				return Station.CurrentRoom;
			}
			return ((TemplateRoomOutdoor)Station.CurrentRootRoom).DM_building.Buildings.FirstOrDefault((Building B) => B.room.Title == roomGuid)?.room;
		}
	}

	public int Power
	{
		get
		{
			return power;
		}
		set
		{
			power = value;
			UpdatePowerImg();
		}
	}

	public void RaiseEmotion(EmotionName emotion)
	{
		if (IsRenderNow)
		{
			DolocAPI.RaiseEmotion(Renderer.transform, emotion);
		}
	}

	public void RaiseEmotion(EmotionName emotion, float probability)
	{
		if (IsRenderNow && RandomUtils.Dice(probability))
		{
			DolocAPI.RaiseEmotion(Renderer.transform, emotion);
		}
	}

	public void UpdateRenderStatus()
	{
		Room currentRoom = DolocAPI.archiveHandle.farmData.currentRoom;
		if (currentRoom.Type != RoomType.Farm)
		{
			return;
		}
		if (currentRoom.Title == roomGuid)
		{
			if (!IsRenderNow)
			{
				Renderer = DolocAPI.EntitySystem.Next<AutomateBotRenderer>();
			}
		}
		else if (IsRenderNow)
		{
			DolocAPI.EntitySystem.Recycle(Renderer);
			Renderer = null;
		}
	}

	public void EnterBuilding(Building building)
	{
		if (building != null && !(building.room.Title == roomGuid))
		{
			roomGuid = building.room.Title;
			RoomGeometry geometry = building.room.Geometry;
			if (building.IsNotCellar())
			{
				anchor = geometry.CalcCellPosition(geometry.DefaultEntryPosition + new Vector2(0f, 3f));
			}
			else
			{
				Vector2 positionWS = building.GetCellarGatePosition2() + new Vector2(0f, 3f);
				anchor = geometry.CalcCellPosition(positionWS);
			}
			RoomChanging = true;
			UpdateRenderStatus();
			decisionMaker.OnEnterRoom(building.room);
		}
	}

	public void EnterMainFarm(Building fromBuilding = null)
	{
		TemplateRoomOutdoor mainFarm = DolocAPI.archiveHandle.MainFarm;
		if (CurrentRoom == mainFarm)
		{
			return;
		}
		Vector2Int vector2Int;
		if (fromBuilding == null)
		{
			Room lastRoom = CurrentRoom;
			if (!(lastRoom is TemplateRoomInHouse))
			{
				vector2Int = mainFarm.proto.geometry.RandomGridPos;
			}
			else
			{
				Building building = DolocAPI.archiveHandle.MainFarm.DM_building.Buildings.FirstOrDefault((Building x) => x.room == lastRoom);
				if (building == null)
				{
					vector2Int = mainFarm.proto.geometry.RandomGridPos;
				}
				else
				{
					Vector2 positionWS = building.EntryPosition + new Vector2(0f, 3f);
					vector2Int = mainFarm.proto.geometry.CalcCellPosition(positionWS);
				}
			}
		}
		else
		{
			vector2Int = mainFarm.proto.geometry.CalcCellPosition(fromBuilding.EntryPosition + new Vector2(0f, 3f));
		}
		roomGuid = mainFarm.Title;
		anchor = vector2Int;
		RoomChanging = true;
		UpdateRenderStatus();
		decisionMaker.OnEnterRoom(mainFarm);
	}

	public void RaiseSpriteFadeUp(Sprite icon, bool fadeUp = true)
	{
		if (IsRenderNow)
		{
			if (fadeUp)
			{
				DolocAPI.RaiseSpriteFadeUp(Renderer.transform.position, icon);
			}
			else
			{
				DolocAPI.RaiseSpriteFadeDown(Renderer.transform.position, icon);
			}
		}
	}

	public void RaiseSpriteArrayFadeUp(Sprite[] icons, bool fadeUp = true)
	{
		if (IsRenderNow)
		{
			if (icons.Length == 1)
			{
				RaiseSpriteFadeUp(icons[0], fadeUp);
			}
			else if (fadeUp)
			{
				DolocAPI.RaiseSpriteArrayFadeUp(Renderer.transform.position, icons);
			}
			else
			{
				DolocAPI.RaiseSpriteArrayFadeDown(Renderer.transform.position, icons);
			}
		}
	}

	private void UpdatePowerImg()
	{
		if (IsRenderNow && proto.Appearance_Ref.ShowLocalBattery)
		{
			int value = (int)((float)power / ((float)proto.powerCapacity * 1f / (float)proto.powerSlot));
			value = Math.Clamp(value, 0, proto.powerSlot - 1);
			Renderer.PowerSpriteIndex = value;
		}
	}

	public void Charge(int value)
	{
		Power += value;
		if (Power >= proto.powerCapacity)
		{
			isCharging = false;
			Power = proto.powerCapacity;
			if (IsRenderNow)
			{
				Renderer.OnStopCharge();
			}
		}
	}

	public void StartToCharge()
	{
		Power = 0;
		isCharging = true;
		if (!IsNotRenderNow)
		{
			Renderer.SetRunning(value: false);
			Renderer.OnCharge(ChargePosition);
			UpdatePowerImg();
		}
	}

	public AutomateBot(AutomateBotInfo proto, AutomateBotStation station)
	{
		this.proto = proto;
		Station = station;
		roomGuid = station.Host.CurrentRoom.Title;
		anchor = station.Anchor + station.proto.CoverSize / 2;
		param = (AutomateParam)Activator.CreateInstance(proto.ParamType, this);
		param.LoadDefault();
		inventory = new LinearInventory(proto.inventorySize);
		decisionMaker = AutomateBotDecisionMaker.Create(this);
	}

	[JsonConstructor]
	public AutomateBot(AutomateParam param, string botName, string roomGuid, Vector2Int anchor, int power, bool isCharging, bool IsPause, LinearInventory inventory)
	{
		proto = DolocConfig.Tables.TbAutomateBot.GetOrDefault(botName);
		if (proto != null)
		{
			this.param = param;
			if (this.param.GetType() != proto.ParamType)
			{
				this.param = (AutomateParam)Activator.CreateInstance(proto.ParamType, this);
			}
			this.roomGuid = roomGuid;
			this.anchor = anchor;
			Power = power;
			this.isCharging = isCharging;
			this.IsPause = IsPause;
			if (this.inventory == null)
			{
				LinearInventory linearInventory2 = (this.inventory = new LinearInventory(proto.inventorySize));
			}
		}
	}

	public void AfterLoadEquipment(AutomateBotStation station)
	{
		if (proto != null)
		{
			param.AfterLoadAutomateBot(this);
			Station = station;
			decisionMaker = AutomateBotDecisionMaker.Create(this);
			decisionMaker.SetLocker(Station.CurrentRootRoom.DM_automate.Locker);
		}
	}

	private void OnRenderBot()
	{
		Renderer.Sprite = proto.sprite;
		AutomateBotAppearanceInfo appearance_Ref = proto.Appearance_Ref;
		Renderer.ToggleLocalBattery = appearance_Ref.ShowLocalBattery;
		Renderer.Sr.sortingLayerName = (isCharging ? "Default" : "GroundFront");
		if (proto.HasAnimator)
		{
			Renderer.AnimatorController = proto.animator;
			Renderer.Animator.enabled = true;
			string stateName = (isCharging ? "charge" : "idle");
			Renderer.Animator.Play(stateName, 0, UnityEngine.Random.value);
		}
		else
		{
			Renderer.Animator.enabled = false;
		}
		Renderer.LocalBatteryInfos = (isCharging ? appearance_Ref.UVInfosLocalBatteryInfosCharge : appearance_Ref.UVInfosLocalBatteryInfosIdle);
		Renderer.position = Position;
		if (isMoving)
		{
			Renderer.RestoreMove(velocityDir);
		}
		if (RoomChanging)
		{
			RoomChanging = false;
			Renderer.ShowUp();
		}
		decisionMaker.OnRender();
		UpdatePowerImg();
	}

	private void OnUnRenderBot()
	{
		decisionMaker.OnUnRender();
		if (RoomChanging)
		{
			RoomChanging = false;
			DolocAPI.RaiseSpriteFadeUp(Renderer.position, proto.sprite, 0.5f, Ease.OutExpo, 0f, Renderer.Sr.flipX);
		}
	}

	public void _RetrieveInventoryToStation()
	{
		Item[] array = inventory.ReadAll();
		foreach (Item item in array)
		{
			inventory.Take(item);
			Station.CreateDropItem(item, Station.IsRender, sendMessage: false);
		}
	}

	public void OnRemove()
	{
		decisionMaker.OnUnload();
		_RetrieveInventoryToStation();
		if (IsRenderNow)
		{
			DolocAPI.EntitySystem.Recycle(Renderer);
			Renderer = null;
		}
	}

	public void Move(Vector2Int pos, float duration = 1f)
	{
		isMoving = true;
		Room currentRoom = CurrentRoom;
		Vector2 vector = currentRoom.Geometry.CalcWorldPosition(anchor);
		Vector2 vector2 = currentRoom.Geometry.CalcWorldPosition(pos);
		velocityDir = (vector2 - vector).normalized;
		anchor = pos;
		if (IsRenderNow)
		{
			Renderer.Move(vector2, duration);
		}
	}

	public void StopMove()
	{
		isMoving = false;
	}

	public void Update()
	{
		Power--;
		decisionMaker.UpdatePerSec();
		decisionMaker.UpdatePerSec();
	}

	public void DebugSetPower(int value)
	{
		Power = value;
	}
}
