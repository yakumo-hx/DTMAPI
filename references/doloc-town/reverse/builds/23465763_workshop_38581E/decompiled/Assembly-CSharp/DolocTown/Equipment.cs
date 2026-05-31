using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
public abstract class Equipment : TerrainContent, IDismantleable, IDecal, IHasIndex, IDecalHost, IElectronicComponentContainer, IElectronicComponent, IWaterable
{
	[DebugObject]
	[JsonObject(MemberSerialization.OptIn)]
	public class WeatherDecoratorManager
	{
		[JsonProperty("weatherType")]
		private WeatherType currentWeatherType;

		[JsonProperty]
		private WeatherType currentDecoratorType;

		[JsonProperty]
		protected readonly Dictionary<WeatherType, WeatherDecorator> decorators = new Dictionary<WeatherType, WeatherDecorator>();

		protected WeatherDecorator currentDecorator;

		private bool isMalignantWeatherNow;

		public Equipment Equipment { get; private set; }

		[DebugInfo("酸雨装饰器", Color = "#9cbd0c")]
		public WeatherDecorator AcidRainDecorator => decorators.GetValueOrDefault(WeatherType.ACID_RAIN);

		[DebugInfo("烈日装饰器", Color = "#fbf236")]
		public WeatherDecorator ScorchSunDecorator => decorators.GetValueOrDefault(WeatherType.SCORCH_SUN);

		public WeatherDecoratorManager(Equipment equipment)
		{
			Equipment = equipment;
			currentWeatherType = WeatherType.NONE;
			isMalignantWeatherNow = false;
			currentDecorator = null;
			currentDecoratorType = WeatherType.NONE;
			WeatherDecoratorProto[] processors = equipment.proto.Processors;
			foreach (WeatherDecoratorProto weatherDecoratorProto in processors)
			{
				if (WeatherDecorator.CreateDecorator(this, weatherDecoratorProto, out var decorator))
				{
					decorators.Add(decorator.WeatherType, decorator);
				}
				else
				{
					Debug.LogError($"无法创建\"{weatherDecoratorProto.GetType()}\"的恶性天气处理器");
				}
			}
		}

		[JsonConstructor]
		public WeatherDecoratorManager(WeatherType weatherType, WeatherType currentDecoratorType, Dictionary<WeatherType, WeatherDecorator> decorators)
		{
			currentWeatherType = weatherType;
			this.decorators = decorators;
			isMalignantWeatherNow = weatherType.IsMalignantWeather();
			currentDecorator = null;
			this.currentDecoratorType = WeatherType.NONE;
			if (currentDecoratorType.IsMalignantWeather() && decorators.TryGetValue(currentDecoratorType, out var value))
			{
				this.currentDecoratorType = currentDecoratorType;
				currentDecorator = value;
			}
		}

		public void SetEquipment(Equipment equipment)
		{
			Equipment = equipment;
			foreach (WeatherDecorator value in decorators.Values)
			{
				value.RetrieveProto(this, GetDecoratorProto(value.GetType()));
			}
			Dictionary<WeatherType, WeatherDecorator> dictionary = decorators.Values.ToDictionary((WeatherDecorator p) => p.WeatherType);
			decorators.Clear();
			foreach (KeyValuePair<WeatherType, WeatherDecorator> item in dictionary)
			{
				if (item.Value.IsValid)
				{
					decorators.Add(item.Key, item.Value);
				}
				else
				{
					Debug.Log($"\"{equipment.proto.Id}\"删除无效的恶性天气装饰器\"{item.Value.GetType()}\"");
				}
			}
			WeatherDecoratorProto[] processors = equipment.proto.Processors;
			foreach (WeatherDecoratorProto proto in processors)
			{
				if (!decorators.Values.Any((WeatherDecorator x) => x.Proto == proto))
				{
					if (WeatherDecorator.CreateDecorator(this, proto, out var decorator))
					{
						Debug.Log($"设备\"{equipment.proto.Id}\"新增恶性天气装饰器\"{proto.GetType()}\"");
						decorators.Add(decorator.WeatherType, decorator);
					}
					else
					{
						Debug.LogError($"无法创建\"{decorator.GetType()}\"的恶性天气处理器");
					}
				}
			}
		}

		public void AfterLoadData()
		{
			if (decorators.Count == 0)
			{
				return;
			}
			foreach (WeatherDecorator value in decorators.Values)
			{
				value.AfterLoadData();
				if (value == currentDecorator)
				{
					value.AfterResumeDecorator();
				}
			}
		}

