using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class Barrel : Equipment, IWaterContainer
{
	private readonly EquipmentFuncBarrel func;

	[JsonProperty]
	public int currentValue;

	[JsonProperty]
	public Counter counter;

	private float reciprocalCapacity => 1f / (float)func.Capacity;

	public override bool IsDirty => currentValue > 0;

	public int Water => currentValue;

	public Barrel(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncBarrel)proto.Function;
		counter = DolocAPI.GlobalParameter.NewTuCounter;
	}

	[JsonConstructor]
	protected Barrel(int id, string equipmentName, Vector3 position, Vector2Int anchor, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, int currentValue, Counter counter = null)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncBarrel)proto.Function;
		this.currentValue = currentValue;
		this.counter = counter ?? DolocAPI.GlobalParameter.NewTuCounter;
	}

	protected override void Update()
	{
		if (base.Host.CurrentWeatherInfo.IsRainy && counter.Tick())
		{
			AddWater(1);
			if (base.IsTouch)
			{
				UpdateProgressBar();
			}
		}
	}

	protected override void UpdateNoRender()
	{
		if (base.Host.CurrentWeatherInfo.IsRainy && counter.Tick())
		{
			AddWater(1);
		}
	}

	private void AddWater(int value)
	{
		if (value > 0)
		{
			currentValue += value;
			if (currentValue > func.Capacity)
			{
				currentValue = func.Capacity;
			}
		}
	}

	public int TakeWater(int require)
	{
		if (require <= 0)
		{
			return 0;
		}
		if (currentValue <= 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrRunoutWater);
			return 0;
		}
		int num = Mathf.Min(currentValue, require);
		currentValue -= num;
		if (!base.IsRender)
		{
			return num;
		}
		UpdateProgressBar();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_TAKE_WATER);
		return num;
	}

	public void Evaporation(int value, bool shouldRender = false)
	{
		if (value > 0)
		{
			currentValue -= value;
			if (currentValue < 0)
			{
				currentValue = 0;
			}
			if (shouldRender && base.IsTouch)
			{
				UpdateProgressBar();
				ShowEvaporationEffect(playSound: false);
			}
		}
	}

	protected override void OnTouch()
	{
		UpdateProgressBar();
		this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationDump, DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected override void OnInteract()
	{
		this.PushSceneOperationTip();
		Item item = DolocAPI.SelectedItem;
		if (item is IWaterContainer waterContainer)
		{
			int result = waterContainer.TakeWater(func.Capacity - currentValue);
			if (result == 0)
			{
				return;
			}
			DolocAPI.agent._Interact(delegate
			{
				AddWater(result);
				UpdateProgressBar();
				DolocAPI.RaiseInstantAnimEffects(base.PositionTop, InstAnimEffectType.WATER_LARGE);
				DolocAPI.RefreshQuickInventorySelected();
			});
		}
		if (!(item?.name == DolocAPI.GlobalParameter.ItemRefBottleOfWater))
		{
			return;
		}
		if (currentValue >= func.Capacity)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrBarrelFull);
		}
		else
		{
			if (!DolocAPI.QueryItemProto(DolocAPI.GlobalParameter.ItemRefWastePlasticBottle, out var proto))
			{
				return;
			}
			DolocAPI.agent._Interact(delegate
			{
				int capacity = ((ItemFunctionBottle)proto.Function).Capacity;
				currentValue += capacity;
				if (currentValue > func.Capacity)
				{
					currentValue = func.Capacity;
				}
				DolocAPI.RaiseInstantAnimEffects(base.PositionTop, InstAnimEffectType.WATER_LITTLE);
				UpdateProgressBar();
				item?.CostSelf();
				string itemRefWastePlasticBottle = DolocAPI.GlobalParameter.ItemRefWastePlasticBottle;
				this.CreateDropItem(itemRefWastePlasticBottle, shouldRender: true, sendMessage: false);
			});
		}
	}

	protected override void OnDisTouch()
	{
		this.HideSceneOperationTip();
		if (!(base.Renderer == null))
		{
			base.Renderer.RemoveRenderComponent<ProgressBarRendererWater>();
		}
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveRenderComponent<ProgressBarRendererWater>();
	}

	private void UpdateProgressBar()
	{
		ProgressBarRendererWater renderComponent = base.Renderer.GetRenderComponent<ProgressBarRendererWater>();
		renderComponent.SetVisible(value: true);
		renderComponent.position2d = base.PositionTop;
		if (currentValue <= 0)
		{
			renderComponent.Progress = -1f;
		}
		else
		{
			renderComponent.Progress = (float)currentValue * reciprocalCapacity;
		}
	}

	public void Affect(IAffector affector)
	{
		if (affector.AffectType == AffectorType.Sprinkler)
		{
			currentValue++;
			DolocAPI.RaiseInstantAnimEffects(base.PositionTop, InstAnimEffectType.WATER_LITTLE);
			if (base.IsTouch)
			{
				UpdateProgressBar();
			}
		}
	}

	public void AffectNoRender(IAffector affector)
	{
		if (affector.AffectType == AffectorType.Sprinkler)
		{
			currentValue++;
		}
	}

	[DebugButton("水容器测试")]
	public void TestInterface()
	{
		Debug.Log($"是否为水容器:{this != null}");
	}
}
