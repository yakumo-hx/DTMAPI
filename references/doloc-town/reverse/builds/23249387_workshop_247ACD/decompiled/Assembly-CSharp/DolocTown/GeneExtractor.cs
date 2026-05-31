using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class GeneExtractor : CustomSynthesizer
{
	private EquipmentFuncGeneExtractor _func => proto.Function as EquipmentFuncGeneExtractor;

	public GeneExtractor(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public GeneExtractor(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, IRecipe latestRecipe, IRecipeGroup latestRecipeGroup, LinearInventory itemBuffer, Counter taskCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter, latestRecipe, latestRecipeGroup, itemBuffer, taskCounter)
	{
	}

	protected override void CreateDropItem(bool isRender, CountItem countItem)
	{
		Item item = null;
		if (itemBuffer.FirstItem is IHasGeneGroup { HasGene: not false } hasGeneGroup)
		{
			item = DolocAPI.GenerateItem(countItem);
			if (item is IHasGeneGroup hasGeneGroup2)
			{
				hasGeneGroup2.SetGeneGroup(hasGeneGroup.GeneGroup);
			}
		}
		this.CreateDropItem(item?.CheckValid(), isRender, sendMessage: true);
	}

	public override bool ContentFilter(Item content)
	{
		if (!base.ContentFilter(content))
		{
			return false;
		}
		if (!(content is ItemSeed { IsCloned: false, HasGene: not false }))
		{
			return false;
		}
		return true;
	}

	protected override Item HandlePreviewItem(Item item)
	{
		if (itemBuffer.FirstItem is IHasGeneGroup { HasGene: not false } hasGeneGroup)
		{
			Item item2 = item.Clone(1);
			if (item2 is IHasGeneGroup hasGeneGroup2)
			{
				hasGeneGroup2.SetGeneGroup(hasGeneGroup.GeneGroup);
				return item2.CheckValid();
			}
		}
		return item;
	}
}