		public void SetWeatherType(WeatherType weatherType)
		{
			if (currentWeatherType == weatherType)
			{
				return;
			}
			currentWeatherType = weatherType;
			isMalignantWeatherNow = weatherType.IsMalignantWeather();
			if (isMalignantWeatherNow && decorators.TryGetValue(weatherType, out var value))
			{
				if (Equipment.IsRender)
				{
					currentDecorator?.End();
					value.Begin();
				}
				else
				{
					currentDecorator?.EndNoRender();
					value.BeginNoRender();
				}
				currentDecoratorType = weatherType;
				currentDecorator = value;
			}
			else if (currentDecorator != null && currentDecorator.EndOnWeatherStop)
			{
				if (Equipment.IsRender)
				{
					currentDecorator?.End();
					Equipment.OnRender();
				}
				else
				{
					currentDecorator?.EndNoRender();
				}
				currentDecoratorType = WeatherType.NONE;
				currentDecorator = null;
			}
		}

		public WeatherDecoratorProto GetDecoratorProto(Type instanceType)
		{
			string protoTypeName = instanceType.Name.Replace("WD_", "WDP_");
			return Equipment.proto.Processors.FirstOrDefault((WeatherDecoratorProto p) => p.GetType().Name == protoTypeName);
		}

		public T GetDecoratorProto<T>() where T : WeatherDecoratorProto
		{
			WeatherDecoratorProto[] processors = Equipment.proto.Processors;
			for (int i = 0; i < processors.Length; i++)
			{
				if (processors[i] is T result)
				{
					return result;
				}
			}
			return null;
		}

		public void RemoveDecorator()
		{
			currentDecoratorType = WeatherType.NONE;
			currentDecorator?.End();
			currentDecorator = null;
			Equipment.OnRender();
		}

		public void RemoveDecoratorNoRender()
		{
			currentDecoratorType = WeatherType.NONE;
			currentDecorator?.EndNoRender();
			currentDecorator = null;
		}

		public bool CheckDecorator(WeatherDecorator decorator)
		{
			return decorator == currentDecorator;
		}

		public virtual void Update()
		{
			if (currentDecorator != null)
			{
				currentDecorator.Update(isMalignantWeatherNow);
			}
			else
			{
				Equipment.Update();
			}
		}

		public virtual void UpdateNoRender()
		{
			if (currentDecorator != null)
			{
				currentDecorator.UpdateNoRender(isMalignantWeatherNow);
			}
			else
			{
				Equipment.UpdateNoRender();
			}
		}

		public virtual void OnRender()
		{
			if (currentDecorator != null)
			{
				currentDecorator.OnRender();
			}
			else
			{
				Equipment.OnRender();
			}
		}

		public virtual void OnUnRender()
		{
			if (currentDecorator != null)
			{
				currentDecorator.OnUnRender();
			}
			else
			{
				Equipment.OnUnRender();
			}
		}

		public virtual void OnTouch()
		{
			if (currentDecorator != null)
			{
				currentDecorator.OnTouch();
			}
			else
			{
				Equipment.OnTouch();
			}
		}

		public virtual void OnDisTouch()
		{
			if (currentDecorator != null)
			{
				currentDecorator.OnDisTouch();
			}
			else
			{
				Equipment.OnDisTouch();
			}
		}

		public virtual void OnInteract()
		{
			if (currentDecorator != null)
			{
				currentDecorator.OnInteract();
			}
			else
			{
				Equipment.OnInteract();
			}
		}

		public void OriginUpdate()
		{
			Equipment.Update();
		}

		public void OriginUpdateNoRender()
		{
			Equipment.UpdateNoRender();
		}

		public void OriginOnRender()
		{
			Equipment.OnRender();
		}

		public void OriginOnUnRender()
		{
			Equipment.OnUnRender();
		}

		public void OriginOnTouch()
		{
			Equipment.OnTouch();
		}

		public void OriginOnDisTouch()
		{
			Equipment.OnDisTouch();
		}

		public void OriginOnInteract()
		{
			Equipment.OnInteract();
		}
	}

	public bool WaitRemove;

	public readonly EquipmentInfo proto;

	private EquipmentInteractableBridge _interactableBridge;

	[JsonProperty]
	[DebugInfo("恶性天气装饰器组")]
	public readonly WeatherDecoratorManager decorator;

	[JsonProperty]
	[DebugInfo("电力元件")]
	protected readonly ElectronicComponent electronicComponent;

	[JsonProperty]
	[DebugInfo("是否翻转")]
	private bool turn;

	public GridRenderer OccupiedGridRenderer { get; set; }

	public IDecalHost DecalHost { get; private set; }

	public int DecalSlotIndex => decalInfo.DecalSlotIndex;

