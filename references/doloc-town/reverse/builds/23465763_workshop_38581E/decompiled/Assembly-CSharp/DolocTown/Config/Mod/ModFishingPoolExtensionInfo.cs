using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Fishing;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModFishingPoolExtensionInfo : BeanBase
{
	public const int __ID__ = -617058979;

	public string Id { get; private set; }

	public FishingPoolInfo Id_Ref { get; private set; }

	public string[] ExtraFishes { get; private set; }

	public FishInfo[] ExtraFishes_Ref { get; private set; }

	public ModFishingPoolExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_fishes"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraFishes = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ExtraFishes[num++] = text;
		}
	}

	public ModFishingPoolExtensionInfo(string id, string[] extra_fishes)
	{
		Id = id;
		ExtraFishes = extra_fishes;
	}

	public static ModFishingPoolExtensionInfo DeserializeModFishingPoolExtensionInfo(JSONNode _json)
	{
		return new ModFishingPoolExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -617058979;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Fishing.TbFishingPool"] as TbFishingPool).GetOrDefault(Id);
		int num = ExtraFishes.Length;
		TbFish tbFish = (TbFish)_tables["Fishing.TbFish"];
		ExtraFishes_Ref = new FishInfo[num];
		for (int i = 0; i < num; i++)
		{
			ExtraFishes_Ref[i] = tbFish.GetOrDefault(ExtraFishes[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraFishes:" + StringUtil.CollectionToString(ExtraFishes) + ",}";
	}
}
