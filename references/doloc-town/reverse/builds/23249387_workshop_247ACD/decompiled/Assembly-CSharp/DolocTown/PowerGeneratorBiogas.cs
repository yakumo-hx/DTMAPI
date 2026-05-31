using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class PowerGeneratorBiogas : PowerGenerator
{
	private readonly EquipmentFuncPowerGeneratorBiogas func;

	private readonly ElectronicComponentGeneratorFuel fuelGenerator;

	public override bool IsFuelGenerator => true;

	public override float FuelPercent => fuelGenerator.FuelPercent;

	public PowerGeneratorBiogas(IEquipmentHost host, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, id, proto, wp, anchor, turn)
	{
		fuelGenerator = (ElectronicComponentGeneratorFuel)electronicComponent;
	}

	public PowerGeneratorBiogas(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			fuelGenerator = (ElectronicComponentGeneratorFuel)electronicComponent;
		}
	}

	public override bool IsSuitableFuel(Item fuelItem)
	{
		return fuelItem?.name == DolocAPI.GlobalParameter.ItemRefFaeces;
	}

	public override void AddFuel(ItemInfo fuelItem, bool sendMessage = false)
	{
		if (fuelItem != null && !(fuelItem.Id != DolocAPI.GlobalParameter.ItemRefFaeces))
		{
			fuelGenerator.AddFuel(fuelItem.ElectricEnergy);
			if (base.IsRender)
			{
				DolocAPI.RaiseSpriteFadeUp(base.PositionCenter, fuelItem.UiSpriteAsset.Asset);
				UpdateProgressBar();
			}
			if (sendMessage)
			{
				SendUseEquipmentMessage();
			}
		}
	}

	private void ShowProgressBar()
	{
		ProgressBarRenderer renderComponent = base.Renderer.GetRenderComponent<ProgressBarRenderer>();
		if (!(renderComponent == null))
		{
			renderComponent.position = base.PositionCenter;
			renderComponent.Progress = fuelGenerator.FuelPercent;
			if (!renderComponent.isVisible)
			{
				renderComponent.Show();
			}
		}
	}

	private void UpdateProgressBar()
	{
		base.Renderer.HandleComponentIfExist(delegate(ProgressBarRenderer bar)
		{
			bar.Progress = fuelGenerator.FuelPercent;
		});
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationFuelIn);
		ShowProgressBar();
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
		base.Renderer.HandleComponentIfExist(delegate(ProgressBarRenderer bar)
		{
			bar.Hide();
		});
	}

	protected override void OnInteract()
	{
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem.name != DolocAPI.GlobalParameter.ItemRefFaeces)
		{
			this.PushSceneOperationTip();
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrFuel);
			return;
		}
		this.PushSceneOperationTip();
		selectedItem.CostSelf();
		fuelGenerator.AddFuel(func.Efficiency);
		UpdateProgressBar();
	}
}
