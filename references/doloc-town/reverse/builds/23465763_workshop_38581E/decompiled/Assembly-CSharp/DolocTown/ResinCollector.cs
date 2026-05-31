using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class ResinCollector : Equipment, IGatherableEquipment
{
	protected EquipmentFuncResinCollector func;

	[JsonProperty]
	private Counter counter;

	[JsonProperty]
	private int currentValue;

	public bool IsGatherable => currentValue >= func.Capacity;

	public override bool ShowSpriteShadow => false;

	public override Vector3 PositionTip => new Vector3(base.Position.x, base.Position.y + 3f);

	public Item[] Gather()
	{
		Item item = DolocAPI.GenerateItem(GetTargetOutputInfo().Output, currentValue);
		UpdateCurrentValue(0);
		return new Item[1] { item };
	}

	public ResinCollector(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncResinCollector)proto.Function;
		counter = new Counter(func.Interval * DolocAPI.GlobalParameter.TULength);
	}

	[JsonConstructor]
	protected ResinCollector(int id, Vector2Int anchor, Vector3 position, string equipmentName, Counter counter, int currentValue, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncResinCollector)proto.Function;
		this.counter = counter;
		this.counter.ValidateInterval(func.Interval * DolocAPI.GlobalParameter.TULength);
		this.currentValue = currentValue;
	}

	public override bool HostFilter(IDecalHost host)
	{
		if (GetTargetOutputInfo(host).Disable)
		{
			return false;
		}
		if (host is DungeonResourceTree dungeonResourceTree)
		{
			return dungeonResourceTree.currentLevel >= dungeonResourceTree.Proto.MaxLevel - 1;
		}
		if (host is PlantBasinTree plantBasinTree)
		{
			if (plantBasinTree.Crop == null)
			{
				return false;
			}
			return plantBasinTree.Crop.CurrentLevel >= plantBasinTree.Crop.protoTree.MaxLevel - 1;
		}
		return false;
	}

	public override void OnCreated()
	{
		base.OnCreated();
		if (base.DecalHost is DungeonResourceTree dungeonResourceTree)
		{
			dungeonResourceTree.SetMaxHealth();
			if (dungeonResourceTree.Renderer != null)
			{
				dungeonResourceTree.Renderer.Shake();
				DolocAPI.cameraController.ShakeScreen(0.1f, 0.2f);
			}
			if (base.Renderer != null)
			{
				DolocAPI.RaiseInstantAnimEffects(base.Renderer.position2d, InstAnimEffectType.IMPACT_01);
				DolocAPI.RaiseInstantPSEffects(base.Renderer.position2d, InstantParticleEffectsType.SAWDUST);
			}
		}
		else if (base.DecalHost is PlantBasinTree { Crop: not null } plantBasinTree)
		{
			plantBasinTree.Crop.SetMaxHealth();
			if (plantBasinTree.Crop.Renderer != null)
			{
				plantBasinTree.Crop.Renderer.Shake();
				DolocAPI.cameraController.ShakeScreen(0.1f, 0.2f);
			}
			if (base.Renderer != null)
			{
				DolocAPI.RaiseInstantAnimEffects(base.Renderer.position2d, InstAnimEffectType.IMPACT_01);
				DolocAPI.RaiseInstantPSEffects(base.Renderer.position2d, InstantParticleEffectsType.SAWDUST);
			}
		}
	}

	public override void OnFell(Vector2 hitPosition)
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_KNOCKING_EQUIPMENT);
		base.Host.RemoveEquipment(this);
		DolocAPI.RefreshScanner();
		DolocAPI.Broadcast(OperationEventType.FELL_EQUIPMENT);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (currentValue > 0)
		{
			ShowTip(DolocConfig.StaticTexts.UiOperationCollect);
		}
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		DisableOutline();
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		DolocAPI.agent._Interact(delegate
		{
			if (currentValue <= 0)
			{
				DolocAPI.RaiseEmotionLimited(DolocAPI.AgentTransform, EmotionName.CONFUSE);
			}
			else
			{
				Collect(putInBackpack: false);
				UpdateCurrentValue(0);
			}
		});
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		Collect(putInBackpack);
	}

	private void Collect(bool putInBackpack)
	{
		ResinCollectorOutputInfo targetOutputInfo = GetTargetOutputInfo();
		for (int i = 0; i < currentValue; i++)
		{
			this.PlaceItemInBagOrCreateDropItem(targetOutputInfo.Output, putInBackpack, sendMessage: true);
		}
		if (base.IsRender)
		{
			HideTip();
		}
	}

	private ResinCollectorOutputInfo GetTargetOutputInfo(IDecalHost host = null)
	{
		if (host == null)
		{
			host = base.DecalHost;
		}
		ResinCollectorOutputInfo resinCollectorOutputInfo = null;
		if (host is DungeonResourceTree dungeonResourceTree)
		{
			resinCollectorOutputInfo = dungeonResourceTree.Proto.ResinCollectorOutput_Ref;
		}
		else if (host is PlantBasinTree { Crop: not null } plantBasinTree)
		{
			resinCollectorOutputInfo = plantBasinTree.Crop.protoTree.ResinCollectorOutput_Ref;
		}
		if (resinCollectorOutputInfo == null)
		{
			resinCollectorOutputInfo = func.DefaultOutput_Ref;
		}
		return resinCollectorOutputInfo;
	}

	protected override void OnRender()
	{
		base.OnRender();
		RefreshSprite();
	}

	protected override void Update()
	{
		UpdateNoRender();
	}

	protected override void UpdateNoRender()
	{
		if (currentValue != func.Capacity && counter.Tick())
		{
			int randomCount = GetTargetOutputInfo().Range.RandomCount;
			UpdateCurrentValue(currentValue + randomCount);
		}
	}

	private void UpdateCurrentValue(int value)
	{
		GetTargetOutputInfo();
		currentValue = Mathf.Clamp(value, 0, func.Capacity);
		RefreshSprite();
	}

	private void RefreshSprite()
	{
		if (base.IsRender)
		{
			ResinCollectorOutputInfo targetOutputInfo = GetTargetOutputInfo();
			base.Renderer.Sprite = ((currentValue == func.Capacity) ? targetOutputInfo.FullAsset.Asset : ((currentValue > 0) ? targetOutputInfo.FilledAsset.Asset : proto.Sprite));
			base.Renderer.ToggleShine(currentValue == func.Capacity);
		}
	}
}
