using DolocTown.Config.TechTree;
using UnityEngine;

namespace DolocTown.GameData;

public static class ArchiveOperationGlobal
{
	public static bool CouldDash(this ArchiveDataHandle handle)
	{
		if (!DolocAPI.gameManager.gameInitConfig.shouldCoverMoveAbilityUnlock)
		{
			return handle.farmData.agentData.isDashUnlocked;
		}
		return true;
	}

	public static bool IsDashUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.agentData.isDashUnlocked;
	}

	public static void LockDash(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.isDashUnlocked = false;
	}

	public static void UnlockDash(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.isDashUnlocked = true;
	}

	public static int DoubleJumpTimes(this ArchiveDataHandle handle)
	{
		if (!DolocAPI.gameManager.gameInitConfig.shouldCoverMoveAbilityUnlock && !handle.farmData.agentData.isDoubleJumpUnlocked)
		{
			return 1;
		}
		return 2;
	}

	public static bool IsDoubleJumpUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.agentData.isDoubleJumpUnlocked;
	}

	public static void LockDoubleJump(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.isDoubleJumpUnlocked = false;
	}

	public static void UnlockDoubleJump(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.isDoubleJumpUnlocked = true;
	}

	public static void ResetPlayerValues(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.ResetHealthAndEnergy();
		handle.farmData.agentData.ResetSpirit();
	}

	public static void ResetPlayerValues(this ArchiveDataHandle handle, float healthPercent, float energyPercent, float spiritPercent)
	{
		healthPercent = Mathf.Clamp01(healthPercent);
		energyPercent = Mathf.Clamp01(energyPercent);
		int b = Mathf.FloorToInt((float)handle.farmData.agentData.MaxHealth * healthPercent);
		handle.farmData.agentData.health = Mathf.Max(handle.farmData.agentData.health, b);
		int b2 = Mathf.FloorToInt((float)handle.farmData.agentData.MaxEnergy * energyPercent);
		handle.farmData.agentData.energy = Mathf.Max(handle.farmData.agentData.energy, b2);
		handle.farmData.agentData.ResetSpirit(spiritPercent);
	}

	public static int _CostHealth(this ArchiveDataHandle handle, int value)
	{
		if (handle.farmData.agentData.overflowHealth >= value)
		{
			handle.farmData.agentData.overflowHealth -= value;
			return handle.farmData.agentData.health;
		}
		value -= handle.farmData.agentData.overflowHealth;
		handle.farmData.agentData.overflowHealth = 0;
		int num = handle.farmData.agentData.health - value;
		if (num <= 0)
		{
			handle.farmData.agentData.health = 0;
			return 0;
		}
		handle.farmData.agentData.health = num;
		return num;
	}

	public static int _AddHealth(this ArchiveDataHandle handle, int value)
	{
		int health = handle.farmData.agentData.health;
		health = Mathf.Min(health + value, handle.farmData.agentData.MaxHealth);
		handle.farmData.agentData.health = health;
		return health;
	}

	public static void _SetOverflowHealth(this ArchiveDataHandle handle, int value)
	{
		if (value > 0)
		{
			handle.farmData.agentData.overflowHealth = Mathf.Min(value, DolocAPI.GlobalParameter.MaxOverflowHealth);
		}
	}

	public static bool _CostEnergy(this ArchiveDataHandle handle, int value)
	{
		if (handle.farmData.agentData.TotalEnergy < value)
		{
			return false;
		}
		if (handle.farmData.agentData.overflowEnergy >= value)
		{
			handle.farmData.agentData.overflowEnergy -= value;
			return true;
		}
		value -= handle.farmData.agentData.overflowEnergy;
		handle.farmData.agentData.overflowEnergy = 0;
		if (handle.farmData.agentData.energy >= value)
		{
			handle.farmData.agentData.energy -= value;
			return true;
		}
		return false;
	}

	public static void _CostEnergyNoProtect(this ArchiveDataHandle handle, int value)
	{
		if (handle.farmData.agentData.overflowEnergy >= value)
		{
			handle.farmData.agentData.overflowEnergy -= value;
			return;
		}
		value -= handle.farmData.agentData.overflowEnergy;
		handle.farmData.agentData.overflowEnergy = 0;
		if (handle.farmData.agentData.energy >= value)
		{
			handle.farmData.agentData.energy -= value;
		}
		else
		{
			handle.farmData.agentData.energy = 0;
		}
	}

	public static void _SetOverflowEnergy(this ArchiveDataHandle handle, int value)
	{
		if (value > 0)
		{
			handle.farmData.agentData.overflowEnergy = Mathf.Min(value, DolocAPI.GlobalParameter.MaxOverflowEnergy);
		}
	}

	public static int _AddEnergy(this ArchiveDataHandle handle, int value)
	{
		int energy = handle.farmData.agentData.energy;
		energy = Mathf.Min(handle.farmData.agentData.MaxEnergy, energy + value);
		handle.farmData.agentData.energy = energy;
		return energy;
	}

	public static bool HasEnoughEnergy(this ArchiveDataHandle handle, int value)
	{
		return handle.farmData.agentData.energy >= value;
	}

	public static int GetTechPoint(this ArchiveDataHandle handle, TechPointType type)
	{
		return handle.farmData.techLevelManager.GetLevelData(type).AvailablePoints;
	}

	public static int GetTechLevel(this ArchiveDataHandle handle, TechPointType type)
	{
		return handle.farmData.techLevelManager.GetLevelData(type).CurrentLevel;
	}

	public static float GetTechPointExpProgress(this ArchiveDataHandle handle, TechPointType type)
	{
		return handle.farmData.techLevelManager.GetLevelData(type).ExpProgress;
	}

	public static void AddTechPoint(this ArchiveDataHandle handle, TechPointType type, int pt)
	{
		handle.farmData.techLevelManager.ChangeTechPoint(type, pt);
	}

	public static bool AddTechExp(this ArchiveDataHandle handle, TechPointType type, int exp, out TechLevelData levelData)
	{
		levelData = null;
		if (exp <= 0)
		{
			return false;
		}
		return handle.farmData.techLevelManager.ChangeTechExp(type, exp, out levelData);
	}

	public static void _CostTechPoint(this ArchiveDataHandle handle, TechPointType type, int count)
	{
		handle.AddTechPoint(type, -1 * count);
	}

	public static bool HasHat(this ArchiveDataHandle handle, out Item hat)
	{
		hat = handle.farmData.agentData.agentEquipment.hatItem;
		return hat != null;
	}

	public static bool EquipHat(this ArchiveDataHandle handle, Item item, out Item oldHat)
	{
		return handle.farmData.agentData.agentEquipment.EquipHat(item, out oldHat);
	}

	public static Item TakeOffHat(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.agentEquipment.EquipHat(null, out var oldHat);
		return oldHat;
	}

	public static bool EquipDrone(this ArchiveDataHandle handle, Item item, out Item oldDrone)
	{
		return handle.farmData.agentData.agentEquipment.EquipDrone(item, out oldDrone);
	}

	public static Item TakeOffDrone(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.agentEquipment.EquipDrone(null, out var oldDrone);
		return oldDrone;
	}

	public static bool EquipActiveItem(this ArchiveDataHandle handle, Item item, out Item oldItem)
	{
		return handle.farmData.agentData.agentEquipment.EquipActiveItem(item, out oldItem);
	}

	public static Item TakeOffActiveItem(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.agentEquipment.EquipActiveItem(null, out var oldItem);
		return oldItem;
	}

	public static bool EquipPassiveItem(this ArchiveDataHandle handle, Item item, out Item oldItem)
	{
		AgentEquipmentManager agentEquipment = handle.farmData.agentData.agentEquipment;
		if (agentEquipment.passiveItem1 == null)
		{
			return agentEquipment.EquipPassiveItem1(item, out oldItem);
		}
		if (DolocAPI.archiveHandle.IsAdditionalPassiveSlotUnlocked() && agentEquipment.passiveItem2 == null)
		{
			return agentEquipment.EquipPassiveItem2(item, out oldItem);
		}
		return agentEquipment.EquipPassiveItem1(item, out oldItem);
	}

	public static bool EquipPassiveItem1(this ArchiveDataHandle handle, Item item, out Item oldItem)
	{
		return handle.farmData.agentData.agentEquipment.EquipPassiveItem1(item, out oldItem);
	}

	public static bool EquipPassiveItem2(this ArchiveDataHandle handle, Item item, out Item oldItem)
	{
		return handle.farmData.agentData.agentEquipment.EquipPassiveItem2(item, out oldItem);
	}

	public static void UnlockMotor(this ArchiveDataHandle handle)
	{
		handle.farmData.agentData.motorData.UnlockMotor();
		handle.farmData.agentData.motorData.UpdateMotorRoom(DolocAPI.CurrentRoom);
		DolocAPI.Motor.transform.position = DolocAPI.AgentPosition;
	}

	public static void UpdateMotorRoom(this ArchiveDataHandle handle, Room room)
	{
		handle.farmData.agentData.motorData.UpdateMotorRoom(room);
	}

	public static bool IsMotorUnlocked(this ArchiveDataHandle handle)
	{
		return handle.farmData.agentData.motorData.isUnlocked;
	}

	public static void SetPlayerName(this ArchiveDataHandle handle, string name)
	{
		handle.farmData.agentData.playerName = name;
	}

	public static string GetPlayerName(this ArchiveDataHandle handle)
	{
		return handle.farmData.agentData.playerName;
	}

	public static void SetPlayerBirthday(this ArchiveDataHandle handle, int month, int day)
	{
		month = Mathf.Clamp(month, 1, DolocAPI.GlobalParameter.Year2Month);
		day = Mathf.Clamp(day, 1, DolocAPI.GlobalParameter.Month2Day);
		handle.farmData.agentData.birthday = new Vector2Int(month, day);
	}

	public static bool IsPlayerBirthday(this ArchiveDataHandle handle, int month, int day)
	{
		if (handle.farmData.agentData.birthday.x == month)
		{
			return handle.farmData.agentData.birthday.y == day;
		}
		return false;
	}
}
