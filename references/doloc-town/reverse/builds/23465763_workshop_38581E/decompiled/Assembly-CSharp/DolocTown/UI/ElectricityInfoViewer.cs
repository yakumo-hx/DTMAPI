using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ElectricityInfoViewer : DolocBasicTip
{
	[SerializeField]
	private Text buildHealth;

	[SerializeField]
	private ElectricityProgressBar generatorInfo;

	[SerializeField]
	private ElectricityProgressBar batteryInfo;

	[SerializeField]
	private Text supportInfo;

	[SerializeField]
	private Color excceedColor;

	[SerializeField]
	private Color lossColor;

	[SerializeField]
	private Sprite excceedIcon;

	[SerializeField]
	private Sprite lossIcon;

	public bool BuildHealthVisible
	{
		set
		{
			buildHealth.gameObject.SetActive(value);
		}
	}

	public void SetBuildHealth(float health, float total)
	{
		buildHealth.text = DolocUtils.Format(DolocConfig.StaticTexts.UiTipBuildHealth, $"{(int)health}/{total}");
	}

	public override void SetVisible(bool value)
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null || currentRoom.Type != RoomType.Farm)
		{
			base.SetVisible(value: false);
		}
		else
		{
			base.SetVisible(value && DolocAPI.CurrentRoom.DM_electric.ShouldShowElectricPanel);
		}
	}

	private Color GetColor(bool exceed)
	{
		if (!exceed)
		{
			return lossColor;
		}
		return excceedColor;
	}

	private Sprite GetIcon(bool exceed)
	{
		if (!exceed)
		{
			return lossIcon;
		}
		return excceedIcon;
	}

	private string GetStatusInfo(ElectricSystemStatus status)
	{
		return status switch
		{
			ElectricSystemStatus.None => DolocConfig.StaticTexts.UiTipElectricityStatusNone, 
			ElectricSystemStatus.LackOfGeneration => DolocConfig.StaticTexts.UiTipElectricityStatusLackOfGeneration, 
			ElectricSystemStatus.LackOfGenerationBatteryCost => DolocConfig.StaticTexts.UiTipElectricityStatusLackOfGenerationCostBattery, 
			ElectricSystemStatus.BatterySaving => DolocConfig.StaticTexts.UiTipElectricityStatusBatterySaving, 
			ElectricSystemStatus.BatteryFull => DolocConfig.StaticTexts.UiTipElectricityStatusBatteryFull, 
			ElectricSystemStatus.PowerLoss => DolocConfig.StaticTexts.UiTipElectricityStatusPowerLoss, 
			_ => string.Empty, 
		};
	}

	public void UpdateInfo()
	{
		IEquipmentHost currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom != null)
		{
			ElectricSystem dM_electric = currentRoom.DM_electric;
			float totalPowerGeneration = dM_electric.TotalPowerGeneration;
			float totalPowerConsumption = dM_electric.TotalPowerConsumption;
			float totalBatteryPower = dM_electric.TotalBatteryPower;
			float totalBatteryPowerCapacity = dM_electric.TotalBatteryPowerCapacity;
			generatorInfo.Text = DolocConfig.StaticTexts.UiTipElectricityGeneratorInfo.Format(totalPowerGeneration, totalPowerConsumption);
			float num = totalPowerGeneration + totalPowerConsumption;
			float num2 = totalPowerGeneration - totalPowerConsumption;
			generatorInfo.Progress = Mathf.Abs(num2) / num;
			bool exceed = num2 >= 0f;
			generatorInfo.ProgressColor = GetColor(exceed);
			generatorInfo.Icon = GetIcon(exceed);
			generatorInfo.IconColor = ((num2 == 0f) ? Color.clear : generatorInfo.ProgressColor);
			batteryInfo.SetVisible(dM_electric.HasBattery);
			if (batteryInfo.isVisible)
			{
				batteryInfo.Text = DolocConfig.StaticTexts.UiTipElectricityBatteryInfo.Format(totalBatteryPower, totalBatteryPowerCapacity);
				batteryInfo.Progress = totalBatteryPower / totalBatteryPowerCapacity;
				bool isBatteryCharingNow = dM_electric.IsBatteryCharingNow;
				batteryInfo.IconColor = (dM_electric.IsBatteryStayNow ? Color.clear : GetColor(dM_electric.IsBatteryCharingNow));
				batteryInfo.Icon = GetIcon(isBatteryCharingNow);
			}
			supportInfo.text = GetStatusInfo(dM_electric.LastStatus);
			if (DolocAPI.CurrentRoom is TemplateRoomInHouse templateRoomInHouse)
			{
				SetBuildHealth(templateRoomInHouse.Health, templateRoomInHouse.MaxHealth);
			}
		}
	}
}
