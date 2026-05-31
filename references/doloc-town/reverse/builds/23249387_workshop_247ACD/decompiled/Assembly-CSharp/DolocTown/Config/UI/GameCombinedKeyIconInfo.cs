using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class GameCombinedKeyIconInfo : BeanBase
{
	public const int __ID__ = -1808920267;

	public string Id { get; private set; }

	public SpriteAsset CombinedIcon { get; private set; }

	public string[] SubPaths { get; private set; }

	public GameCombinedKeyIconInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["combined_icon"].IsObject)
		{
			throw new SerializationException();
		}
		CombinedIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["combined_icon"]));
		JSONNode jSONNode = _json["sub_paths"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		SubPaths = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			SubPaths[num++] = text;
		}
	}

	public GameCombinedKeyIconInfo(string id, SpriteAsset combined_icon, string[] sub_paths)
	{
		Id = id;
		CombinedIcon = combined_icon;
		SubPaths = sub_paths;
	}

	public static GameCombinedKeyIconInfo DeserializeGameCombinedKeyIconInfo(JSONNode _json)
	{
		return new GameCombinedKeyIconInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1808920267;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",CombinedIcon:" + CombinedIcon?.ToString() + ",SubPaths:" + StringUtil.CollectionToString(SubPaths) + ",}";
	}
}
