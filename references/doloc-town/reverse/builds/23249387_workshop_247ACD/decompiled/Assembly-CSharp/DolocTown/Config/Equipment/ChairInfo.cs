using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class ChairInfo : BeanBase
{
	public const int __ID__ = -1673212813;

	public string Id { get; private set; }

	public bool Save { get; private set; }

	public bool FaceRight { get; private set; }

	public SpriteAsset Foreground { get; private set; }

	public SpriteAsset ForegroundFlip { get; private set; }

	public float TimeScale { get; private set; }

	public int LifeTimer { get; private set; }

	public int AddHealth { get; private set; }

	public int AddEnergy { get; private set; }

	public ChairInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["save"].IsBoolean)
		{
			throw new SerializationException();
		}
		Save = _json["save"];
		if (!_json["face_right"].IsBoolean)
		{
			throw new SerializationException();
		}
		FaceRight = _json["face_right"];
		if (!_json["foreground"].IsObject)
		{
			throw new SerializationException();
		}
		Foreground = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["foreground"]));
		if (!_json["foreground_flip"].IsObject)
		{
			throw new SerializationException();
		}
		ForegroundFlip = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["foreground_flip"]));
		if (!_json["time_scale"].IsNumber)
		{
			throw new SerializationException();
		}
		TimeScale = _json["time_scale"];
		if (!_json["life_timer"].IsNumber)
		{
			throw new SerializationException();
		}
		LifeTimer = _json["life_timer"];
		if (!_json["add_health"].IsNumber)
		{
			throw new SerializationException();
		}
		AddHealth = _json["add_health"];
		if (!_json["add_energy"].IsNumber)
		{
			throw new SerializationException();
		}
		AddEnergy = _json["add_energy"];
	}

	public ChairInfo(string id, bool save, bool face_right, SpriteAsset foreground, SpriteAsset foreground_flip, float time_scale, int life_timer, int add_health, int add_energy)
	{
		Id = id;
		Save = save;
		FaceRight = face_right;
		Foreground = foreground;
		ForegroundFlip = foreground_flip;
		TimeScale = time_scale;
		LifeTimer = life_timer;
		AddHealth = add_health;
		AddEnergy = add_energy;
	}

	public static ChairInfo DeserializeChairInfo(JSONNode _json)
	{
		return new ChairInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1673212813;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Save:" + Save + ",FaceRight:" + FaceRight + ",Foreground:" + Foreground?.ToString() + ",ForegroundFlip:" + ForegroundFlip?.ToString() + ",TimeScale:" + TimeScale + ",LifeTimer:" + LifeTimer + ",AddHealth:" + AddHealth + ",AddEnergy:" + AddEnergy + ",}";
	}
}
