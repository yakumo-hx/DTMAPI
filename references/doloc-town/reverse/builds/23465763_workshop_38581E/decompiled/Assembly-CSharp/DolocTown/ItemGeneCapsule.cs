using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemGeneCapsule : Item, IHasGeneGroup, IHasSubscript
{
	public bool HasGene => !geneGroup.IsEmpty;

	public bool HasUnnaturalGenes => geneGroup.Genes.Length > 1;

	public bool IsCloned => false;

	public GeneGroup GeneGroup => geneGroup;

	private ItemFunctionGeneCapsule func => base.proto.Function as ItemFunctionGeneCapsule;

	public Sprite SubscriptSprite => DolocAPI.GlobalParameter.ItemSubscriptHasGene.GetByIndex(geneGroup.GeneCount - 1);

	[JsonProperty]
	public GeneGroup geneGroup { get; private set; }

	public ItemGeneCapsule(ItemInfo proto, int count)
		: base(proto, count)
	{
		geneGroup = GetDefaultGeneGroup();
	}

	[JsonConstructor]
	protected ItemGeneCapsule(string itemName, int itemCount, GeneGroup geneGroup)
		: base(itemName, itemCount)
	{
		this.geneGroup = geneGroup ?? GetDefaultGeneGroup();
	}

	public GeneGroup GetDefaultGeneGroup()
	{
		return new GeneGroup(func.Genes);
	}

	public override Item Clone(int count)
	{
		ItemGeneCapsule itemGeneCapsule = new ItemGeneCapsule(base.proto, count);
		((IHasGeneGroup)itemGeneCapsule).SetGeneGroup(geneGroup);
		return itemGeneCapsule;
	}

	public override bool IsSame(Item other)
	{
		if (base.IsSame(other) && other is ItemGeneCapsule itemGeneCapsule)
		{
			return geneGroup.IsSame(itemGeneCapsule.geneGroup);
		}
		return false;
	}

	public override string GetExtraInfo1()
	{
		return geneGroup?.Genes.GetDescription(null) ?? string.Empty;
	}

	protected override bool CanBuyback()
	{
		return !HasUnnaturalGenes;
	}

	public override Item CheckValid()
	{
		Item item = base.CheckValid();
		CropGeneInfo value;
		if (GeneGroup.GeneCount == 0)
		{
			item = DolocAPI.GenerateItem(DolocAPI.GlobalParameter.ItemRefGeneCapsuleEmpty, count);
		}
		else if (GeneGroup.GeneCount == 1 && DolocConfig.Tables.TbCropGene.DataMap.TryGetValue(GeneGroup.geneIds[0], out value))
		{
			item = DolocAPI.GenerateItem(value.CapsuleItem, count);
		}
		return item ?? this;
	}
}
