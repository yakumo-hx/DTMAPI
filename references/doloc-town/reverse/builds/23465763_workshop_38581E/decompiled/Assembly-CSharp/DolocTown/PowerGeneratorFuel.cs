using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class PowerGeneratorFuel : PowerGenerator
{
	private readonly ElectronicComponentGeneratorFuel generatorFuel;

	private bool hasMoreFuel;

	public override bool IsFuelGenerator => true;

	public override bool IsDirty => generatorFuel.Fuel > 0f;

	public override bool CanInteractContinues
	{
		get
		{
			if (CheckCurrentItemCanInteract())
			{
				return !generatorFuel.IsFull;
			}
			return false;
		}
	}

	private Vector3 PositionBar
	{
		get
		{
			Vector3 positionCenter = base.PositionCenter;
			positionCenter.z = -6f;
			return positionCenter;
		}
	}

	public override float FuelPercent => generatorFuel.FuelPercent;

	public PowerGeneratorFuel(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		generatorFuel = (ElectronicComponentGeneratorFuel)electronicComponent;
	}

	[JsonConstructor]
	protected PowerGeneratorFuel(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		generatorFuel = (ElectronicComponentGeneratorFuel)electronicComponent;
	}

	protected override void OnInteract()
	{
		Item item = DolocAPI.SelectedItem;
		if (item == null)
		{
			PushTip();
			return;
		}
		if (item.proto.ElectricEnergy == 0)
		{
			PushTip();
			DolocAPI.ShowMessageBoxSmall(DolocConfig.StaticTexts.UiOperationErrFuel);
			return;
		}
		if (generatorFuel.IsFull)
		{
			PushTip();
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrFuelFull);
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			PushTip();
			if (item.CostSelf(out var _, showFadeUpIcon: false))
			{
				DolocAPI.RaiseSpriteFadeUp(base.PositionCenter, item.uiSprite);
				DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.LIGHT_SMOKE);
				generatorFuel.AddFuel(item.proto.ElectricEnergy);
				base.Renderer.UpdateProgressBarRenderer(generatorFuel.FuelPercent, base.PositionCenter);
				base.Renderer.SetStateRendererStatus(state: true, base.PositionCenter);
				hasMoreFuel = true;
				SendUseEquipmentMessage();
			}
		});
	}

	public override bool IsSuitableFuel(Item fuelItem)
	{
		if (fuelItem != null && fuelItem.proto.ElectricEnergy > 0)
		{
			return fuelItem.name != DolocAPI.GlobalParameter.ItemRefFaeces;
		}
		return false;
	}

	public override void AddFuel(ItemInfo fuelItem, bool sendMessage = false)
	{
		if (fuelItem != null)
		{
			generatorFuel.AddFuel(fuelItem.ElectricEnergy);
			if (base.IsRender)
			{
				DolocAPI.RaiseSpriteFadeUp(base.PositionCenter, fuelItem.UiSpriteAsset.Asset);
				DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.LIGHT_SMOKE);
				base.Renderer.UpdateProgressBarRendererOnlyVisible(generatorFuel.FuelPercent, base.PositionCenter);
				base.Renderer.SetStateRendererStatus(state: true, base.PositionCenter);
			}
			hasMoreFuel = true;
			if (sendMessage)
			{
				SendUseEquipmentMessage();
			}
		}
	}

	private bool CheckCurrentItemCanInteract()
	{
		return IsSuitableFuel(DolocAPI.SelectedItem);
	}

	protected override void Update()
	{
		if (base.IsTouch)
		{
			base.Renderer.UpdateProgressBarRenderer(generatorFuel.FuelPercent);
		}
		if (hasMoreFuel)
		{
			if (generatorFuel.Fuel <= 0f)
			{
				hasMoreFuel = false;
				base.Renderer.HideStateRenderer();
			}
		}
		else if (generatorFuel.Fuel > 0f)
		{
			hasMoreFuel = true;
			base.Renderer.SetStateRendererStatus(state: true);
		}
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationFuelIn);
		base.Renderer.UpdateProgressBarRenderer(generatorFuel.FuelPercent, PositionBar);
	}

	protected override void OnDisTouch()
	{
		HideTip();
		base.Renderer.RemoveProgressBarRenderer();
	}

	protected override void OnRender()
	{
		hasMoreFuel = generatorFuel.Fuel > 0f;
		if (hasMoreFuel)
		{
			base.Renderer.SetStateRendererStatus(state: true, base.PositionCenter);
		}
		else
		{
			base.Renderer.HideStateRenderer();
		}
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveStateRenderer();
		base.Renderer.RemoveProgressBarRenderer();
	}

	protected override void OnGeneratorChangeState(bool value)
	{
		if (base.Renderer != null)
		{
			base.Renderer.RemoveProgressBarRenderer();
		}
	}
}
