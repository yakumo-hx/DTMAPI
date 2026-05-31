using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemSeed : Item, IHasGeneGroup, IHasSubscript
{
	private SeedInfo _seedProto;

	[JsonProperty]
	private bool isClonedSeed;

	[JsonProperty]
	private int incubationProgress;

	private bool enablePlantInInvalidSeason;

	private Vector2Int latestCellAnchor;

	public SeedInfo seedProto
	{
		get
		{
			if (_seedProto == null)
			{
				DolocAPI.QuerySeedProto(base.proto.Id, out _seedProto);
			}
			return _seedProto;
		}
	}

	public override string description => (IsCloned ? (DolocConfig.StaticTexts.ItemSeedCloned.Colored(DolocUiColor.SLIENTCOLOR_RED) + "\u00a0") : string.Empty) + base.description;

	public bool HasGene => !geneGroup.IsEmpty;

	public bool HasUnnaturalGenes => !geneGroup.IsSame(seedProto?.NatureGenes_Ref);

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

	[JsonProperty]
	public GeneGroup geneGroup { get; private set; }

	public bool IsCloned
	{
		get
		{
			if (isClonedSeed)
			{
				return HasUnnaturalGenes;
			}
			return false;
		}
	}

	public ItemSeed(ItemInfo proto, int count)
		: base(proto, count)
	{
		geneGroup = GetDefaultGeneGroup();
	}

	[JsonConstructor]
	protected ItemSeed(string itemName, int itemCount, GeneGroup geneGroup = null, bool isClonedSeed = false, int incubationProgress = 0)
		: base(itemName, itemCount)
	{
		this.geneGroup = geneGroup ?? GetDefaultGeneGroup();
		this.isClonedSeed = isClonedSeed && HasUnnaturalGenes;
		this.incubationProgress = incubationProgress;
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Use();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Use();
	}

	private void Use()
	{
		FlowerPot equipment2;
		if (TryGetSelectedEquipment(out PlantBasin equipment))
		{
			PlantSeed(equipment);
		}
		else if (TryGetSelectedEquipment(out equipment2))
		{
			PlantSeed(equipment2);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipPlantErr);
		}
	}

	public void PlantSeed(PlantBasin basin)
	{
		if (basin.IsPlanted)
		{
			return;
		}
		BodyController agent = DolocAPI.agent;
		if (!agent.IsCurrentStateSupportInteract || !agent.IsCurrentStateSupportUseItem)
		{
			return;
		}
		agent._Interact(delegate
		{
			Item item;
			if (basin.SeedTypeInfo.Id != seedProto.SeedType)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrInvalidPlantbasin, title));
			}
			else if (!enablePlantInInvalidSeason && !CheckCurrentSeasonValid())
			{
				enablePlantInInvalidSeason = true;
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrInvalidSeason, title));
			}
			else if (CostSelf(out item))
			{
				basin.Plant(this, shouldRender: true, sendMessage: true);
				enablePlantInInvalidSeason = false;
			}
		});
	}

	public void PlantSeed(FlowerPot pot)
	{
		BodyController agent = DolocAPI.agent;
		if (!agent.IsCurrentStateSupportInteract || !agent.IsCurrentStateSupportUseItem)
		{
			return;
		}
		agent._Interact(delegate
		{
			if (!pot.IsPlanted && CostSelf(out var _))
			{
				pot.Plant(Clone(1));
			}
		});
	}

	public bool CanSurviveInWeather(WeatherType weatherType, bool ignoreGene = false)
	{
		if (!weatherType.IsMalignantWeather())
		{
			return true;
		}
		switch (weatherType)
		{
		case WeatherType.ACID_RAIN:
			if (!ignoreGene)
			{
				return geneGroup.ContainsGene("anti_acid_rain");
			}
			return false;
		case WeatherType.SCORCH_SUN:
			if (!ignoreGene)
			{
				return geneGroup.ContainsGene("conifer_leaf");
			}
			return false;
		default:
			return true;
		}
	}

	public bool CheckSeasonValid(int month, bool isRoomIgnoreSeason, bool isRoomIgnoreSeasonFungus, bool ignoreGene = false)
	{
		if (isRoomIgnoreSeason)
		{
			return true;
		}
		if (seedProto.SeedType_Ref.UseRoomEffect && isRoomIgnoreSeasonFungus)
		{
			return true;
		}
		if (!ignoreGene && geneGroup.ContainsGene("robust_health"))
		{
			return true;
		}
		if (seedProto.GrowthMonths.Length != 0)
		{
			return seedProto.GrowthMonths.Contains(month);
		}
		return true;
	}

	public bool CheckCurrentSeasonValid(bool ignoreGene = false)
	{
		if (DolocAPI.CurrentRoom.RoomEffectInfo.IgnoreSeason)
		{
			return true;
		}
		if (seedProto.SeedType_Ref.UseRoomEffect && DolocAPI.CurrentRoom.RoomEffectInfo.IgnoreSeasonFungus)
		{
			return true;
		}
		if (!ignoreGene && geneGroup.ContainsGene("robust_health"))
		{
			return true;
		}
		if (seedProto.GrowthMonths.Length != 0)
		{
			return seedProto.GrowthMonths.Contains(DolocAPI.archiveHandle.timeData.dateNow.Month);
		}
		return true;
	}

	public GeneGroup GetDefaultGeneGroup()
	{
		return new GeneGroup(seedProto?.NatureGenes);
	}

	public bool Incubate(int threshold)
	{
		if (incubationProgress > threshold)
		{
			return false;
		}
		if (++incubationProgress == threshold)
		{
			geneGroup.RollGene(seedProto, clearExisting: true);
			incubationProgress++;
			return false;
		}
		return true;
	}

	public void ResetOverflowIncubationProgress(int threshold)
	{
		if (incubationProgress >= threshold)
		{
			incubationProgress = 0;
		}
	}

	public void ClearIncubationProgress(int threshold)
	{
		incubationProgress = 0;
	}

	public void CloneGeneGroup(GeneGroup geneGroup)
	{
		((IHasGeneGroup)this).SetGeneGroup(geneGroup);
		isClonedSeed = HasUnnaturalGenes;
	}

	public void ClearCloneMark()
	{
		isClonedSeed = false;
	}

	public override bool IsSame(Item other)
	{
		if (base.IsSame(other) && other is ItemSeed itemSeed && geneGroup.IsSame(itemSeed.geneGroup))
		{
			return IsCloned == itemSeed.IsCloned;
		}
		return false;
	}

	public override Item Clone(int count)
	{
		ItemSeed itemSeed = new ItemSeed(base.proto, count);
		((IHasGeneGroup)itemSeed).SetGeneGroup(geneGroup);
		itemSeed.isClonedSeed = isClonedSeed;
		itemSeed.incubationProgress = incubationProgress;
		return itemSeed;
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		if (seedProto != null && base.allowBuildEquipment)
		{
			ShowCellTip(new Vector2Int(0, 0), new Vector2Int(1, 1), flipWhenFaceLeft: true);
			enablePlantInInvalidSeason = false;
		}
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = CheckPlantBasin() || CheckFlowerPot();
		Vector2Int a = latestCellAnchor;
		latestCellAnchor = base.cellTip.CellAnchor;
		if (Vector2Int.Distance(a, latestCellAnchor) > 0f)
		{
			enablePlantInInvalidSeason = false;
		}
	}

	private bool CheckPlantBasin()
	{
		if (TryGetSelectedEquipment(out PlantBasin equipment) && !equipment.IsPlanted && equipment.SeedTypeInfo.Id == _seedProto.SeedType)
		{
			return CheckCurrentSeasonValid();
		}
		return false;
	}

	private bool CheckFlowerPot()
	{
		if (TryGetSelectedEquipment(out FlowerPot equipment))
		{
			return !equipment.IsPlanted;
		}
		return false;
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
