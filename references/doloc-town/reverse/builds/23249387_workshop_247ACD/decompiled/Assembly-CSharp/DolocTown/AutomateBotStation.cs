using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class AutomateBotStation : Equipment, IAutomateBotManager
{
	protected readonly ElectronicComponentAppliance _applianceCustom;

	protected readonly EquipmentFuncAutomateBotStation func;

	[JsonProperty]
	protected AutomateBot[] bots;

	private bool shouldUpdateLight;

	public bool IsLargeStation => func.IsLarge;

	[DebugInfo("影响范围起点(网格)", Color = "#ff4f4f")]
	public Vector2Int AreaAnchor { get; private set; }

	[DebugInfo("影响范围大小(网格)", Color = "#ff4f4f")]
	public Vector2Int AreaSize { get; private set; }

	[DebugInfo("影响范围左下角(世界)", Color = "#ff4f4f")]
	public Vector2 AreaLeftBottom { get; private set; }

	[DebugInfo("影响范围右上角(世界)", Color = "#ff4f4f")]
	public Vector2 AreaRightTop { get; private set; }

	[DebugInfo("房间Id", Color = "#fffde3")]
	public string CurrentRoomGuid => base.CurrentRoom.Title;

	public AutomateBot[] Bots => bots;

	public Vector2[] ChargingPositions => func.ChargingPositions;

	public virtual IEnumerable<AutomateBot> ActivatedBots => Bots.Where((AutomateBot bot) => bot != null && !bot.IsCharging);

	public virtual IEnumerable<AutomateBot> AllBots => Bots.Where((AutomateBot bot) => bot != null);

	public override bool IsOccupy => Bots.Any((AutomateBot bot) => bot != null);

	public AutomateStationEnv Env { get; private set; }

	[DebugInfo("自动化无人机1号", Color = "#ff4f4f")]
	private AutomateBot bot1 => bots[0];

	[DebugInfo("自动化无人机2号", Color = "#ff4f4f")]
	private AutomateBot bot2 => bots[1];

	[DebugInfo("自动化无人机3号", Color = "#ff4f4f")]
	private AutomateBot bot3 => bots[2];

	[Command("add_bot", Desc = "添加一台自动化无人机")]
	private static void Command_AddAutomateBot(string name)
	{
		if (name.IsNullOrEmpty())
		{
			Debug.LogError("请输入无人机名称");
			return;
		}
		if (!DolocConfig.Tables.TbAutomateBot.DataMap.TryGetValue(name, out var value))
		{
			Debug.LogError("没有找到目标无人机配置:" + name);
			return;
		}
		if (!(DolocAPI.SelectedEquipment is AutomateBotStation automateBotStation))
		{
			Debug.LogError("请选中一个自动化无人机基站");
			return;
		}
		for (int i = 0; i < automateBotStation.Bots.Length; i++)
		{
			if (automateBotStation.Bots[i] == null)
			{
				automateBotStation.Bots[i] = new AutomateBot(value, automateBotStation)
				{
					Renderer = DolocAPI.EntitySystem.Next<AutomateBotRenderer>()
				};
				return;
			}
		}
		Debug.LogError("无人机基站已满员");
	}

	public override string GetOccupyInfo()
	{
		return DolocConfig.StaticTexts.FarmbuilderErrAutomateBotOccupied;
	}

	public AutomateBotStation(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncAutomateBotStation)proto.Function;
		_applianceCustom = electronicComponent as ElectronicComponentAppliance;
		CalculateArea();
		bots = new AutomateBot[func.Capacity];
		for (int i = 0; i < bots.Length; i++)
		{
			bots[i] = null;
		}
	}

	[JsonConstructor]
	protected AutomateBotStation(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, AutomateBot[] bots)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = proto.Function as EquipmentFuncAutomateBotStation;
		_applianceCustom = base.electronicComponent as ElectronicComponentAppliance;
		this.bots = bots;
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		CalculateArea();
		AutomateBot[] array = bots;
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.AfterLoadEquipment(this);
		}
		ValidateBots();
	}

	public Vector2 GetChargePosition(AutomateBot bot)
	{
		int num = Array.IndexOf(bots, bot);
		if (num <= 0 || num >= ChargingPositions.Length)
		{
			num = 0;
		}
		return (Vector2)base.Position + ChargingPositions[num];
	}

	private void ValidateBots()
	{
		for (int i = 0; i < bots.Length; i++)
		{
			AutomateBot automateBot = bots[i];
			if (automateBot != null && !automateBot.IsDeseralizeValid)
			{
				automateBot._RetrieveInventoryToStation();
				bots[i] = null;
			}
		}
		if (func.Capacity == bots.Length)
		{
			return;
		}
		if (func.Capacity > bots.Length)
		{
			AutomateBot[] array = new AutomateBot[func.Capacity];
			for (int j = 0; j < bots.Length; j++)
			{
				array[j] = bots[j];
			}
			for (int k = bots.Length; k < array.Length; k++)
			{
				array[k] = null;
			}
			bots = array;
			return;
		}
		AutomateBot[] array2 = new AutomateBot[func.Capacity];
		for (int l = 0; l < func.Capacity; l++)
		{
			array2[l] = bots[l];
		}
		for (int m = func.Capacity; m < bots.Length; m++)
		{
			if (bots[m] != null)
			{
				this.CreateDropItem(bots[m].proto.Id, base.IsRender, sendMessage: false);
				bots[m].OnRemove();
				bots[m] = null;
			}
		}
		bots = array2;
	}

	private void ResetStationEnv()
	{
		Env = new AutomateStationEnv(this);
	}

	public bool IsInArea(Vector2 pos)
	{
		if (pos.x >= AreaLeftBottom.x && pos.x <= AreaRightTop.x && pos.y >= AreaLeftBottom.y)
		{
			return pos.y <= AreaRightTop.y;
		}
		return false;
	}

	private void CalculateArea()
	{
		AreaAnchor = new Vector2Int(base.Anchor.x - func.HorizontalRange, base.Anchor.y - func.VerticalRangeBottom);
		AreaSize = new Vector2Int(func.HorizontalRange * 2 + base.CoveredSize.x, func.VerticalRangeBottom + func.VerticalRangeBottom + base.CoveredSize.y);
		AreaLeftBottom = base.CurrentRoom.Geometry.CalcWorldPosition(AreaAnchor);
		AreaRightTop = DolocTransform.CalcWorldSize(AreaSize) + AreaLeftBottom;
		ResetStationEnv();
	}

	public Vector2Int GetRandomPosition()
	{
		return new Vector2Int(AreaAnchor.x, base.Anchor.y) + new Vector2Int(UnityEngine.Random.Range(0, AreaSize.x), UnityEngine.Random.Range(0, func.VerticalRangeTop + base.CoveredSize.y));
	}

	public void Unload(int botIndex)
	{
		if (botIndex >= 0 && botIndex <= Bots.Length - 1 && Bots[botIndex] != null)
		{
			this.CreateDropItem(Bots[botIndex].proto.Id, shouldRender: true, sendMessage: false);
			Bots[botIndex].OnRemove();
			Bots[botIndex] = null;
		}
	}

	public bool Load(int botIndex, Item item)
	{
		if (botIndex < 0 || botIndex > Bots.Length - 1)
		{
			return false;
		}
		if (!(item is ItemAutomateBot itemAutomateBot))
		{
			return false;
		}
		if (Bots[botIndex] != null)
		{
			return false;
		}
		Bots[botIndex] = new AutomateBot(itemAutomateBot.AutomateBotInfo, this)
		{
			Renderer = DolocAPI.EntitySystem.Next<AutomateBotRenderer>()
		};
		return true;
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveRenderComponent<GridArea>();
	}

	protected override void OnRender()
	{
		base.OnRender();
		UpdateSignalLight();
	}

	private void UpdateSignalLight()
	{
		bool flag = bots.Any((AutomateBot x) => x != null);
		LampInfo lampInfo = (flag ? func.LampOn_Ref : func.LampOff_Ref);
		base.Renderer.Sr.ToggleLightOn(lampInfo.EmissionSpriteAsset.Asset, lampInfo.EmissionColor, lampInfo.EmissionIntensity, flag);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (!base.CurrentRoom.IsInHouse)
		{
			GridArea renderComponent = base.Renderer.GetRenderComponent<GridArea>();
			renderComponent.GridPosition = BuilderUtils.GetRealGridPos(AreaAnchor, base.CurrentRoom.RoomPosition);
			renderComponent.GridSize = AreaSize;
			renderComponent.Alpha = 1f;
		}
	}

	protected override void OnDisTouch()
	{
		if (!(base.Renderer == null))
		{
			base.Renderer.RemoveRenderComponent<GridArea>();
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		DolocAPI.EnterUI((AutomateBotUiState state) => state.HandleStartUpArgs(this));
		shouldUpdateLight = true;
		SendUseEquipmentMessage();
	}

	protected override void Update()
	{
		AutomateBot[] array = bots;
		foreach (AutomateBot automateBot in array)
		{
			if (automateBot != null && automateBot.IsCharging && _applianceCustom.Launch())
			{
				automateBot.Charge(func.MotionPointProduce);
			}
		}
		if (shouldUpdateLight)
		{
			shouldUpdateLight = false;
			UpdateSignalLight();
		}
	}

	protected override void UpdateNoRender()
	{
		Update();
	}

	public override void OnMove()
	{
		base.OnMove();
		CalculateArea();
	}

	public void OnBuildingChanged(Building building, bool isRemoved)
	{
		ResetStationEnv();
		if (!isRemoved)
		{
			return;
		}
		AutomateBot[] array = bots;
		foreach (AutomateBot automateBot in array)
		{
			if (automateBot != null && automateBot.CurrentRoom == null)
			{
				automateBot.DecisionMaker.StopTask();
				automateBot.EnterMainFarm(building);
			}
		}
	}

	[DebugButton("充电完成")]
	public void SetBotPowerFull()
	{
		AutomateBot[] array = bots;
		foreach (AutomateBot automateBot in array)
		{
			automateBot?.DebugSetPower(automateBot.proto.powerCapacity);
		}
	}
}
