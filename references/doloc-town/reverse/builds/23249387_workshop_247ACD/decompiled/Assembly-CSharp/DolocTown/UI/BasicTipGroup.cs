using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Settings;
using DolocTown.Config.Weather;
using UnityEngine;

namespace DolocTown.UI;

public class BasicTipGroup : DolocUiEntity
{
	private bool isEnabled;

	[SerializeField]
	private MoneyTip moneyTip;

	[SerializeField]
	private TimeTip timeTip;

	[SerializeField]
	private PositionTip positionTip;

	[SerializeField]
	private ElectricityInfoViewer electricityTip;

	[SerializeField]
	private MissionTipManager missionTips;

	[SerializeField]
	private AgentStatusTip agentStatusTip;

	[SerializeField]
	private BuffTip buffTip;

	[SerializeField]
	private MenuTip menuTip;

	[SerializeField]
	private GameVersionTip versionTip;

	[SerializeField]
	private AgentCellTip agentCellTip;

	[SerializeField]
	public OperationGuidanceManager guidanceTips;

	[SerializeField]
	private SimpleGroup[] groups;

	private List<DolocBasicTip> allTips = new List<DolocBasicTip>();

	private Dictionary<DolocBasicTip, UserSettingType> objToSettingTypes = new Dictionary<DolocBasicTip, UserSettingType>();

	private CanvasGroup canvasGroup;

	private bool allowInteracte = true;

	public MoneyTip MoneyTip => moneyTip;

	public TimeTip TimeTip => timeTip;

	public ElectricityInfoViewer ElectricityTip => electricityTip;

	public MissionTipManager MissionTips => missionTips;

	public AgentStatusTip AgentStatusBar => agentStatusTip;

	public BuffTip BuffTip => buffTip;

	public AgentCellTip AgentCellTip => agentCellTip;

	public float alpha
	{
		set
		{
			canvasGroup.alpha = value;
		}
	}

	public string PositionText
	{
		get
		{
			return positionTip.PositionText;
		}
		set
		{
			positionTip.PositionText = value;
		}
	}

	private bool ShouldShowElectricPanel
	{
		get
		{
			if (!DolocAPI.IsDataLoaded || DolocAPI.CurrentRoom == null)
			{
				return false;
			}
			if (DolocAPI.CurrentRoom.Type != RoomType.Farm)
			{
				return false;
			}
			return DolocAPI.CurrentRoom.DM_electric.ShouldShowElectricPanel;
		}
	}

	public UserSettingType[] allSettings => objToSettingTypes.Values.ToArray();

	public void RefreshPosition()
	{
		PositionText = DolocAPI.archiveHandle?.currentSceneTitle ?? "";
	}

	private void RegisterTipComponent(DolocBasicTip tipCmp, UserSettingType settingType)
	{
		allTips.Add(tipCmp);
		objToSettingTypes.Add(tipCmp, settingType);
	}

	protected override void __Init()
	{
		base.__Init();
		if (!TryGetComponent<CanvasGroup>(out canvasGroup))
		{
			canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
		}
		guidanceTips.Init();
		RegisterTipComponent(moneyTip, UserSettingType.TIP_MONEY);
		RegisterTipComponent(timeTip, UserSettingType.TIP_TIME);
		RegisterTipComponent(positionTip, UserSettingType.TIP_POSITION);
		RegisterTipComponent(electricityTip, UserSettingType.TIP_ELECTRIC_INFO);
		RegisterTipComponent(missionTips, UserSettingType.TIP_MISSION);
		RegisterTipComponent(agentStatusTip, UserSettingType.TIP_AGENT_STATUS);
		RegisterTipComponent(buffTip, UserSettingType.TIP_BUFF);
		RegisterTipComponent(agentCellTip, UserSettingType.TIP_INTERACT_CELL);
		RegisterTipComponent(versionTip, UserSettingType.TIP_VERSION);
		RegisterTipComponent(menuTip, UserSettingType.TIP_MENU);
		foreach (DolocBasicTip tip in allTips)
		{
			tip.Init();
			if (!objToSettingTypes.TryGetValue(tip, out var value))
			{
				break;
			}
			DolocAPI.RegisterMsgListener(value, delegate(object _, GameEventArgs args)
			{
				bool flag = (bool)((GameEventArgs<object>)args).value;
				tip.showBySetting = flag;
				if (DolocAPI.IsDataLoaded)
				{
					tip.SetVisible(flag);
				}
			});
		}
	}

	public void Show()
	{
		if (isEnabled)
		{
			return;
		}
		isEnabled = true;
		if (!DolocAPI.IsDataLoaded)
		{
			return;
		}
		foreach (DolocBasicTip allTip in allTips)
		{
			allTip.ResetCanvasGroup();
			allTip.SetVisible(value: true);
		}
		canvasGroup.alpha = 1f;
		canvasGroup.interactable = allowInteracte;
		canvasGroup.blocksRaycasts = allowInteracte;
		SimpleGroup[] array = groups;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateVisibleState();
		}
		RefreshAll();
		SetVisible(value: true);
	}

	public void RefreshAll()
	{
		timeTip.UpdateTimeInfo();
		moneyTip.SetMoney(DolocAPI.archiveHandle.CurrentMoney, useAnimation: false);
		UpdateWeatherTipVisibleState();
		UpdateElectricityInfoViewer();
		electricityTip.UpdateInfo();
		agentStatusTip.UpdateAllInfo();
	}

	public void Hide()
	{
		if (isEnabled)
		{
			isEnabled = false;
			canvasGroup.alpha = 0f;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
		}
	}

	public void EnableInteract(bool value)
	{
		allowInteracte = value;
		if (isEnabled)
		{
			canvasGroup.interactable = value;
			canvasGroup.blocksRaycasts = value;
		}
	}

	public void ResetBasicTipState()
	{
		Hide();
		foreach (DolocBasicTip allTip in allTips)
		{
			UserSettingType userSettingType = objToSettingTypes[allTip];
			allTip.SetVisible(allTip.showBySetting = DolocAPI.userSettings.GetOrDefault<bool>(userSettingType));
		}
	}

	public void UpdateWeatherTipVisibleState()
	{
	}

	public void UpdateElectricityInfoViewer()
	{
		electricityTip.SetVisible(ShouldShowElectricPanel);
	}

	public void UpdatePerSecond()
	{
		electricityTip.UpdateInfo();
	}

	public void UpdatePerTU()
	{
		timeTip.UpdateTimeInfo(needRefresh: false);
		agentStatusTip.UpdateAllInfo();
	}

	public void UpdateTimeInfo()
	{
		timeTip.UpdateTimeInfo();
	}

	public void OnWeatherChanged(WeatherType type)
	{
		agentStatusTip.SetCorrosionBarVisible(type == WeatherType.ACID_RAIN);
	}

	public void OnEnterRoom(Room room)
	{
		electricityTip.BuildHealthVisible = false;
		electricityTip.SetVisible(room.Type == RoomType.Farm);
		if (room is TemplateRoomInHouse templateRoomInHouse)
		{
			electricityTip.BuildHealthVisible = true;
			electricityTip.SetBuildHealth(templateRoomInHouse.Health, templateRoomInHouse.MaxHealth);
		}
	}

	public void ResetUIState()
	{
		buffTip.Clear();
		missionTips.Clear();
		guidanceTips.Clear();
	}
}
