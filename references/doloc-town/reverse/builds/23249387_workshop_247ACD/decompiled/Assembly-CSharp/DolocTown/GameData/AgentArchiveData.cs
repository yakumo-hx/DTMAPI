using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Buff;
using DolocTown.Config.Settings;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class AgentArchiveData
{
	[JsonProperty("maxHealth")]
	public int maxHealth;

	[JsonProperty("maxEnergy")]
	public int maxEnergy;

	[JsonProperty("maxSpirit")]
	public int maxSpirit;

	[JsonProperty("energy")]
	public int energy;

	[JsonProperty("health")]
	public int health;

	[JsonProperty("spirit")]
	public int spirit;

	[JsonProperty("overflow_health")]
	public int overflowHealth;

	[JsonProperty("overflow_energy")]
	public int overflowEnergy;

	[JsonProperty("spiritEnabled")]
	public bool spiritEnabled;

	[JsonProperty("money")]
	public int money;

	[JsonProperty]
	public bool disableDisposeItem;

	[JsonProperty]
	public bool hasComeoutAfterSleep;

	[JsonProperty]
	public bool isDashUnlocked;

	[JsonProperty]
	public bool isDoubleJumpUnlocked;

	[JsonProperty]
	public BuffManager buffManager;

	[JsonProperty]
	public bool unfoldMissionTips;

	[JsonProperty]
	public bool autoBattle;

	[JsonProperty]
	public int backpackLevel;

	[JsonProperty]
	public int farmLevel;

	[JsonProperty]
	public readonly AgentEquipmentManager agentEquipment;

	[JsonProperty]
	public readonly MotorDataManager motorData;

	[JsonProperty]
	public bool hasShoweredToday;

	[JsonProperty]
	private int tuAfterWakeUp;

	[JsonProperty]
	public string customPlayerName;

	[JsonProperty]
	private string backupBackpackJson;

	[JsonProperty]
	private string backupEquipmentJson;

	[JsonProperty]
	private Counter corrosionCounter;

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	public Vector2Int birthday;

	private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.Auto,
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore
	};

	private HashSet<int> promptedSpiritThreshold = new HashSet<int>();

	[JsonProperty("agentPosition")]
	[JsonConverter(typeof(VectorConverter))]
	private Vector3 AgentPosition => DolocAPI.AgentPosition;

	public string playerName
	{
		get
		{
			if (!string.IsNullOrEmpty(customPlayerName))
			{
				return customPlayerName;
			}
			return globalParameter.PlayerDefaultName;
		}
		set
		{
			customPlayerName = ((value == globalParameter.PlayerDefaultName) ? "" : value);
		}
	}

	public int MaxHealth => maxHealth;

	public int MaxEnergy => maxEnergy;

	public int MaxSpirit => maxSpirit;

	public int HealthGap => MaxHealth - health;

	public int EnergyGap => MaxEnergy - energy;

	public int TotalEnergy => energy + overflowEnergy;

	private GlobalParameterInfo globalParameter => DolocAPI.GlobalParameter;

	public float CurrentSpiritPercent => (float)spirit / (float)MaxSpirit;

	public Vector3 _agentPosition { get; private set; }

	public float CorrosionProcess => corrosionCounter.Process;

	public AgentArchiveData()
	{
		customPlayerName = "";
		maxEnergy = globalParameter.InitEnergy;
		maxHealth = globalParameter.InitHealth;
		maxSpirit = globalParameter.InitSpirit;
		health = MaxHealth;
		energy = MaxEnergy;
		spirit = MaxSpirit;
		overflowHealth = 0;
		overflowEnergy = 0;
		spiritEnabled = true;
		money = 0;
		disableDisposeItem = false;
		hasComeoutAfterSleep = true;
		isDashUnlocked = true;
		isDoubleJumpUnlocked = true;
		hasShoweredToday = false;
		buffManager = new BuffManager();
		unfoldMissionTips = true;
		autoBattle = false;
		backpackLevel = 0;
		farmLevel = 0;
		agentEquipment = new AgentEquipmentManager();
		motorData = new MotorDataManager();
		birthday = DolocAPI.GlobalParameter.PlayerDefaultBirthday;
		corrosionCounter = new Counter(DolocAPI.GlobalParameter.CorrosionCounterLength);
	}

	[JsonConstructor]
	public AgentArchiveData(string customPlayerName, Vector2Int birthday, Vector3 agentPosition, int maxHealth, int maxEnergy, int maxSpirit, int energy, int health, int spirit, int overflowHealth, int overflowEnergy, bool spiritEnabled, int money, bool disableDisposeItem, bool hasComeoutAfterSleep, int tuAfterWakeUp, bool isDashUnlocked, bool isDoubleJumpUnlocked, BuffManager buffManager, bool unfoldMissionTips, bool autoBattle, int garbageShredderCarLevel, int backpackLevel, int farmLevel, bool hasShoweredToday, AgentEquipmentManager agentEquipment = null, MotorDataManager motorData = null, string backupBackpackJson = null, string backupEquipmentJson = null, Counter corrosionCounter = null)
	{
		this.customPlayerName = customPlayerName;
		this.birthday = birthday;
		_agentPosition = agentPosition;
		this.maxHealth = maxHealth;
		this.maxEnergy = maxEnergy;
		this.maxSpirit = maxSpirit;
		_ValidateAgentStatus();
		this.energy = Mathf.Min(maxEnergy, energy);
		this.health = Mathf.Min(maxHealth, health);
		this.spirit = Mathf.Min(maxSpirit, spirit);
		this.overflowHealth = overflowHealth;
		this.overflowEnergy = overflowEnergy;
		this.spiritEnabled = spiritEnabled;
		this.money = money;
		this.disableDisposeItem = disableDisposeItem;
		this.hasComeoutAfterSleep = hasComeoutAfterSleep;
		this.isDashUnlocked = isDashUnlocked;
		this.isDoubleJumpUnlocked = isDoubleJumpUnlocked;
		this.hasShoweredToday = hasShoweredToday;
		this.buffManager = buffManager;
		this.unfoldMissionTips = unfoldMissionTips;
		this.autoBattle = autoBattle;
		this.backpackLevel = backpackLevel;
		this.farmLevel = farmLevel;
		this.agentEquipment = agentEquipment ?? new AgentEquipmentManager();
		this.motorData = motorData ?? new MotorDataManager();
		this.backupBackpackJson = backupBackpackJson;
		this.backupEquipmentJson = backupEquipmentJson;
		this.corrosionCounter = corrosionCounter ?? new Counter(DolocAPI.GlobalParameter.CorrosionCounterLength);
		this.corrosionCounter.ValidateInterval(DolocAPI.GlobalParameter.CorrosionCounterLength);
	}

	public void AfterLoadData()
	{
		agentEquipment.AfterLoadData();
		motorData.AfterLoadData();
	}

	public void CorrodeAgent(WeatherType weatherType, bool shouldRender)
	{
		if (weatherType != WeatherType.ACID_RAIN)
		{
			corrosionCounter.DownTickNoLoop();
			return;
		}
		Room currentRoom = DolocAPI.archiveHandle.currentRoom;
		if (currentRoom == null)
		{
			return;
		}
		if (currentRoom.AffectedByMalignantWeather && !agentEquipment.EquipmentAbility.immuneAcidRain && !DolocAPI.AbilitySystem.stateAbility.ImmuneAcidRain)
		{
			if (corrosionCounter.Tick() && shouldRender)
			{
				DolocAPI.agent.OnNatureElementAttacked(DolocAPI.AgentPosition, DolocAPI.GlobalParameter.CorrosionHealthCost);
			}
		}
		else
		{
			corrosionCounter.DownTickNoLoop();
		}
	}

	public void BackupBackpack()
	{
		if (!backupBackpackJson.IsNullOrEmpty())
		{
			Debug.LogError("背包备份未恢复，禁止重复备份");
		}
		else
		{
			backupBackpackJson = JsonConvert.SerializeObject(DolocAPI.archiveHandle.InventorySystem.inventory, Formatting.None, jsonSettings);
		}
	}

	public void RevertBackpack()
	{
		try
		{
			LinearInventory other = JsonConvert.DeserializeObject<LinearInventory>(backupBackpackJson, jsonSettings);
			DolocAPI.archiveHandle.InventorySystem.inventory.Overwrite(other, shouldEmit: true);
			DolocAPI.archiveHandle.ValidateBackpackCapacity();
			backupBackpackJson = null;
		}
		catch (Exception exception)
		{
			Debug.LogError("恢复背包失败！");
			Debug.LogException(exception);
		}
	}

	public void BackupEquipment()
	{
		if (!backupEquipmentJson.IsNullOrEmpty())
		{
			Debug.LogError("装备栏备份未恢复，禁止重复备份");
		}
		else
		{
			backupEquipmentJson = JsonConvert.SerializeObject(agentEquipment, Formatting.None, jsonSettings);
		}
	}

	public void RevertEquipment()
	{
		try
		{
			AgentEquipmentManager other = JsonConvert.DeserializeObject<AgentEquipmentManager>(backupEquipmentJson, jsonSettings);
			DolocAPI.archiveHandle.farmData.agentData.agentEquipment.OverwriteAll(other);
			backupEquipmentJson = null;
		}
		catch (Exception exception)
		{
			Debug.LogError("恢复装备栏失败！");
			Debug.LogException(exception);
		}
	}

	private void _ValidateAgentStatus()
	{
		if (maxEnergy <= 0)
		{
			maxEnergy = globalParameter.InitEnergy;
		}
		if (maxHealth <= 0)
		{
			maxHealth = globalParameter.InitHealth;
		}
		if (maxSpirit <= 0)
		{
			maxSpirit = globalParameter.InitSpirit;
		}
	}

	public bool UpgradeSpirit(int value)
	{
		if (value <= 0)
		{
			return false;
		}
		maxSpirit += value;
		spirit = maxSpirit;
		return true;
	}

	public bool UpgradeEnergy(int value)
	{
		if (value <= 0)
		{
			return false;
		}
		maxEnergy += value;
		energy = maxEnergy;
		return true;
	}

	public bool UpgradeHealth(int value)
	{
		if (value <= 0)
		{
			return false;
		}
		maxHealth += value;
		health = maxHealth;
		return true;
	}

	public void UpdatePerTU(WeatherType weatherType)
	{
		_UpdateSpirit();
		buffManager.UpdatePerTU();
		agentEquipment.UpdatePerTu();
		CorrodeAgent(weatherType, shouldRender: true);
	}

	public void UpdatePerTUNoRender(WeatherType weatherType)
	{
		buffManager.UpdatePerTUNoRender();
		agentEquipment.UpdatePerTuNoRender();
		CorrodeAgent(weatherType, shouldRender: false);
	}

	private void _UpdateSpirit()
	{
		if (spiritEnabled && !DolocAPI.gameManager.gameInitConfig.ignoreSpiritCost)
		{
			spirit--;
			HandleSpiritChange(spirit);
			TickTuAfterAwake();
			if (spirit <= 0)
			{
				new FaintGameState(DolocAPI.userInput, FaintReason.Tired).Startup();
			}
		}
	}

	private void HandleSpiritChange(int spirit)
	{
		foreach (SpiritThresholdInfo data in DolocConfig.Tables.TbSpiritThreshold.DataList)
		{
			if (!promptedSpiritThreshold.Contains(data.SpiritThreshold) && spirit <= data.SpiritThreshold)
			{
				promptedSpiritThreshold.Add(data.SpiritThreshold);
				DolocAPI.RaiseEmotion(DolocAPI.AgentTransform, EmotionName.TIRED);
				DolocAPI.ShowMessageBoxSmallErr(data.SmallInfo);
				DolocAPI.ShowMessageBoxInfo(data.LargeInfo);
				break;
			}
		}
	}

	private void TickTuAfterAwake()
	{
		tuAfterWakeUp++;
		for (int num = DolocConfig.Tables.TbRecoveryDecay.DataList.Count - 1; num >= 0; num--)
		{
			RecoveryDecayInfo recoveryDecayInfo = DolocConfig.Tables.TbRecoveryDecay.DataList[num];
			if (tuAfterWakeUp >= recoveryDecayInfo.TimeSinceAwake)
			{
				if (!DolocAPI.HasBuff(recoveryDecayInfo.BuffId))
				{
					ClearRecoveryDecayBuffs();
					buffManager.Add(recoveryDecayInfo.BuffId);
				}
				break;
			}
		}
	}

	public void ClearRecoveryDecayBuffs()
	{
		foreach (RecoveryDecayInfo data in DolocConfig.Tables.TbRecoveryDecay.DataList)
		{
			buffManager.Remove(data.BuffId);
		}
	}

	public void OnSpiritReset()
	{
		promptedSpiritThreshold.Clear();
		tuAfterWakeUp = 0;
		ClearRecoveryDecayBuffs();
	}

	public void ResetHealthAndEnergy()
	{
		health = MaxHealth;
		energy = MaxEnergy;
	}

	public void ResetSpirit(float percent)
	{
		int b = Mathf.Clamp(Mathf.FloorToInt((float)MaxSpirit * percent), 0, MaxSpirit);
		spirit = Mathf.Max(spirit, b);
		OnSpiritReset();
	}

	public void AddSpirit(int value)
	{
		spirit = Mathf.Clamp(spirit + value, 0, MaxSpirit);
		promptedSpiritThreshold.Clear();
	}

	public void ResetSpirit()
	{
		spirit = MaxSpirit;
		OnSpiritReset();
	}

	public void DailyRefresh()
	{
		hasShoweredToday = false;
	}

	public bool CheckHasShoweredToday()
	{
		if (hasShoweredToday)
		{
			return true;
		}
		hasShoweredToday = true;
		return false;
	}
}
