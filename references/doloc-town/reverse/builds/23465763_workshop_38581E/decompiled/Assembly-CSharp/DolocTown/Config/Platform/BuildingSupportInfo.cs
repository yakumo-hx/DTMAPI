using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Platform;

public sealed class BuildingSupportInfo : BeanBase
{
	public const int __ID__ = -470644786;

	public string Id { get; private set; }

	public TileAsset[] PlatformFront { get; private set; }

	public TileAsset[] PlatformSide { get; private set; }

	public TileAsset Beam { get; private set; }

	public int ColumnWidth { get; private set; }

	public TileAsset[] ColumnForegroundMiddle { get; private set; }

	public TileAsset[] ColumnForegroundBottom { get; private set; }

	public TileAsset[] ColumnBackgroundMiddle { get; private set; }

	public TileAsset[] ColumnBackgroundBottom { get; private set; }

	public TileAsset[] ColumnMiddle { get; private set; }

	public TileAsset[] ColumnBottom { get; private set; }

	public TileAssetArray[] Fulcrums { get; private set; }

	public BuildingSupportInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["platform_front"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		PlatformFront = new TileAsset[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child));
			PlatformFront[num++] = tileAsset;
		}
		JSONNode jSONNode2 = _json["platform_side"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		PlatformSide = new TileAsset[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset2 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child2));
			PlatformSide[num2++] = tileAsset2;
		}
		if (!_json["beam"].IsObject)
		{
			throw new SerializationException();
		}
		Beam = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(_json["beam"]));
		if (!_json["column_width"].IsNumber)
		{
			throw new SerializationException();
		}
		ColumnWidth = _json["column_width"];
		JSONNode jSONNode3 = _json["column_foreground_middle"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		ColumnForegroundMiddle = new TileAsset[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset3 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child3));
			ColumnForegroundMiddle[num3++] = tileAsset3;
		}
		JSONNode jSONNode4 = _json["column_foreground_bottom"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		ColumnForegroundBottom = new TileAsset[count4];
		int num4 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset4 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child4));
			ColumnForegroundBottom[num4++] = tileAsset4;
		}
		JSONNode jSONNode5 = _json["column_background_middle"];
		if (!jSONNode5.IsArray)
		{
			throw new SerializationException();
		}
		int count5 = jSONNode5.Count;
		ColumnBackgroundMiddle = new TileAsset[count5];
		int num5 = 0;
		foreach (JSONNode child5 in jSONNode5.Children)
		{
			if (!child5.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset5 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child5));
			ColumnBackgroundMiddle[num5++] = tileAsset5;
		}
		JSONNode jSONNode6 = _json["column_background_bottom"];
		if (!jSONNode6.IsArray)
		{
			throw new SerializationException();
		}
		int count6 = jSONNode6.Count;
		ColumnBackgroundBottom = new TileAsset[count6];
		int num6 = 0;
		foreach (JSONNode child6 in jSONNode6.Children)
		{
			if (!child6.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset6 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child6));
			ColumnBackgroundBottom[num6++] = tileAsset6;
		}
		JSONNode jSONNode7 = _json["column_middle"];
		if (!jSONNode7.IsArray)
		{
			throw new SerializationException();
		}
		int count7 = jSONNode7.Count;
		ColumnMiddle = new TileAsset[count7];
		int num7 = 0;
		foreach (JSONNode child7 in jSONNode7.Children)
		{
			if (!child7.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset7 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child7));
			ColumnMiddle[num7++] = tileAsset7;
		}
		JSONNode jSONNode8 = _json["column_bottom"];
		if (!jSONNode8.IsArray)
		{
			throw new SerializationException();
		}
		int count8 = jSONNode8.Count;
		ColumnBottom = new TileAsset[count8];
		int num8 = 0;
		foreach (JSONNode child8 in jSONNode8.Children)
		{
			if (!child8.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset8 = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child8));
			ColumnBottom[num8++] = tileAsset8;
		}
		JSONNode jSONNode9 = _json["fulcrums"];
		if (!jSONNode9.IsArray)
		{
			throw new SerializationException();
		}
		int count9 = jSONNode9.Count;
		Fulcrums = new TileAssetArray[count9];
		int num9 = 0;
		foreach (JSONNode child9 in jSONNode9.Children)
		{
			if (!child9.IsObject)
			{
				throw new SerializationException();
			}
			TileAssetArray tileAssetArray = ExternalTypeUtil.TileAssetArrayConverter(CfgTileAssetArray.DeserializeCfgTileAssetArray(child9));
			Fulcrums[num9++] = tileAssetArray;
		}
	}

	public BuildingSupportInfo(string id, TileAsset[] platform_front, TileAsset[] platform_side, TileAsset beam, int column_width, TileAsset[] column_foreground_middle, TileAsset[] column_foreground_bottom, TileAsset[] column_background_middle, TileAsset[] column_background_bottom, TileAsset[] column_middle, TileAsset[] column_bottom, TileAssetArray[] fulcrums)
	{
		Id = id;
		PlatformFront = platform_front;
		PlatformSide = platform_side;
		Beam = beam;
		ColumnWidth = column_width;
		ColumnForegroundMiddle = column_foreground_middle;
		ColumnForegroundBottom = column_foreground_bottom;
		ColumnBackgroundMiddle = column_background_middle;
		ColumnBackgroundBottom = column_background_bottom;
		ColumnMiddle = column_middle;
		ColumnBottom = column_bottom;
		Fulcrums = fulcrums;
	}

	public static BuildingSupportInfo DeserializeBuildingSupportInfo(JSONNode _json)
	{
		return new BuildingSupportInfo(_json);
	}

	public override int GetTypeId()
	{
		return -470644786;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",PlatformFront:" + StringUtil.CollectionToString(PlatformFront) + ",PlatformSide:" + StringUtil.CollectionToString(PlatformSide) + ",Beam:" + Beam?.ToString() + ",ColumnWidth:" + ColumnWidth + ",ColumnForegroundMiddle:" + StringUtil.CollectionToString(ColumnForegroundMiddle) + ",ColumnForegroundBottom:" + StringUtil.CollectionToString(ColumnForegroundBottom) + ",ColumnBackgroundMiddle:" + StringUtil.CollectionToString(ColumnBackgroundMiddle) + ",ColumnBackgroundBottom:" + StringUtil.CollectionToString(ColumnBackgroundBottom) + ",ColumnMiddle:" + StringUtil.CollectionToString(ColumnMiddle) + ",ColumnBottom:" + StringUtil.CollectionToString(ColumnBottom) + ",Fulcrums:" + StringUtil.CollectionToString(Fulcrums) + ",}";
	}
}
