using DolocTown.Config;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public class AgentStatusTip : DolocBasicTip
{
	[SerializeField]
	private AgentStatusBar healthBar;

	[SerializeField]
	private AgentStatusBar energyBar;

	[SerializeField]
	private AgentCorrosionBar corrosionBar;

	[SerializeField]
	private ProgressCircle spiritProgressBar;

	private AgentArchiveData agentData => DolocAPI.archiveHandle.farmData.agentData;

	private float healthPercent
	{
		set
		{
			healthBar.Value = Mathf.Clamp01(value);
		}
	}

	private float overflowHealthPercent
	{
		set
		{
			healthBar.OverflowValue = Mathf.Clamp01(value);
		}
	}

	private float energyPercent
	{
		set
		{
			energyBar.Value = Mathf.Clamp01(value);
		}
	}

	private float overflowEnergyPercent
	{
		set
		{
			energyBar.OverflowValue = Mathf.Clamp01(value);
		}
	}

	private float spiritPercent
	{
		get
		{
			return spiritProgressBar.Progress;
		}
		set
		{
			spiritProgressBar.Progress = value;
		}
	}

	public float corrosionValue
	{
		set
		{
			corrosionBar.Value = Mathf.Clamp(value, 0.01f, 1f);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		healthBar.Init();
		DolocButtonComponent healthBtn = healthBar.gameObject.GetOrCreateButton();
		healthBtn.onPointerEnter.AddListener(delegate
		{
			Color color2 = (((float)agentData.health / (float)agentData.MaxHealth < 0.1f) ? DolocColor.drakRed : DolocColor.green);
			string text3 = DolocUtils.Format(DolocConfig.StaticTexts.UiTipHealthValue, agentData.health.ToString().Colored(color2), agentData.MaxHealth);
			healthBtn.GetComponent<RectTransform>().HoverTextSmall(text3, UIAlignmentType.LeftBottom, UIAlignmentType.LeftTop);
		});
		healthBtn.onPointerExit.AddListener(this.HideHoverBox);
		energyBar.Init();
		DolocButtonComponent energyBtn = energyBar.gameObject.GetOrCreateButton();
		energyBtn.onPointerEnter.AddListener(delegate
		{
			Color color = (((float)agentData.energy / (float)agentData.MaxEnergy < 0.1f) ? DolocColor.drakRed : DolocColor.blue);
			string text2 = DolocUtils.Format(DolocConfig.StaticTexts.UiTipEnergyValue, agentData.energy.ToString().Colored(color), agentData.MaxEnergy);
			energyBtn.GetComponent<RectTransform>().HoverTextSmall(text2, UIAlignmentType.LeftBottom, UIAlignmentType.LeftTop);
		});
		energyBtn.onPointerExit.AddListener(this.HideHoverBox);
		corrosionBar.Init();
		DolocButtonComponent corrosionBtn = corrosionBar.gameObject.GetOrCreateButton();
		corrosionBtn.onPointerEnter.AddListener(delegate
		{
			corrosionBtn.GetComponent<RectTransform>().HoverTextSmall(DolocConfig.StaticTexts.UiTipCorrosionValue, UIAlignmentType.LeftBottom, UIAlignmentType.LeftTop);
		});
		corrosionBtn.onPointerExit.AddListener(this.HideHoverBox);
		spiritProgressBar.Init();
		DolocButtonComponent spiritBtn = spiritProgressBar.gameObject.GetOrCreateButton();
		spiritBtn.onPointerEnter.AddListener(delegate
		{
			RectTransform component = spiritBtn.GetComponent<RectTransform>();
			float currentSpiritPercent = agentData.CurrentSpiritPercent;
			string text = ((currentSpiritPercent >= 0.25f) ? ((!(currentSpiritPercent >= 0.5f)) ? DolocConfig.StaticTexts.UiTipCurrentSpiritLe25 : DolocConfig.StaticTexts.UiTipCurrentSpiritLe50) : ((!(currentSpiritPercent >= 0.1f)) ? DolocConfig.StaticTexts.UiTipCurrentSpiritLe0 : DolocConfig.StaticTexts.UiTipCurrentSpiritLe10));
			component.HoverTextSmall(text, UIAlignmentType.LeftBottom, UIAlignmentType.LeftTop);
		});
		spiritBtn.onPointerExit.AddListener(this.HideHoverBox);
	}

	public void UpdateAllInfo()
	{
		UpdateHealth();
		UpdateEnergy();
		UpdateSpirit();
		SetCorrosionBarVisible(DolocAPI.archiveHandle.WeatherSystem.WeatherType == WeatherType.ACID_RAIN);
	}

	public void UpdateHealth()
	{
		healthPercent = DolocAPI.archiveHandle.CurrentHealthPercent;
		overflowHealthPercent = DolocAPI.archiveHandle.CurrentOverflowHealthPercent;
	}

	public void UpdateEnergy()
	{
		energyPercent = DolocAPI.archiveHandle.CurrentEnergyPercent;
		overflowEnergyPercent = DolocAPI.archiveHandle.CurrentOverflowEnergyPercent;
	}

	public void UpdateSpirit()
	{
		spiritPercent = DolocAPI.archiveHandle.CurrentSpiritPercent;
	}

	public void SetCorrosionBarVisible(bool visible)
	{
		UpdateCorrosionProgress();
		corrosionBar.SetVisible(visible);
	}

	private void UpdateCorrosionProgress()
	{
		corrosionValue = DolocAPI.archiveHandle.farmData.agentData.CorrosionProcess;
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		spiritProgressBar.gameObject.SetActive(base.gameObject.activeSelf);
	}
}
