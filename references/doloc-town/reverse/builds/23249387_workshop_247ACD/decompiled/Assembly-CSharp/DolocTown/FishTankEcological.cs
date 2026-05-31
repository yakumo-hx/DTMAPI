using System;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FishTankEcological : Equipment, IGatherableEquipment, IApplianceHost, IFishTank, IContainer
{
	private readonly EquipmentFuncFishTankEcological func;

	private readonly Counter tuCounter;

	private readonly Counter tuCounterNoElectricity;

	private readonly ApplianceHandle worker;

	private readonly GameEntitySlot<EquipmentParticleSystemRenderer> productEffects = new GameEntitySlot<EquipmentParticleSystemRenderer>("stars");

	public bool IsGatherable => tank.HasProduct;

	public override bool IsDirty => tank.IsDirty;

	[JsonProperty]
	public FarmFishTank tank { get; private set; }

	[JsonProperty]
	public Counter updateCounter { get; private set; }

	[JsonProperty]
	public Counter workCounter { get; private set; }

	[JsonProperty]
	public bool isWorking { get; set; }

	public int Energy => 0;

	public int EnergyCapacity => 0;

	public float EnergyPercent => 0f;

	public bool CanAddFeeds => false;

	public bool shouldWork => tank.TotalFishCount > 0;

	private string CurrentPrompt
	{
		get
		{
			if (!tank.HasProduct)
			{
				return DolocConfig.StaticTexts.UiOperationView;
			}
			return DolocConfig.StaticTexts.UiOperationCollect;
		}
	}

	public ElectronicComponentAppliance Appliance { get; private set; }

	public string title => Title;

	public LinearInventory inventory => tank.inventory;

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public Equipment FishTankEquipmentEntity => this;

	public Item[] Gather()
	{
		if (!tank.HasProduct)
		{
			return Array.Empty<Item>();
		}
		return tank.GetProductItems().ToArray();
	}

	public FishTankEcological(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncFishTankEcological)proto.Function;
		tank = new FarmFishTank(this);
		workCounter = new Counter(func.WorkDuration);
		updateCounter = new Counter(func.UpdateInterval);
		isWorking = false;
		Appliance = (ElectronicComponentAppliance)electronicComponent;
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		tuCounterNoElectricity = DolocAPI.GlobalParameter.NewTuCounter;
		worker = new ApplianceHandle(this);
	}

	[JsonConstructor]
	public FishTankEcological(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, FarmFishTank tank, Counter workCounter, bool isWorking, Counter updateCounter = null)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			func = (EquipmentFuncFishTankEcological)proto.Function;
			tank.SetEquipment(this);
			this.tank = (tank.Valid ? tank : new FarmFishTank(this));
			this.workCounter = workCounter ?? new Counter(func.WorkDuration);
			this.workCounter.ValidateInterval(func.WorkDuration);
			this.updateCounter = updateCounter ?? new Counter(func.UpdateInterval);
			this.updateCounter.ValidateInterval(func.UpdateInterval);
			this.isWorking = isWorking;
			Appliance = (ElectronicComponentAppliance)electronicComponent;
			tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
			tuCounterNoElectricity = DolocAPI.GlobalParameter.NewTuCounter;
			worker = new ApplianceHandle(this);
		}
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		tank.SetEquipment(this);
	}

	public void AddFeeds(ItemInfo proto)
	{
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, CurrentPrompt);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (tank.HasProduct)
		{
			this.PushSceneOperationTip();
			tank.CollectProductItems();
			productEffects.Release();
			this.ChangeSceneOperationTipPrompt(CurrentPrompt);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_WATER_COLLECT);
		}
		else
		{
			this.PushSceneOperationTipToHide();
			tank.OpenFishTankUI(_OnCloseInventory);
		}
	}

	protected override void Update()
	{
		if (tuCounterNoElectricity.Tick())
		{
			tank.GrowFries();
		}
		worker.UpdatePerSec();
	}

	protected override void UpdateNoRender()
	{
		if (tuCounterNoElectricity.Tick())
		{
			tank.GrowFries();
		}
		worker.UpdatePerSec();
	}

	protected override void OnRender()
	{
		base.Renderer.Sr.RenderAsFishTank(func.AlphaMask.Asset, tank.TotalFishCount, base.index);
		if (tank.HasProduct)
		{
			productEffects.Do(delegate(EquipmentParticleSystemRenderer R)
			{
				R.Play(base.PositionCenter);
			});
		}
	}

	protected override void OnUnRender()
	{
		base.Renderer.Sr.RenderAsNormal();
		productEffects.Release();
	}

	public override void OnCreated()
	{
		base.OnCreated();
		tank.ApplyFishTankExtensions();
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		tank.OnHostRemove(putInBackpack);
	}

	private void _OnCloseInventory()
	{
		int value = tank.ResolveFishTank();
		if (base.IsRender)
		{
			base.Renderer.Sr.SetFishTankFishCount(value);
		}
		DolocAPI.ReQuickSelectCurrentItem();
	}

	public void OnApplianceWork()
	{
		if (!tuCounter.Tick() || !updateCounter.Tick())
		{
			return;
		}
		tank.AddMetabolism(tank.TotalMetabolismIncrease);
		if (tank.HasProduct && base.IsRender)
		{
			productEffects.Do(delegate(EquipmentParticleSystemRenderer R)
			{
				R.Play(base.PositionCenter);
			});
		}
	}

	public void OnApplianceStart()
	{
		if (base.IsRender)
		{
			base.Renderer.SetStateRendererStatus(state: true);
		}
	}

	public void OnApplianceIdle()
	{
		if (base.IsRender)
		{
			base.Renderer.SetStateRendererStatus(state: false);
		}
	}

	public void OnApplianceTurnOff()
	{
		if (base.IsRender)
		{
			base.Renderer.HideStateRenderer();
		}
	}

	public bool ContentFilter(Item content)
	{
		return tank.ContentFilter(content);
	}
}