	public virtual bool TouchableAsDecal => true;

	[JsonProperty]
	public DecalInfo decalInfo { get; set; }

	public DecalInfo DecalInfo
	{
		get
		{
			return decalInfo;
		}
		set
		{
			decalInfo = value;
		}
	}

	public DecalSlot[] FitSlots => proto.FitSlots;

	public DecalSlot[] ContainedSlots => proto.ContainedSlots;

	public Dictionary<int, IDecal> AttachedDecals { get; set; } = new Dictionary<int, IDecal>();


	public Sprite HostSprite => proto.Sprite;

	public Vector3 WorldPosition => base.Position;

	[DebugInfo("设备名称", Color = "#ffff00")]
	public virtual string Title => proto.Title;

	[DebugInfo("设备类型")]
	public string EquipmentTypeName => GetType().ToString();

	[DebugInfo("设备名称", Color = "#ffff00")]
	public virtual Sprite OverrideUiSprite => null;

	public virtual string ExtraInfoAsItem { get; } = string.Empty;


	public IEquipmentHost Host { get; private set; }

	public IInteractable Interactable
	{
		get
		{
			if (_interactableBridge == null)
			{
				_interactableBridge = new EquipmentInteractableBridge(this);
			}
			return _interactableBridge;
		}
	}

	public Room CurrentRootRoom => Host?.RootRoom;

	public Room CurrentRoom => Host?.CurrentRoom;

	public virtual bool IsValid => proto != null;

	public bool Turn
	{
		get
		{
			return turn;
		}
		set
		{
			turn = value;
		}
	}

	public virtual IElectronicComponent IElectronicComponent => electronicComponent;

	public virtual bool IsElectric => proto.isElectrical;

	public virtual bool IsOccupy => false;

	public virtual bool IsDirty => false;

	public virtual Sprite EquipmentSprite
	{
		get
		{
			if (!turn)
			{
				return proto.Sprite;
			}
			return proto.TurnSprite;
		}
	}

	public Vector3 HeightLevel { get; private set; }

	public float Height => HeightLevel.z - HeightLevel.x;

	public Vector3 WidthLevel { get; private set; }

	public bool IsTouch { get; private set; }

	public bool IsRender { get; private set; }

	public EquipmentRenderer Renderer { get; set; }

	public Vector3 PositionBottom => new Vector3(base.Position.x, HeightLevel.x);

	public Vector3 PositionTop => new Vector3(base.Position.x, HeightLevel.z);

	public Vector3 PositionCenter => new Vector3(base.Position.x, HeightLevel.y);

	public virtual Vector3 PositionTip => new Vector3(base.Position.x, base.Position.y + 6f);

	public Vector2Int CoveredSize => proto.GetCoveredSize(WorldPosition);

	public Vector2Int RightBottom => new Vector2Int(base.Anchor.x + CoveredSize.x - 1, base.Anchor.y);

	public Vector3 PositionTipRT => new Vector3(WidthLevel.z, HeightLevel.z);

	public Item AsDropItem
	{
		get
		{
			Item item = DolocAPI.GenerateItem(proto.Id);
			if (item is ItemEquipment itemEquipment && IsDirty)
			{
				itemEquipment.equipmentEntity = this;
			}
			return item;
		}
	}

	public virtual bool ShowSpriteShadow => true;

	[JsonProperty("equipmentName")]
	public string Name => proto.Id;

	public virtual bool CanInteractContinues => false;

	protected AgentCellTip cellTip => DolocAPI.uiSystem.basicTip.AgentCellTip;

	public virtual void OnFell(Vector2 hitPosition)
	{
		DolocAPI.RaiseInstantAnimEffects(hitPosition, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_KNOCKING_EQUIPMENT);
		if (!WaitRemove)
		{
			if (IsOccupy)
			{
				Renderer.Shake();
				DolocAPI.ShowMessageBoxSmallErr(GetOccupyInfo());
				return;
			}
			WaitRemove = true;
			Renderer.Shiner(2f);
			Renderer.Shake(2f);
			DolocAPI.Delay(2f, OnRecover);
		}
		else
		{
			Host.RemoveEquipment(this);
			DolocAPI.RefreshScanner();
			DolocAPI.Broadcast(OperationEventType.FELL_EQUIPMENT);
		}
	}

	public void OnRecover()
	{
		WaitRemove = false;
	}

	public virtual void SetDecalHost(IDecalHost decalHost)
	{
		DecalHost = decalHost;
	}

	public virtual bool HostFilter(IDecalHost host)
	{
		return true;
	}

	public void OnTakeOff()
	{
	}

