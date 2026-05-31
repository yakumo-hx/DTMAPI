using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class FarmFishFormationInfo : BeanBase
{
	private readonly struct SubFormationCondition
	{
		public readonly HashSet<string> fishIds;

		public readonly Vector2Int countRange;

		public readonly int groupId;

		public SubFormationCondition(HashSet<string> fishIds, Vector2Int countRange, int groupId)
		{
			this.fishIds = fishIds;
			this.countRange = countRange;
			this.groupId = groupId;
		}

		public bool CheckCondition(IEnumerable<string> items)
		{
			int num = 0;
			foreach (string item in items)
			{
				if (fishIds.Contains(item))
				{
					num++;
				}
			}
			return CheckCount(countRange, num);
		}

		private bool CheckCount(Vector2Int countRange, int count)
		{
			if (countRange.x >= 0 && count < countRange.x)
			{
				return false;
			}
			if (countRange.y >= 0 && count > countRange.y)
			{
				return false;
			}
			return true;
		}
	}

	private readonly struct FormationCondition
	{
		private readonly SubFormationCondition[] conditions;

		public FormationCondition(SubFormationCondition[] conditions)
		{
			this.conditions = conditions;
		}

		public bool CheckCondition(IEnumerable<string> items)
		{
			return conditions.Any((SubFormationCondition condition) => condition.CheckCondition(items));
		}
	}

	public const int __ID__ = -573945011;

	private FormationCondition[] _conditions;

	public string Id { get; private set; }

	public int Weight { get; private set; }

	public string FormationOutputFish { get; private set; }

	public Vector2Int CountRange { get; private set; }

	public FishFormationCondition[] FormationConditions { get; private set; }

	public FarmFishFormationInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["weight"].IsNumber)
		{
			throw new SerializationException();
		}
		Weight = _json["weight"];
		if (!_json["formation_output_fish"].IsString)
		{
			throw new SerializationException();
		}
		FormationOutputFish = _json["formation_output_fish"];
		if (!_json["count_range"].IsObject)
		{
			throw new SerializationException();
		}
		CountRange = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["count_range"]));
		JSONNode jSONNode = _json["formation_conditions"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		FormationConditions = new FishFormationCondition[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			FishFormationCondition fishFormationCondition = FishFormationCondition.DeserializeFishFormationCondition(child);
			FormationConditions[num++] = fishFormationCondition;
		}
	}

	public FarmFishFormationInfo(string id, int weight, string formation_output_fish, Vector2Int count_range, FishFormationCondition[] formation_conditions)
	{
		Id = id;
		Weight = weight;
		FormationOutputFish = formation_output_fish;
		CountRange = count_range;
		FormationConditions = formation_conditions;
	}

	public static FarmFishFormationInfo DeserializeFarmFishFormationInfo(JSONNode _json)
	{
		return new FarmFishFormationInfo(_json);
	}

	public override int GetTypeId()
	{
		return -573945011;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		FishFormationCondition[] formationConditions = FormationConditions;
		for (int i = 0; i < formationConditions.Length; i++)
		{
			formationConditions[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		FishFormationCondition[] formationConditions = FormationConditions;
		for (int i = 0; i < formationConditions.Length; i++)
		{
			formationConditions[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Weight:" + Weight + ",FormationOutputFish:" + FormationOutputFish + ",CountRange:" + CountRange.ToString() + ",FormationConditions:" + StringUtil.CollectionToString(FormationConditions) + ",}";
	}

	public static FarmFishFormationInfo[] GetAllSatisfiedFormations(IEnumerable<string> items)
	{
		return DolocConfig.Tables.TbFarmFishFormation.DataList.Where((FarmFishFormationInfo formation) => formation.CheckFormation(items)).ToArray();
	}

	public bool CheckFormation(IEnumerable<string> items)
	{
		if (_conditions == null)
		{
			Init();
		}
		if (!_conditions.IsNullOrEmpty())
		{
			return _conditions.All((FormationCondition condition) => condition.CheckCondition(items));
		}
		return false;
	}

	private void Init()
	{
		if (_conditions != null)
		{
			return;
		}
		List<SubFormationCondition> list = new List<SubFormationCondition>();
		FishFormationCondition[] formationConditions = FormationConditions;
		foreach (FishFormationCondition fishFormationCondition in formationConditions)
		{
			if (!TryParseSubFormationCondition(fishFormationCondition, out var output))
			{
				Debug.LogError($"<FarmFishFormation>解析条件失败: {fishFormationCondition}");
			}
			else
			{
				list.Add(output);
			}
		}
		List<FormationCondition> list2 = new List<FormationCondition>();
		Dictionary<int, List<SubFormationCondition>> dictionary = new Dictionary<int, List<SubFormationCondition>>();
		foreach (SubFormationCondition item in list)
		{
			if (item.groupId < 0)
			{
				list2.Add(new FormationCondition(new SubFormationCondition[1] { item }));
				continue;
			}
			if (!dictionary.TryGetValue(item.groupId, out var value))
			{
				value = new List<SubFormationCondition>();
				dictionary[item.groupId] = value;
			}
			value.Add(item);
		}
		foreach (KeyValuePair<int, List<SubFormationCondition>> item2 in dictionary)
		{
			SubFormationCondition[] conditions = item2.Value.ToArray();
			list2.Add(new FormationCondition(conditions));
		}
		_conditions = list2.ToArray();
	}

	private static bool TryParseSubFormationCondition(FishFormationCondition condition, out SubFormationCondition output)
	{
		output = default(SubFormationCondition);
		HashSet<string> hashSet = new HashSet<string>();
		if (!IsFarmFish(condition.FishId))
		{
			return false;
		}
		hashSet.Add(condition.FishId);
		output = new SubFormationCondition(hashSet, condition.CountRange, condition.Index);
		return true;
	}

	private static bool IsFarmFish(string name)
	{
		return DolocConfig.Tables.TbFarmFish.DataMap.ContainsKey(name);
	}
}
