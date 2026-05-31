using DolocTown.Config;
using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class SeedCompressor : CustomSynthesizer
{
	public SeedCompressor(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public SeedCompressor(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, IRecipe latestRecipe, IRecipeGroup latestRecipeGroup, LinearInventory itemBuffer, Counter taskCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter, latestRecipe, latestRecipeGroup, itemBuffer, taskCounter)
	{
	}

	protected override string GetExtraInfo(Item item)
	{
		if (item is IHasGeneGroup { HasGene: not false })
		{
			return DolocConfig.StaticTexts.RecipePanelRandomGeneHint;
		}
		return "";
	}

	public override bool ContentFilter(Item content)
	{
		if (base.ContentFilter(content))
		{
			if (content is IHasGeneGroup hasGeneGroup)
			{
				return !hasGeneGroup.IsCloned;
			}
			return false;
		}
		return false;
	}

	protected override Item HandlePreviewItem(Item item)
	{
		if (item is IHasGeneGroup hasGeneGroup)
		{
			hasGeneGroup.GeneGroup.Clear();
		}
		return item;
	}

	protected override void CreateDropItem(bool isRender, CountItem countItem)
	{
		Item item = DolocAPI.GenerateItem(countItem);
		if (itemBuffer.FirstItem is ItemCrop { HasGene: not false } itemCrop && item is ItemSeed itemSeed)
		{
			((IHasGeneGroup)itemSeed).SetGeneGroup(itemCrop.GeneGroup.Compress(itemCrop.GetDefaultGeneGroup()));
		}
		this.CreateDropItem(item, isRender, sendMessage: true);
	}
}