	public void RefreshPosition()
	{
		if (DecalHost != null && DecalHost.TryGetSlotWorldPosByIndex(DecalSlotIndex, out var worldPos))
		{
			(int, int, int, int) coveredTileLBRT = proto.GetCoveredTileLBRT(Host.CurrentRoom.RoomPosition, worldPos);
			Vector2Int anchorTile = proto.GetAnchorTile(coveredTileLBRT);
			Host.MoveEquipment(this, anchorTile, worldPos);
		}
	}

	public virtual string GetOccupyInfo()
	{
		return DolocConfig.StaticTexts.FarmbuilderErrEquipmentOccupied;
	}

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		TerrainLayerName key = (proto.isDecal ? TerrainLayerName.Decal : TerrainLayerName.Equipment);
		return new Dictionary<TerrainLayerName, Vector2Int[]> { 
		{
			key,
			proto.GetCoveredPositions(base.Anchor, CoveredSize)
		} };
	}

	protected Equipment(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(instanceId, anchor, wp)
	{
		this.proto = proto;
		Host = host;
		if (proto == null)
		{
			Debug.LogError($"设备\"{GetType()}\"原型为空");
			return;
		}
		this.turn = turn && proto.Reversible;
		decorator = new WeatherDecoratorManager(this);
		electronicComponent = ElectronicComponent.CreateComponent(this);
		CalcSizeInfo();
	}

	[JsonConstructor]
	protected Equipment(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position)
	{
		if (!DolocAPI.QueryEquipment(equipmentName, out var equipmentInfo))
		{
			Debug.LogError("设备\"" + equipmentName + "\"原型丢失");
			return;
		}
		proto = equipmentInfo;
		this.decorator = decorator;
		decorator?.SetEquipment(this);
		this.electronicComponent = ElectronicComponent.ValidateComponent(electronicComponent, this);
		this.decalInfo = decalInfo;
		this.turn = turn && equipmentInfo.Reversible;
	}

	protected override bool ValidateDeserialization()
	{
		return proto != null;
	}

	public virtual void AfterNewGame()
	{
	}

	public virtual void AfterLoadEquipment()
	{
		decorator?.AfterLoadData();
		electronicComponent?.AfterLoadElectricComponent();
	}

	public void SetHost(IEquipmentHost host, bool validatePosition = true)
	{
		if (host != null)
		{
			Host = host;
			if (validatePosition && !proto.isDecal)
			{
				position.x = ((float)base.Anchor.x + (float)proto.CoverSize.x / 2f) * 1.5f + CurrentRoom.RoomPosition.x;
			}
			CalcSizeInfo();
			AfterSetHost();
		}
	}

	protected virtual void AfterSetHost()
	{
	}

	protected virtual void CalcSizeInfo()
	{
		float num = (proto.SpriteSize.y - 2f) * 0.125f;
		float num2 = (proto.SpriteSize.x - 2f) * 0.125f;
		HeightLevel = new Vector3(base.Position.y, base.Position.y - 0.125f + num * 0.5f, base.Position.y - 0.125f + num);
		WidthLevel = new Vector3(base.Position.x - num2 * 0.5f, base.Position.x, base.Position.x + num2 * 0.5f);
	}

	public override void MoveTerrainContent(Vector2Int anchor, Vector3 position)
	{
		base.MoveTerrainContent(anchor, position);
		CalcSizeInfo();
	}

	public override void ShiftTerrainContent(Vector2Int offset, Vector3 positionOffset)
	{
		base.ShiftTerrainContent(offset, positionOffset);
		OnMove();
	}

	public void DecoratedInteract()
	{
		decorator.OnInteract();
	}

	public void DecoratedTouch()
	{
		IsTouch = true;
		decorator.OnTouch();
	}

	public void DecoratedDisTouch()
	{
		IsTouch = false;
		decorator.OnDisTouch();
	}

	public void DecoratedRender()
	{
		IsRender = true;
		decorator.OnRender();
	}

	public void DecoratedUnRender()
	{
		IsTouch = false;
		IsRender = false;
		decorator.OnUnRender();
	}

	public void DecoratedRemove()
	{
		OnRemove();
		if (IsRender)
		{
			decorator.OnUnRender();
		}
		IsTouch = false;
		IsRender = false;
	}

	public void DecoratedMove()
	{
		decorator.OnUnRender();
		decorator.OnRender();
		OnMove();
	}

	public void DecoratedUpdate()
	{
		decorator.Update();
	}

	public void DecoratedUpdateNoRender()
	{
		decorator.UpdateNoRender();
	}

	public void OriginalInteract()
	{
		OnInteract();
	}

	public void OriginalTouch()
	{
		IsTouch = true;
		OnTouch();
	}

	public void OriginalDisTouch()
	{
		IsTouch = false;
		OnDisTouch();
	}

	public void OriginalRender()
	{
		IsRender = true;
		OnRender();
	}

	public void OriginalUnRender()
	{
		IsTouch = false;
		IsRender = false;
		OnUnRender();
	}

	public void OriginalUpdate()
	{
		Update();
	}

	public void OriginalUpdateNoRender()
	{
		UpdateNoRender();
	}

	void IWaterable.OnWater()
	{
		OnManualWater();
	}

	protected void ShowEvaporationEffect(bool playSound = true)
	{
		DolocAPI.RaiseInstantPSEffects(PositionTop, InstantParticleEffectsType.EVAPORATION);
		if (playSound && Renderer != null)
		{
			DolocAPI.Sound.Post3DSoundEventOnce(SoundEvents.PLAY_OBJECT_EVAPORATION, Renderer.gameObject);
		}
	}

	public virtual void ShowOutline()
	{
		if (IsRender && !(proto.Function is EquipmentFuncDecorator))
		{
			Renderer.ShowOutline = true;
		}
	}

	public virtual void DisableOutline()
	{
		if (IsRender)
		{
			Renderer.ShowOutline = false;
		}
	}

	protected void ShowCellTip(Vector2Int offset, Vector2Int tilesize, bool flipWhenFaceLeft)
	{
		cellTip.Show(offset, tilesize, flipWhenFaceLeft, RefreshCellTip);
	}

	protected void HideCellTip()
	{
		cellTip.Hide();
	}

	protected void ShowAroundInventoryAreaCellTip()
	{
		Vector2Int inventoryAroundOffset = DolocAPI.GlobalParameter.InventoryAroundOffset;
		Vector2Int inventoryAroundArea = DolocAPI.GlobalParameter.InventoryAroundArea;
		ShowCellTip(inventoryAroundOffset, inventoryAroundArea, flipWhenFaceLeft: true);
		cellTip.CellTipValid = false;
	}

	protected virtual void RefreshCellTip()
	{
	}

	protected virtual void OnInteract()
	{
	}

	protected virtual void OnTouch()
	{
	}

	protected virtual void OnDisTouch()
	{
	}

	protected virtual void OnRender()
	{
	}

	protected virtual void OnUnRender()
	{
	}

	public virtual void OnCreated()
	{
	}

	public virtual Equipment Thunder(bool shouldRender)
	{
		if (this is LightningArrester lightningArrester)
		{
			lightningArrester.OnThunder(shouldRender);
			return this;
		}
		LightningArrester[] equipments = Host.GetEquipments<LightningArrester>();
		foreach (LightningArrester lightningArrester2 in equipments)
		{
			if (lightningArrester2.IsCoverPositions(base.CoveredPositions))
			{
				lightningArrester2.OnThunder(shouldRender);
				return lightningArrester2;
			}
		}
		OnThunder(shouldRender);
		return this;
	}

	public virtual void OnThunder(bool shouldRender)
	{
	}

	public virtual void RetrieveItemOnRemoval(bool putInBackpack)
	{
		this.TakeOffAllDecals(putInBackpack);
		Item item = DolocAPI.GenerateItem(proto.Id);
		if (item is ItemEquipment itemEquipment && IsDirty)
		{
			itemEquipment.equipmentEntity = this;
		}
		this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: false);
	}

	public virtual void OnFunctionChange()
	{
	}

	public virtual void OnMove()
	{
		foreach (IDecal value in AttachedDecals.Values)
		{
			value.RefreshPosition();
		}
	}

	protected virtual void OnRemove()
	{
	}

	protected virtual void Update()
	{
	}

	protected virtual void UpdateNoRender()
	{
	}

	protected virtual void OnManualWater()
	{
	}

	protected virtual void OnHostChanged()
	{
	}

	protected virtual void SendUseEquipmentMessage()
	{
		DolocAPI.BroadcastString(GameEventType.USE_EQUIPMENT, proto.Id);
	}

	protected void ShowTip(string prompt)
	{
		this.ShowSceneOperationTip(PositionTip, prompt, DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected void HideTip()
	{
		this.HideSceneOperationTip();
	}

	protected void PushTip()
	{
		this.PushSceneOperationTip();
	}

	protected void PushTipToHide()
	{
		this.PushSceneOperationTipToHide();
	}

	protected void PushTipToDisappear()
	{
		this.PushSceneOperationTipToDisappear();
	}

	protected void ChangeTipPrompt(string prompt)
	{
		this.ChangeSceneOperationTipPrompt(prompt);
	}
}
