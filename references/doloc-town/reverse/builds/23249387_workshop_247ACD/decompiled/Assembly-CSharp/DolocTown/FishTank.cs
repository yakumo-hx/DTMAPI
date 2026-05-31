using System;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class FishTank : Equipment, IGatherableEquipment, IFishTank, IContainer
{
	private readonly EquipmentFuncFishTank func;

	[JsonProperty]
	[DebugInfo("食料量", AllowEdit = true)]
	private int energy;

	[JsonProperty]
	private Counter metabolismCounter;

	private readonly Counter tuCounter;

	private GameEntitySlot<EquipmentParticleSystemRenderer> productEffects = new GameEntitySlot<EquipmentParticleSystemRenderer>("stars");

	public bool IsGatherable => tank.HasProduct;

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

	public override bool CanInteractContinues
	{
		get
		{
			if (CheckCurrentItemCanInteract(out var _))
			{
				return !IsFishTankEnergyFull;
			}
			return false;
		}
	}

	[JsonProperty]
	[DebugInfo("鱼缸")]
	public FarmFishTank tank { get; private set; }

	[DebugInfo("代谢计数器")]
	[DebugProgressBar(null)]
	private float metabolismCounterProgress => metabolismCounter.Process;

	[DebugInfo("代谢进度")]
	[DebugProgressBar(null)]
	private float metabolismProgress => (float)tank.metabolism / (float)func.MetabolismThreshold;

	[DebugInfo("当前食料总容量")]
	private float totalEnergyCapacity => func.EnergyCapacity + tank.ExtensionEnergyCapacity;

	public int Energy => energy;

	public int EnergyCapacity => func.EnergyCapacity;

	public float EnergyPercent => Mathf.Clamp01((float)energy / totalEnergyCapacity);

	public bool CanAddFeeds => true;

	public bool IsFishTankEnergyFull => (float)energy >= totalEnergyCapacity;

	public Vector2 BarPosition => base.PositionTop + new Vector3(0f, 0.25f);

	private string CurrentPrompt
	{
		get
		{
			if (tank.HasProduct)
			{
				return DolocConfig.StaticTexts.UiOperationCollect;
			}
			Item selectedItem = DolocAPI.SelectedItem;
			if (selectedItem != null && DolocConfig.Tables.TbFishFeed.IsFishFeeds(selectedItem.name, out var _))
			{
				return DolocConfig.StaticTexts.UiOperationFill;
			}
			return DolocConfig.StaticTexts.UiOperationView;
		}
	}

	public LinearInventory inventory => tank.inventory;

	public string title => proto.Title;

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

	public FishTank(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncFishTank)proto.Function;
		tank = new FarmFishTank(this);
		metabolismCounter = new Counter(func.UpdateInterval);
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
	}

	[JsonConstructor]
	public FishTank(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, FarmFishTank tank, int energy, Counter metabolismCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			func = (EquipmentFuncFishTank)proto.Function;
			tank.SetEquipment(this);
			this.tank = (tank.Valid ? tank : new FarmFishTank(this));
			this.energy = energy;
			this.metabolismCounter = metabolismCounter ?? new Counter(func.UpdateInterval);
			tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		}
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		tank.SetEquipment(this);
	}

	private void ShowProgressBar()
	{
		if (!(base.Renderer == null))
		{
			ProgressBarRendererFeeder renderComponent = base.Renderer.GetRenderComponent<ProgressBarRendererFeeder>();
			if (!(renderComponent == null))
			{
				renderComponent.SetVisible(value: true);
				renderComponent.Progress = (float)energy / totalEnergyCapacity;
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
				progressBarRendererFeeder.Progress = (float)energy / totalEnergyCapacity;
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

	private bool CheckCurrentItemCanInteract(out int feedsEnergy)
	{
		return DolocConfig.Tables.TbFishFeed.IsFishFeeds(DolocAPI.SelectedItem?.name, out feedsEnergy);
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
			return;
		}
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem != null)
		{
			if (CheckCurrentItemCanInteract(out var feedsEnergy))
			{
				if (IsFishTankEnergyFull)
				{
					DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrFeederFull);
					OpenFishTankUI();
					return;
				}
				DolocAPI.agent._Interact(delegate
				{
					PushTip();
					if (selectedItem.CostSelf(out var _, showFadeUpIcon: false))
					{
						energy += feedsEnergy;
						UpdateProgressBar();
						this.ChangeSceneOperationTipPrompt(CurrentPrompt);
					}
				});
			}
			else
			{
				this.PushSceneOperationTipToHide();
				OpenFishTankUI();
			}
		}
		else
		{
			this.PushSceneOperationTipToHide();
			OpenFishTankUI();
		}
	}

	public void AddFeeds(ItemInfo proto)
	{
		if (proto != null && DolocConfig.Tables.TbFishFeed.IsFishFeeds(proto.Id, out var num))
		{
			energy += num;
			UpdateProgressBar();
		}
	}

	private void OpenFishTankUI()
	{
		tank.OpenFishTankUI(_OnCloseInventory);
	}

	protected override void Update()
	{
		if (tuCounter.Tick())
		{
			tank.GrowFries();
			if (energy > 0 && metabolismCounter.Tick())
			{
				Metabolism(shouldRender: true);
			}
		}
	}

	protected override void UpdateNoRender()
	{
		if (tuCounter.Tick())
		{
			tank.GrowFries();
			if (energy > 0 && metabolismCounter.Tick())
			{
				Metabolism(shouldRender: false);
			}
		}
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
		base.Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
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

	private void Metabolism(bool shouldRender)
	{
		energy = tank.AddNaturalMetabolism(energy);
		if (shouldRender && tank.HasProduct)
		{
			productEffects.Do(delegate(EquipmentParticleSystemRenderer R)
			{
				R.Play(base.PositionCenter);
			});
		}
	}

	[DebugButton("生成鱼产品")]
	private void GenFishProduct()
	{
		tank.GenFishProcut();
	}

	[DebugButton("模拟生成鱼产品")]
	private void SimulateFishProduct()
	{
		tank.SimulateFishProduct();
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

	public bool ContentFilter(Item content)
	{
		return tank.ContentFilter(content);
	}
}
