using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneChipInfo : BeanBase
{
	public const int __ID__ = 1317697980;

	public string Id { get; private set; }

	public string AttackEffectsExternal { get; private set; }

	public int AttackIncreaseFixed { get; private set; }

	public float AttackIncrease { get; private set; }

	public float CriticalRateIncrease { get; private set; }

	public float AttackSpeedIncrease { get; private set; }

	public float AccuracyIncrease { get; private set; }

	public float PowerCostDecrease { get; private set; }

	public float AttackDistanceIncrease { get; private set; }

	public float MoveSpeedIncrease { get; private set; }

	public int ClipCapacityAddition { get; private set; }

	public float ReloadDurationDecrease { get; private set; }

	public DroneChipInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["attack_effects_external"].IsString)
		{
			throw new SerializationException();
		}
		AttackEffectsExternal = _json["attack_effects_external"];
		if (!_json["attack_increase_fixed"].IsNumber)
		{
			throw new SerializationException();
		}
		AttackIncreaseFixed = _json["attack_increase_fixed"];
		if (!_json["attack_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		AttackIncrease = _json["attack_increase"];
		if (!_json["critical_rate_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		CriticalRateIncrease = _json["critical_rate_increase"];
		if (!_json["attack_speed_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		AttackSpeedIncrease = _json["attack_speed_increase"];
		if (!_json["accuracy_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		AccuracyIncrease = _json["accuracy_increase"];
		if (!_json["power_cost_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerCostDecrease = _json["power_cost_decrease"];
		if (!_json["attack_distance_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		AttackDistanceIncrease = _json["attack_distance_increase"];
		if (!_json["move_speed_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		MoveSpeedIncrease = _json["move_speed_increase"];
		if (!_json["clip_capacity_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		ClipCapacityAddition = _json["clip_capacity_addition"];
		if (!_json["reload_duration_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		ReloadDurationDecrease = _json["reload_duration_decrease"];
	}

	public DroneChipInfo(string id, string attack_effects_external, int attack_increase_fixed, float attack_increase, float critical_rate_increase, float attack_speed_increase, float accuracy_increase, float power_cost_decrease, float attack_distance_increase, float move_speed_increase, int clip_capacity_addition, float reload_duration_decrease)
	{
		Id = id;
		AttackEffectsExternal = attack_effects_external;
		AttackIncreaseFixed = attack_increase_fixed;
		AttackIncrease = attack_increase;
		CriticalRateIncrease = critical_rate_increase;
		AttackSpeedIncrease = attack_speed_increase;
		AccuracyIncrease = accuracy_increase;
		PowerCostDecrease = power_cost_decrease;
		AttackDistanceIncrease = attack_distance_increase;
		MoveSpeedIncrease = move_speed_increase;
		ClipCapacityAddition = clip_capacity_addition;
		ReloadDurationDecrease = reload_duration_decrease;
	}

	public static DroneChipInfo DeserializeDroneChipInfo(JSONNode _json)
	{
		return new DroneChipInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1317697980;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",AttackEffectsExternal:" + AttackEffectsExternal + ",AttackIncreaseFixed:" + AttackIncreaseFixed + ",AttackIncrease:" + AttackIncrease + ",CriticalRateIncrease:" + CriticalRateIncrease + ",AttackSpeedIncrease:" + AttackSpeedIncrease + ",AccuracyIncrease:" + AccuracyIncrease + ",PowerCostDecrease:" + PowerCostDecrease + ",AttackDistanceIncrease:" + AttackDistanceIncrease + ",MoveSpeedIncrease:" + MoveSpeedIncrease + ",ClipCapacityAddition:" + ClipCapacityAddition + ",ReloadDurationDecrease:" + ReloadDurationDecrease + ",}";
	}
}
