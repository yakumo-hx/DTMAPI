using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using DolocTown.Config.Platform;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Building;

public sealed class BuildingInfo : BeanBase
{
	public const int __ID__ = -1823919492;

	private TerrainLayerName frontLayers = TerrainLayerName.Ground | TerrainLayerName.CeilingFront;

	private TerrainLayerName allLayers = TerrainLayerName.Ceiling | TerrainLayerName.Ground;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public string BlueprintItem { get; private set; }

	public ItemInfo BlueprintItem_Ref { get; private set; }

	public int Order { get; private set; }

	public float Health { get; private set; }

	public float DamageSufferRate { get; private set; }

	public float DamageSufferRateThunder { get; private set; }

	public int AnimalSpace { get; private set; }

	public string DefaultExterior { get; private set; }

	public BuildingExteriorInfo DefaultExterior_Ref { get; private set; }

	public string DefaultWallpaper { get; private set; }

	public BuildingWallpaperInfo DefaultWallpaper_Ref { get; private set; }

	public bool DisableWeather { get; private set; }

	public int GroundDepth { get; private set; }

	public bool IsUnique { get; private set; }

	public int MoneyCost { get; private set; }

	public CountItem[] ItemCost { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public int EntryPixelX { get; private set; }

	public Vector2Int ArrowTipPixelOffset { get; private set; }

	public Vector2Int HealthTipPixelOffset { get; private set; }

	public string SupportId { get; private set; }

	public BuildingSupportInfo SupportId_Ref { get; private set; }

	public ArrayInt[] TileMarks { get; private set; }

	public List<BuildingLevelData> BuildingLevelDatas { get; private set; }

	public int MaxLevel => BuildingLevelDatas.Count - 1;

	public float HealthReciprocal => 1f / Health;

	public BuildingExteriorData DefaultExteriorData { get; private set; }

	public Sprite DefaultSceneSprite => DefaultExteriorData.ClosedSpriteGroup.SceneSprite.Asset;

	public BuildingWallpaperData DefaultWallpaperData { get; private set; }

	public bool IsAnimalBuilding => AnimalSpace > 0;

	public bool HasHealthInfo
	{
		get
		{
			if (DamageSufferRate > 0f)
			{
				return DamageSufferRateThunder > 0f;
			}
			return false;
		}
	}

	public BuildingLevelData DefaultLevelData => GetLevelData(0);

	public Vector2Int CoverSize { get; private set; }

	public Vector2Int DefaultInnerSize
	{
		get
		{
			DolocAPI.QueryTemplateRoom(DefaultLevelData.TemplateRoomName, out var proto);
			return proto.geometry.gridSize;
		}
	}

	public int FrontCellingWidth { get; private set; }

	public int FrontFloorWidth { get; private set; }

	public int NoOverlayWidth { get; private set; }

	public int MaxDepth { get; private set; }

	public int Height => CoverSize.y;

	public FulcrumData[] FulcrumDatas { get; private set; }

	public Vector2 EntryPosition { get; private set; }

	public Vector2Int[] FrontFloorPositions { get; private set; }

	public Vector2Int[] FrontCeilingPositions { get; private set; }

	public BuildingTileType[][] TileTypes { get; private set; }

	public BuildingInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["blueprint_item"].IsString)
		{
			throw new SerializationException();
		}
		BlueprintItem = _json["blueprint_item"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
		if (!_json["health"].IsNumber)
		{
			throw new SerializationException();
		}
		Health = _json["health"];
		if (!_json["damage_suffer_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		DamageSufferRate = _json["damage_suffer_rate"];
		if (!_json["damage_suffer_rate_thunder"].IsNumber)
		{
			throw new SerializationException();
		}
		DamageSufferRateThunder = _json["damage_suffer_rate_thunder"];
		if (!_json["animal_space"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalSpace = _json["animal_space"];
		if (!_json["default_exterior"].IsString)
		{
			throw new SerializationException();
		}
		DefaultExterior = _json["default_exterior"];
		if (!_json["default_wallpaper"].IsString)
		{
			throw new SerializationException();
		}
		DefaultWallpaper = _json["default_wallpaper"];
		if (!_json["disable_weather"].IsBoolean)
		{
			throw new SerializationException();
		}
		DisableWeather = _json["disable_weather"];
		if (!_json["ground_depth"].IsNumber)
		{
			throw new SerializationException();
		}
		GroundDepth = _json["ground_depth"];
		if (!_json["is_unique"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsUnique = _json["is_unique"];
		if (!_json["money_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		MoneyCost = _json["money_cost"];
		JSONNode jSONNode = _json["item_cost"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ItemCost = new CountItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem countItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			ItemCost[num++] = countItem;
		}
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["entry_pixel_x"].IsNumber)
		{
			throw new SerializationException();
		}
		EntryPixelX = _json["entry_pixel_x"];
		if (!_json["arrow_tip_pixel_offset"].IsObject)
		{
			throw new SerializationException();
		}
		ArrowTipPixelOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["arrow_tip_pixel_offset"]));
		if (!_json["health_tip_pixel_offset"].IsObject)
		{
			throw new SerializationException();
		}
		HealthTipPixelOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["health_tip_pixel_offset"]));
		if (!_json["support_id"].IsString)
		{
			throw new SerializationException();
		}
		SupportId = _json["support_id"];
		JSONNode jSONNode2 = _json["tile_marks"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		TileMarks = new ArrayInt[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			ArrayInt arrayInt = ArrayInt.DeserializeArrayInt(child2);
			TileMarks[num2++] = arrayInt;
		}
		JSONNode jSONNode3 = _json["building_level_datas"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		BuildingLevelDatas = new List<BuildingLevelData>(jSONNode3.Count);
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			BuildingLevelData item = BuildingLevelData.DeserializeBuildingLevelData(child3);
			BuildingLevelDatas.Add(item);
		}
	}

	public BuildingInfo(string id, string title, string description, string blueprint_item, int order, float health, float damage_suffer_rate, float damage_suffer_rate_thunder, int animal_space, string default_exterior, string default_wallpaper, bool disable_weather, int ground_depth, bool is_unique, int money_cost, CountItem[] item_cost, SpriteAsset ui_sprite_asset, int entry_pixel_x, Vector2Int arrow_tip_pixel_offset, Vector2Int health_tip_pixel_offset, string support_id, ArrayInt[] tile_marks, List<BuildingLevelData> building_level_datas)
	{
		Id = id;
		Title = title;
		Description = description;
		BlueprintItem = blueprint_item;
		Order = order;
		Health = health;
		DamageSufferRate = damage_suffer_rate;
		DamageSufferRateThunder = damage_suffer_rate_thunder;
		AnimalSpace = animal_space;
		DefaultExterior = default_exterior;
		DefaultWallpaper = default_wallpaper;
		DisableWeather = disable_weather;
		GroundDepth = ground_depth;
		IsUnique = is_unique;
		MoneyCost = money_cost;
		ItemCost = item_cost;
		UiSpriteAsset = ui_sprite_asset;
		EntryPixelX = entry_pixel_x;
		ArrowTipPixelOffset = arrow_tip_pixel_offset;
		HealthTipPixelOffset = health_tip_pixel_offset;
		SupportId = support_id;
		TileMarks = tile_marks;
		BuildingLevelDatas = building_level_datas;
	}

	public static BuildingInfo DeserializeBuildingInfo(JSONNode _json)
	{
		return new BuildingInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1823919492;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		BlueprintItem_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(BlueprintItem);
		DefaultExterior_Ref = (_tables["Building.TbBuildingExterior"] as TbBuildingExterior).GetOrDefault(DefaultExterior);
		DefaultWallpaper_Ref = (_tables["Building.TbBuildingWallpaper"] as TbBuildingWallpaper).GetOrDefault(DefaultWallpaper);
		SupportId_Ref = (_tables["Platform.TbBuildingSupport"] as TbBuildingSupport).GetOrDefault(SupportId);
		ArrayInt[] tileMarks = TileMarks;
		for (int i = 0; i < tileMarks.Length; i++)
		{
			tileMarks[i]?.Resolve(_tables);
		}
		foreach (BuildingLevelData buildingLevelData in BuildingLevelDatas)
		{
			buildingLevelData?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
		ArrayInt[] tileMarks = TileMarks;
		for (int i = 0; i < tileMarks.Length; i++)
		{
			tileMarks[i]?.TranslateText(translator);
		}
		foreach (BuildingLevelData buildingLevelData in BuildingLevelDatas)
		{
			buildingLevelData?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",Description:" + Description + ",BlueprintItem:" + BlueprintItem + ",Order:" + Order + ",Health:" + Health + ",DamageSufferRate:" + DamageSufferRate + ",DamageSufferRateThunder:" + DamageSufferRateThunder + ",AnimalSpace:" + AnimalSpace + ",DefaultExterior:" + DefaultExterior + ",DefaultWallpaper:" + DefaultWallpaper + ",DisableWeather:" + DisableWeather + ",GroundDepth:" + GroundDepth + ",IsUnique:" + IsUnique + ",MoneyCost:" + MoneyCost + ",ItemCost:" + StringUtil.CollectionToString(ItemCost) + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",EntryPixelX:" + EntryPixelX + ",ArrowTipPixelOffset:" + ArrowTipPixelOffset.ToString() + ",HealthTipPixelOffset:" + HealthTipPixelOffset.ToString() + ",SupportId:" + SupportId + ",TileMarks:" + StringUtil.CollectionToString(TileMarks) + ",BuildingLevelDatas:" + StringUtil.CollectionToString(BuildingLevelDatas) + ",}";
	}

	private void PostResolve()
	{
		try
		{
			if (DefaultExterior_Ref != null && DefaultExterior_Ref.ExteriorDatas_Index.TryGetValue(Id, out var value))
			{
				DefaultExteriorData = value;
			}
			if (DefaultWallpaper_Ref != null && DefaultWallpaper_Ref.WallpaperDatas_Index.TryGetValue(Id, out var value2))
			{
				DefaultWallpaperData = value2;
			}
			TileTypes = LoadTileTypes();
			CoverSize = new Vector2Int(TileTypes[0].Length, TileTypes.Length);
			FrontCellingWidth = GetPositionsOfType(Vector2Int.zero, BuildingTileType.Ceiling, BuildingTileType.Side).Length;
			FrontFloorWidth = GetPositionsOfType(Vector2Int.zero, BuildingTileType.Floor, BuildingTileType.Side).Length;
			MaxDepth = GetPositionsOfType(Vector2Int.zero, BuildingTileType.Ceiling, BuildingTileType.Front).Length;
			NoOverlayWidth = Geometry.GetRectFromPositions(GetPositionsOfType(Vector2Int.zero, BuildingTileType.NoOverlay)).width;
			CalFulcrumData();
			EntryPosition = CalEntryPosition();
			FrontCeilingPositions = GetPositionsOfType(Vector2Int.zero, BuildingTileType.Front | BuildingTileType.Ceiling);
			FrontFloorPositions = GetPositionsOfType(Vector2Int.zero, BuildingTileType.Front | BuildingTileType.Floor);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			Debug.LogError("建筑<" + Id + ">配置数据有误，请检查配置");
		}
	}

	public Vector2Int[] GetGroundPositions(Vector2Int anchor, int depth = 1)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < CoverSize.x; i++)
		{
			for (int j = 0; j < depth; j++)
			{
				list.Add(new Vector2Int(i, -(j + 1)) + anchor);
			}
		}
		return list.ToArray();
	}

	public Vector2Int[] GetCoverPositions(Vector2Int anchor)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < CoverSize.x; i++)
		{
			for (int j = 0; j < CoverSize.y; j++)
			{
				list.Add(anchor + new Vector2Int(i, j));
			}
		}
		return list.ToArray();
	}

	public Vector2Int[] GetPositionsOfType(Vector2Int anchor, BuildingTileType tileType)
	{
		return GetPositionsOfType(anchor, tileType, Vector2Int.zero);
	}

	public Vector2Int[] GetPositionsOfType(Vector2Int anchor, BuildingTileType tileType, Vector2Int offset)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < CoverSize.y; i++)
		{
			for (int j = 0; j < CoverSize.x; j++)
			{
				if (TileTypes[i][j].HasFlag(tileType))
				{
					list.Add(anchor + new Vector2Int(j, i) + offset);
				}
			}
		}
		return list.ToArray();
	}

