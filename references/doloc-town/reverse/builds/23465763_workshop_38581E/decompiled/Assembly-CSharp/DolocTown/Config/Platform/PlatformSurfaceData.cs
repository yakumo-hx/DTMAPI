using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Weight;
using SimpleJSON;

namespace DolocTown.Config.Platform;

public sealed class PlatformSurfaceData : BeanBase
{
	public const int __ID__ = -1019622007;

	public SpriteAsset LeftSock { get; private set; }

	public SpriteAsset[] LeftSequence { get; private set; }

	public SpriteWeight[] FillTiles { get; private set; }

	public SpriteAsset[] RightSequence { get; private set; }

	public SpriteAsset RightSock { get; private set; }

	public PlatformSurfaceData(JSONNode _json)
	{
		if (!_json["left_sock"].IsObject)
		{
			throw new SerializationException();
		}
		LeftSock = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["left_sock"]));
		JSONNode jSONNode = _json["left_sequence"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		LeftSequence = new SpriteAsset[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child));
			LeftSequence[num++] = spriteAsset;
		}
		JSONNode jSONNode2 = _json["fill_tiles"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		FillTiles = new SpriteWeight[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			SpriteWeight spriteWeight = SpriteWeight.DeserializeSpriteWeight(child2);
			FillTiles[num2++] = spriteWeight;
		}
		JSONNode jSONNode3 = _json["right_sequence"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		RightSequence = new SpriteAsset[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset2 = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child3));
			RightSequence[num3++] = spriteAsset2;
		}
		if (!_json["right_sock"].IsObject)
		{
			throw new SerializationException();
		}
		RightSock = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["right_sock"]));
	}

	public PlatformSurfaceData(SpriteAsset left_sock, SpriteAsset[] left_sequence, SpriteWeight[] fill_tiles, SpriteAsset[] right_sequence, SpriteAsset right_sock)
	{
		LeftSock = left_sock;
		LeftSequence = left_sequence;
		FillTiles = fill_tiles;
		RightSequence = right_sequence;
		RightSock = right_sock;
	}

	public static PlatformSurfaceData DeserializePlatformSurfaceData(JSONNode _json)
	{
		return new PlatformSurfaceData(_json);
	}

	public override int GetTypeId()
	{
		return -1019622007;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpriteWeight[] fillTiles = FillTiles;
		for (int i = 0; i < fillTiles.Length; i++)
		{
			fillTiles[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SpriteWeight[] fillTiles = FillTiles;
		for (int i = 0; i < fillTiles.Length; i++)
		{
			fillTiles[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ LeftSock:" + LeftSock?.ToString() + ",LeftSequence:" + StringUtil.CollectionToString(LeftSequence) + ",FillTiles:" + StringUtil.CollectionToString(FillTiles) + ",RightSequence:" + StringUtil.CollectionToString(RightSequence) + ",RightSock:" + RightSock?.ToString() + ",}";
	}
}
