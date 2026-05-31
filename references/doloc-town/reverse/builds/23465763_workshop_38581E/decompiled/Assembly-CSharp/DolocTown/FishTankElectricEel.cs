using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class FishTankElectricEel : Equipment, IFishTank, IContainer
{
	private readonly ElectronicComponentGeneratorCustom customGenerator;

	private readonly EquipmentFuncFishTankElectricEel func;

	private readonly Counter tuCounter;

	[JsonProperty]
	[DebugInfo("食料", AllowEdit = true)]
	private int energy;

	[JsonProperty]
	[DebugInfo("代谢计数器")]
	private Counter metabolismCounter;

	public override bool IsDirty
	{
		get
		{
			if (!tank.IsDirty)
			{
				return energy > 0;
			}
			return true;
		}
	}

	[JsonProperty]
	[DebugInfo("鱼缸")]
	public FarmFishTank tank { get; private set; }

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

	public int Energy => energy;

	public int EnergyCapacity => func.EnergyCapacity;

	public float EnergyPercent => Mathf.Clamp01((float)energy / (float)EnergyCapacity);

	public bool CanAddFeeds => true;

	public Vector2 BarPosition => base.PositionTop + new Vector3(0f, 2.5f);

	public string title => Title;

	public LinearInventory inventory => tank.inventory;

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public Equipment FishTankEquipmentEntity => this;

	public FishTankElectricEel(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncFishTankElectricEel)proto.Function;
		if (electronicComponent is ElectronicComponentGeneratorCustom electronicComponentGeneratorCustom)
		{
			customGenerator = electronicComponentGeneratorCustom;
		}
		tank = new FarmFishTank(this);
		metabolismCounter = new Counter(func.UpdateInterval);
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
	}

	[JsonConstructor]
	protected FishTankElectricEel(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, FarmFishTank tank, int energy, Counter metabolismCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			func = (EquipmentFuncFishTankElectricEel)proto.Function;
			if (electronicComponent is ElectronicComponentGeneratorCustom electronicComponentGeneratorCustom)
			{
				customGenerator = electronicComponentGeneratorCustom;
			}
			tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
			tank.SetEquipment(this);
			this.tank = (tank.Valid ? tank : new FarmFishTank(this));
			this.energy = Mathf.Max(0, energy);
			this.metabolismCounter = metabolismCounter ?? new Counter(func.UpdateInterval);
			this.metabolismCounter.ValidateInterval(func.UpdateInterval);
		}
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		tank.SetEquipment(this);
	}

	protected override void Update()
	{
		if (tuCounter.Tick())
		{
			if (energy > 0)
			{
				customGenerator.Charge(func.EfficiencyPerFish * (float)tank.fishShoal.TotalFishCount);
			}
			energy = tank.AddNaturalMetabolism(energy);
		}
	}

	protected override void UpdateNoRender()
	{
		if (tuCounter.Tick())
		{
			if (energy > 0)
			{
				customGenerator.Charge(func.EfficiencyPerFish * (float)tank.fishShoal.TotalFishCount);
			}
			energy = tank.AddNaturalMetabolism(energy);
		}
	}

	private void ShowProgressBar()
	{
		if (!(base.Renderer == null))
		{
			ProgressBarRendererFeeder renderComponent = base.Renderer.GetRenderComponent<ProgressBarRendererFeeder>();
			if (!(renderComponent == null))
			{
				renderComponent.SetVisible(value: true);
				renderComponent.Progress = (float)energy / (float)func.EnergyCapacity;
				renderComponent.position = BarPosition;
			}
		}
	}

	private void HideProgressBar()
	{
		if (!(base.Renderer == null))
		{
			ProgressBarRendererFeeder progressBarRendererFeeder = base.Renderer.FetchRenderComponent<ProgressBarRendererFeeder>();
			if (progressBarRendererFeeder != null)
			{
				progressBarRendererFeeder.SetVisible(value: false);
			}
		}
	}

	private void UpdateProgressBar()
	{
		if (!(base.Renderer == null))
		{
			ProgressBarRendererFeeder progressBarRendererFeeder = base.Renderer.FetchRenderComponent<ProgressBarRendererFeeder>();
			if (progressBarRendererFeeder != null)
			{
				progressBarRendererFeeder.Progress = (float)energy / (float)func.EnergyCapacity;
			}
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, CurrentPrompt);
		ShowProgressBar();
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
		HideProgressBar();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (tank.HasProduct)
		{
			this.PushSceneOperationTip();
			tank.CollectProductItems();
			this.ChangeSceneOperationTipPrompt(CurrentPrompt);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_WATER_COLLECT);
			return;
		}
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem != null && DolocConfig.Tables.TbFeed.DataMap.TryGetValue(selectedItem.name, out var value))
		{
			selectedItem.CostSelf();
			energy += value.Energy;
			UpdateProgressBar();
		}
		else
		{
			this.PushSceneOperationTipToHide();
			tank.OpenFishTankUI(_OnCloseInventory);
		}
	}

	protected override void OnRender()
	{
		base.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_FISH_TANK;
		base.Renderer.Sr.RenderAsFishTank(func.AlphaMask.Asset, tank.TotalFishCount, base.index);
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

	protected override void OnUnRender()
	{
		base.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
	}

	private void _OnCloseInventory()
	{
		int value = tank.ResolveFishTank();
		if (base.IsRender)
		{
			base.Renderer.Sr.SetFishTankFishCount(value);
		}
	}

	public bool ContentFilter(Item content)
	{
		if (content == null)
		{
			return false;
		}
		return DolocConfig.Tables.TbFarmFish.DataMap.ContainsKey(content.name);
	}

	public void AddFeeds(ItemInfo proto)
	{
		if (proto != null && DolocConfig.Tables.TbFishFeed.IsFishFeeds(proto.Id, out var num))
		{
			energy += num;
			UpdateProgressBar();
		}
	}
}
