using DolocTown.Config;
using DolocTown.Config.TechTree;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class TechLevelData
{
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly TechPointType type;

	[JsonProperty]
	private int totalExp;

	public readonly TechPointInfo proto;

	private int currentLevel;

	private int currentExp;

	public int TotalExp => totalExp;

	public int AvailablePoints { get; private set; }

	public int CurrentLevel => currentLevel;

	public int CurrentExp => currentExp;

	public Sprite Icon => proto.Icon.Asset;

	public bool IsValid => proto != null;

	public bool CanLevelUp => currentLevel < proto.MaxLevel;

	public bool CanExpUp => totalExp < proto.MaxExp;

	public bool IsFullyMax
	{
		get
		{
			if (!CanExpUp)
			{
				return !CanLevelUp;
			}
			return false;
		}
	}

	public float ExpProgress
	{
		get
		{
			if (!CanExpUp)
			{
				return 1f;
			}
			TechLevelInfo currentLevelInfo = GetCurrentLevelInfo();
			return (float)currentExp / (float)currentLevelInfo.RequiredExp;
		}
	}

	public string ExpProgressString
	{
		get
		{
			TechLevelInfo currentLevelInfo = GetCurrentLevelInfo();
			if (currentLevelInfo == null)
			{
				return "";
			}
			return $"{currentExp}/{currentLevelInfo.RequiredExp}";
		}
	}

	public int PointsForNextLevel => GetCurrentLevelInfo()?.RewardPoints ?? 0;

	[JsonConstructor]
	public TechLevelData(TechPointType type, int totalExp = 0)
	{
		this.type = type;
		proto = DolocConfig.Tables.TbTechPoint.GetOrDefault(type);
		if (proto != null)
		{
			this.totalExp = totalExp;
			RefreshLevelAndExp();
		}
	}

	private void RefreshLevelAndExp()
	{
		proto.GetLevelAndExp(totalExp, out currentLevel, out currentExp);
	}

	private TechLevelInfo GetLevelInfo(int level)
	{
		if (!proto.LevelMap_Index.TryGetValue(level, out var value))
		{
			return null;
		}
		return value;
	}

	public TechLevelInfo GetCurrentLevelInfo()
	{
		return GetLevelInfo(currentLevel);
	}

	public void __Clear()
	{
		totalExp = 0;
		AvailablePoints = 0;
		currentLevel = 0;
		currentExp = 0;
	}

	public bool AddTechExp(int exp)
	{
		if (exp <= 0)
		{
			return false;
		}
		int num = currentLevel;
		totalExp += exp;
		RefreshLevelAndExp();
		if (num == currentLevel)
		{
			return false;
		}
		for (int i = num; i < currentLevel; i++)
		{
			AvailablePoints += GetLevelInfo(i).RewardPoints;
		}
		return true;
	}

	public void ResetAvailablePoints()
	{
		AvailablePoints = 0;
		for (int i = 0; i < currentLevel; i++)
		{
			AvailablePoints += GetLevelInfo(i).RewardPoints;
		}
	}

	public void ChangeTechPoint(int point)
	{
		AvailablePoints += point;
		AvailablePoints = Mathf.Max(0, AvailablePoints);
	}
}
