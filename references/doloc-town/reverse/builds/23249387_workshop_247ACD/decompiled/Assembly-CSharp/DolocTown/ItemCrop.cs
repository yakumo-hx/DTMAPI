using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemCrop : Item, IHasGeneGroup, IEatable, IHasSubscript
{
	private SeedInfo _seedProto;

	private ItemFunctionCrop func => base.proto.Function as ItemFunctionCrop;

	public SeedInfo seedProto => _seedProto ?? (_seedProto = DolocConfig.Tables.TbSeed.GetOrDefault(func.SeedItem ?? ""));

	public bool HasGene => !geneGroup.IsEmpty;

	public bool HasUnnaturalGenes => !geneGroup.IsSame(seedProto?.NatureGenes_Ref);

	public bool IsCloned
	{
		get
		{
			if (HasUnnaturalGenes)
			{
				return geneGroup.ContainsGenes(DolocAPI.GlobalParameter.ClonedCropGeneGroup_Ref);
			}
			return false;
		}
	}

	public GeneGroup GeneGroup => geneGroup;

	public Sprite SubscriptSprite
	{
		get
		{
			if (!IsCloned)
			{
				return DolocAPI.GlobalParameter.ItemSubscriptHasGene.GetByIndex(geneGroup.GeneCount - 1);
			}
			return DolocAPI.GlobalParameter.ItemSubscriptClone.GetByIndex(geneGroup.GeneCount - 1);
		}
	}

	public override string description => (IsCloned ? (DolocConfig.StaticTexts.ItemSeedCloned.Colored(DolocUiColor.SLIENTCOLOR_RED) + "\u00a0") : string.Empty) + base.description;

	public Item eatableItem => this;

	public EatingEffectInfo effectProto => func.EatingEffect_Ref;

	[JsonProperty]
	public GeneGroup geneGroup { get; private set; }

	public ItemCrop(ItemInfo proto, int count)
		: base(proto, count)
	{
		geneGroup = GetDefaultGeneGroup();
	}

	[JsonConstructor]
	protected ItemCrop(string itemName, int itemCount, GeneGroup geneGroup, bool isCloned)
		: base(itemName, itemCount)
	{
		this.geneGroup = geneGroup ?? GetDefaultGeneGroup();
	}

	public GeneGroup GetDefaultGeneGroup()
	{
		return new GeneGroup(seedProto?.NatureGenes);
	}

	public override bool IsSame(Item other)
	{
		if (base.IsSame(other) && other is ItemCrop itemCrop)
		{
			return geneGroup.IsSame(itemCrop.geneGroup);
		}
		return false;
	}

	public override Item Clone(int count)
	{
		ItemCrop itemCrop = new ItemCrop(base.proto, count);
		((IHasGeneGroup)itemCrop).SetGeneGroup(geneGroup);
		return itemCrop;
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		if (((IEatable)this).isValid)
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format(DolocConfig.StaticTexts.ItemConfirmUse, title), Use);
		}
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		((IEatable)this).Eat();
	}

	public override string GetExtraInfo1()
	{
		return geneGroup?.Genes.GetDescription(seedProto?.NatureGenes) ?? string.Empty;
	}

	protected override bool CanBuyback()
	{
		return !HasUnnaturalGenes;
	}
}