	public Vector2Int[] GetPositionsOfType(Vector2Int anchor, BuildingTileType includedFlag, BuildingTileType excludedFlag, Vector2Int offset)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < CoverSize.y; i++)
		{
			for (int j = 0; j < CoverSize.x; j++)
			{
				if (TileTypes[i][j].HasFlag(includedFlag) && !TileTypes[i][j].HasFlag(excludedFlag))
				{
					list.Add(anchor + new Vector2Int(j, i) + offset);
				}
			}
		}
		return list.ToArray();
	}

	public Vector2Int[] GetPositionsOfType(Vector2Int anchor, BuildingTileType includedFlag, BuildingTileType excludedFlag)
	{
		return GetPositionsOfType(anchor, includedFlag, excludedFlag, Vector2Int.zero);
	}

	public BuildingTileType GetPositionType(Vector2Int position)
	{
		if (0 <= position.y && position.y < TileTypes.Length && 0 <= position.x && position.x < TileTypes[0].Length)
		{
			return TileTypes[position.y][position.x];
		}
		return BuildingTileType.None;
	}

	private BuildingTileType[][] LoadTileTypes()
	{
		DolocAssert.IsTrue(TileMarks.Length != 0 && TileMarks[0].Array.Length != 0);
		BuildingTileType[][] array = new BuildingTileType[TileMarks.Length][];
		int num = TileMarks[0].Array.Length;
		for (int i = 0; i < TileMarks.Length; i++)
		{
			int[] array2 = TileMarks[i].Array;
			DolocAssert.IsTrue(array2.Length == num);
			array[i] = array2.Select((int x) => (BuildingTileType)x).ToArray();
		}
		return array;
	}

	public BuildingLevelData GetLevelData(int level)
	{
		return BuildingLevelDatas[Mathf.Clamp(level, 0, BuildingLevelDatas.Count - 1)];
	}

	private void CalFulcrumData()
	{
		FulcrumDatas = new FulcrumData[4];
		Vector2Int[] positionsOfType = GetPositionsOfType(Vector2Int.zero, BuildingTileType.Fulcrum);
		int num = positionsOfType.Length;
		DolocAssert.IsTrue(num == 3 || num == 4, "支点数量应为3或者4，请检查建筑<" + Id + ">的瓦片标注");
		FulcrumDatas = new FulcrumData[4];
		int columnWidth = SupportId_Ref.ColumnWidth;
		FulcrumDatas[0] = new FulcrumData(GetXContinuesPositions(positionsOfType[0], columnWidth), GetRaycastLayer(columnWidth, leftOnlyFront: true, rightOnlyFront: false), 0);
		FulcrumDatas[1] = new FulcrumData(GetXContinuesPositions(positionsOfType[1], columnWidth), GetRaycastLayer(columnWidth, leftOnlyFront: false, rightOnlyFront: false), MaxDepth);
		FulcrumDatas[2] = new FulcrumData(GetXContinuesPositions(positionsOfType[^2], columnWidth), GetRaycastLayer(columnWidth, leftOnlyFront: true, rightOnlyFront: false), 0);
		FulcrumDatas[3] = new FulcrumData(GetXContinuesPositions(positionsOfType[^1], columnWidth), GetRaycastLayer(columnWidth, leftOnlyFront: false, rightOnlyFront: false), MaxDepth);
	}

	private Vector2Int[] GetXContinuesPositions(Vector2Int pos, int width)
	{
		Vector2Int[] array = new Vector2Int[width];
		for (int i = 0; i < width; i++)
		{
			array[i] = new Vector2Int(pos.x + i, pos.y);
		}
		return array;
	}

	private TerrainLayerName[] GetRaycastLayer(int width, bool leftOnlyFront, bool rightOnlyFront)
	{
		List<TerrainLayerName> list = new List<TerrainLayerName>();
		for (int i = 0; i < width; i++)
		{
			if (i < (width + 1) / 2)
			{
				list.Add(leftOnlyFront ? frontLayers : allLayers);
			}
			else
			{
				list.Add(rightOnlyFront ? frontLayers : allLayers);
			}
		}
		return list.ToArray();
	}

	private Vector2 CalEntryPosition()
	{
		return new Vector2((float)EntryPixelX * 0.125f, 0f);
	}

	public void GetDoorInfo(Vector2Int anchor, out Vector2Int lb, out Vector2Int size)
	{
		Vector2Int[] positionsOfType = GetPositionsOfType(anchor, BuildingTileType.Door);
		lb = positionsOfType[0];
		size = positionsOfType[^1] - positionsOfType[0];
		size.x++;
		size.y++;
	}

	public Vector2[] GetLinkGatePositions(int level, BuildingLinkType type)
	{
		BuildingLevelData levelData = GetLevelData(level);
		return type switch
		{
			BuildingLinkType.Left => levelData.LinkGatesLeft, 
			BuildingLinkType.Right => levelData.LinkGatesRight, 
			BuildingLinkType.Bottom => levelData.LinkGatesBottom, 
			BuildingLinkType.Top => levelData.LinkGatesTop, 
			_ => Array.Empty<Vector2>(), 
		};
	}

	public bool GetNearestLinkGatePosition(int level, BuildingLinkType type, float position, out Vector2 output, out int index, out float minDst)
	{
		output = default(Vector2);
		index = -1;
		minDst = float.MaxValue;
		Vector2[] linkGatePositions = GetLinkGatePositions(level, type);
		if (linkGatePositions.Length == 0)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < linkGatePositions.Length; i++)
		{
			Vector2 vector = linkGatePositions[i];
			float num = Math.Abs(vector.x - position);
			if (!(num >= minDst))
			{
				minDst = num;
				output = vector;
				index = i;
				result = true;
			}
		}
		return result;
	}

	public bool TryGetLinkGatePosition(int level, BuildingLinkType type, int idx, out Vector2 position)
	{
		position = default(Vector2);
		Vector2[] linkGatePositions = GetLinkGatePositions(level, type);
		if (idx < 0 || idx >= linkGatePositions.Length)
		{
			return false;
		}
		position = linkGatePositions[idx];
		return true;
	}
}
