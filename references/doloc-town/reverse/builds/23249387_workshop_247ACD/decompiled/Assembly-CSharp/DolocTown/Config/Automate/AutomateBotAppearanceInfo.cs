using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotAppearanceInfo : BeanBase
{
	public const int __ID__ = -589322805;

	private float[] _uvInfosLocalBatteryInfosCharge;

	private float[] _uvInfosLocalBatteryInfosIdle;

	public string Id { get; private set; }

	public SpriteAsset Sprite { get; private set; }

	public bool ShowLocalBattery { get; private set; }

	public int[] LocalBatteryInfosCharge { get; private set; }

	public int[] LocalBatteryInfosIdle { get; private set; }

	public int PowerSlot { get; private set; }

	public AnimatorAsset Animator { get; private set; }

	public float[] UVInfosLocalBatteryInfosCharge
	{
		get
		{
			if (_uvInfosLocalBatteryInfosCharge != null)
			{
				return _uvInfosLocalBatteryInfosCharge;
			}
			_uvInfosLocalBatteryInfosCharge = CalcUVInfos(Sprite.Asset, LocalBatteryInfosCharge);
			return _uvInfosLocalBatteryInfosCharge;
		}
	}

	public float[] UVInfosLocalBatteryInfosIdle
	{
		get
		{
			if (_uvInfosLocalBatteryInfosIdle != null)
			{
				return _uvInfosLocalBatteryInfosIdle;
			}
			_uvInfosLocalBatteryInfosIdle = CalcUVInfos(Sprite.Asset, LocalBatteryInfosIdle);
			return _uvInfosLocalBatteryInfosIdle;
		}
	}

	public AutomateBotAppearanceInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["sprite"].IsObject)
		{
			throw new SerializationException();
		}
		Sprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite"]));
		if (!_json["show_local_battery"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShowLocalBattery = _json["show_local_battery"];
		JSONNode jSONNode = _json["local_battery_infos_charge"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		LocalBatteryInfosCharge = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			LocalBatteryInfosCharge[num++] = num2;
		}
		JSONNode jSONNode2 = _json["local_battery_infos_idle"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		LocalBatteryInfosIdle = new int[count2];
		int num3 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsNumber)
			{
				throw new SerializationException();
			}
			int num4 = child2;
			LocalBatteryInfosIdle[num3++] = num4;
		}
		if (!_json["power_slot"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerSlot = _json["power_slot"];
		if (!_json["animator"].IsObject)
		{
			throw new SerializationException();
		}
		Animator = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator"]));
	}

	public AutomateBotAppearanceInfo(string id, SpriteAsset sprite, bool show_local_battery, int[] local_battery_infos_charge, int[] local_battery_infos_idle, int power_slot, AnimatorAsset animator)
	{
		Id = id;
		Sprite = sprite;
		ShowLocalBattery = show_local_battery;
		LocalBatteryInfosCharge = local_battery_infos_charge;
		LocalBatteryInfosIdle = local_battery_infos_idle;
		PowerSlot = power_slot;
		Animator = animator;
	}

	public static AutomateBotAppearanceInfo DeserializeAutomateBotAppearanceInfo(JSONNode _json)
	{
		return new AutomateBotAppearanceInfo(_json);
	}

	public override int GetTypeId()
	{
		return -589322805;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Sprite:" + Sprite?.ToString() + ",ShowLocalBattery:" + ShowLocalBattery + ",LocalBatteryInfosCharge:" + StringUtil.CollectionToString(LocalBatteryInfosCharge) + ",LocalBatteryInfosIdle:" + StringUtil.CollectionToString(LocalBatteryInfosIdle) + ",PowerSlot:" + PowerSlot + ",Animator:" + Animator?.ToString() + ",}";
	}

	private float[] CalcUVInfos(Sprite sprite, int[] localBatteryInfos)
	{
		float[] array = new float[localBatteryInfos.Length];
		for (int i = 0; i < localBatteryInfos.Length; i++)
		{
			float num = ((i % 2 == 0) ? sprite.rect.size.x : sprite.rect.size.y);
			array[i] = (float)localBatteryInfos[i] / num;
		}
		return array;
	}
}
