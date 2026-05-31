using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.Config;

public class ExternalTypeUtil
{
	public static CountItem CountItemConverter(CfgCountItem item)
	{
		return new CountItem(item.ItemName, item.ItemCount);
	}

	public static RangedItem RangedItemConverter(CfgRangedItem item)
	{
		return new RangedItem(item.ItemName, item.MinCount, item.MaxCount);
	}

	public static RewardProto RewardProtoConverter(CfgRewardProto rewardProto)
	{
		return new RewardProto(rewardProto.RewardType, rewardProto.TargetId, rewardProto.TargetCount);
	}

	public static Vector2 Vector2Converter(CfgVector2 vector)
	{
		return new Vector2(vector.X, vector.Y);
	}

	public static Vector2Int Vector2IntConverter(CfgVector2Int vector)
	{
		return new Vector2Int(vector.X, vector.Y);
	}

	public static Vector3Int Vector3IntConverter(CfgVector3Int vector)
	{
		return new Vector3Int(vector.X, vector.Y, vector.Z);
	}

	public static SpriteAsset SpriteAssetConverter(CfgSpriteAsset asset)
	{
		return new SpriteAsset(asset);
	}

	public static Texture2DAsset TextureAssetConverter(CfgTexture2DAsset asset)
	{
		return new Texture2DAsset(asset);
	}

	public static SpriteAssetArray SpriteAssetArrayConverter(CfgSpriteAssetArray asset)
	{
		return new SpriteAssetArray(asset);
	}

	public static PrefabAsset PrefabAssetConverter(CfgPrefabAsset asset)
	{
		return new PrefabAsset(asset);
	}

	public static TileAsset TileAssetConverter(CfgTileAsset asset)
	{
		return new TileAsset(asset);
	}

	public static TileAssetArray TileAssetArrayConverter(CfgTileAssetArray asset)
	{
		return new TileAssetArray(asset);
	}

	public static AnimatorAsset AnimatorAssetConverter(CfgAnimatorAsset asset)
	{
		return new AnimatorAsset(asset.Url);
	}

	public static NpcScheduleAsset NpcScheduleAssetConverter(CfgNpcScheduleAsset asset)
	{
		return new NpcScheduleAsset(asset.Url);
	}

	public static SwitchScheduleAsset SwitchScheduleAssetConverter(CfgSwitchScheduleAsset asset)
	{
		return new SwitchScheduleAsset(asset.Url);
	}

	public static EffectGroupAsset EffectGroupAssetConverter(CfgEffectGroupAsset asset)
	{
		return new EffectGroupAsset(asset.Url);
	}

	public static TmpFontAsset TmpFontAssetConverter(CfgTmpFontAsset asset)
	{
		return new TmpFontAsset(asset.Url);
	}

	public static FontAsset FontAssetConverter(CfgFontAsset asset)
	{
		return new FontAsset(asset.Url);
	}

	public static MaterialAsset MaterialAssetConverter(CfgMaterialAsset asset)
	{
		return new MaterialAsset(asset.Url);
	}

	public static Color ColorConverter(CfgColor cfgColor)
	{
		return new Color((float)cfgColor.R / 255f, (float)cfgColor.G / 255f, (float)cfgColor.B / 255f, (float)cfgColor.A / 255f);
	}

	public static Color ColorConverter(CfgHDRColor cfgColor)
	{
		return new Color((float)cfgColor.R / 255f, (float)cfgColor.G / 255f, (float)cfgColor.B / 255f) * Mathf.Pow(2f, cfgColor.Intensity);
	}

	public static Color ColorConverter(CfgHexColor cfgColor)
	{
		ColorUtility.TryParseHtmlString(cfgColor.Hex, out var color);
		return color;
	}
}
