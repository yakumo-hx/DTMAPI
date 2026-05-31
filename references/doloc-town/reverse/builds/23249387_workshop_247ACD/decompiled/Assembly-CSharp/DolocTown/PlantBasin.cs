using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.Config.TechTree;
using DolocTown.Config.Weather;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class PlantBasin : Equipment, IAffectorReceiver, IAnimalMoodAffector, IFeeder, IAnimalInteractable, IHasCropInfo
{
	[JsonProperty]
	[DebugInfo("作物")]
	protected Crop crop;

	[JsonProperty]
	[DebugInfo("补给槽")]
	protected PlantBasinSupply supply;

	[JsonProperty]
	private readonly Counter updateCounter;

	private readonly Counter waterReminder = new Counter(2);

	public readonly EquipmentFuncPlantBasinBase func;

	protected SingleSpriteRender fertilizerRenderer;

	protected SingleSpriteRender protectorRenderer;

	protected ContinuesAnimationEffectsSlot cropShinyRenderer = new ContinuesAnimationEffectsSlot(ContinuesAnimEffectType.CROP_SHINE);

	private int __animal_counter;

	private bool CouldHarvest
	{
		get
		{
			if (crop == null)
			{
				return false;
			}
			if (!crop.isMature)
			{
				return crop.isDead;
			}
			return true;
		}
	}

	[JsonProperty]
	public ItemSeed currentSeed { get; private set; }

	public Crop Crop => crop;

	public bool IsPlanted => crop != null;

	public bool IsNotPlanted => crop == null;

	public PlantBasinSupply Supply => supply;

	public virtual SeedTypeInfo SeedTypeInfo => func.SeedType_Ref;

	public bool IsProtected => supply.IsProtected;

	public bool IsFertilizerd => supply.IsFertilized;

	public Vector3 PositionCrop => new Vector3(base.Position.x + func.CenterOffset.x * 0.125f, base.HeightLevel.z + func.CenterOffset.y * 0.125f, base.PositionCenter.z - proto.WorldSpriteSize.y * 0.499f);

	public Vector3 PositionFertilizer
	{
		get
		{
			Vector3 positionCrop = PositionCrop;
			return new Vector3(positionCrop.x, positionCrop.y, positionCrop.z + 0.125f);
		}
	}

	public Vector3 PositionCropFertilized
	{
		get
		{
			Vector3 positionCrop = PositionCrop;
			float num = 0.5f;
			positionCrop.y += num;
			positionCrop.z -= num;
			return positionCrop;
		}
	}

	public Vector3 PositionProtector
	{
		get
		{
			Vector3 vector = new Vector3(0f, 0f, -0.0001f);
			return PositionCrop + vector;
		}
	}

	public float GrowthAddition
	{
		get
		{
			float num = base.Host.CurrentRoom.GrowthAddition + (supply.GetLighting() ? DolocAPI.GlobalParameter.SunGrowthAddition : 0f);
			if (crop == null || !crop.seedProto.SeedType_Ref.UseRoomEffect)
			{
				return num;
			}
			return num + base.Host.CurrentRoom.GrowthAdditionFungus;
		}
	}

	public int MoodContribution => crop?.MoodContribution ?? 0;

	public Vector2Int AnimalInteractablePosition => base.Anchor;

	public Vector2 AnimalInteractablePositionWS => base.PositionBottom;

	public int AnimalInteractableWidth => proto.CoverSize.x;

	public bool AnimalInteractableIsValid => base.index >= 0;

	public bool IsAnimalInteractableLocked { get; set; }

	public int AnimalCounter
	{
		get
		{
			return __animal_counter;
		}
		set
		{
			__animal_counter = Mathf.Max(0, value);
		}
	}

	public int FeederPriority => 2;

	private string FeedsName
	{
		get
		{
			if (crop == null)
			{
				return string.Empty;
			}
			return crop.seedProto?.Id.ToLower() ?? string.Empty;
		}
	}

	public bool IsFeederEmpty
	{
		get
		{
			int energy;
			return !IsCropEatable(out energy);
		}
	}

	public bool HasCrop
	{
		get
		{
			Crop crop = this.crop;
			if (crop != null)
			{
				return !crop.isDead;
			}
			return false;
		}
	}

	public bool IsCropMature => crop?.isMature ?? false;

	public bool NeedWaterOrClear
	{
		get
		{
			if (crop == null)
			{
				return false;
			}
			if (crop.TryGetNeedWaterOrClear(out var value))
			{
				return value;
			}
			if (!crop.isDead)
			{
				if (!supply.IsMoist)
				{
					return !crop.isMature;
				}
				return false;
			}
			return true;
		}
	}

	public string CropTitle
	{
		get
		{
			if (!HasCrop)
			{
				return "";
			}
			return DolocAPI.GetItemTitle(crop.seedProto.CropOutputs.First().itemName);
		}
	}

	private string prefix
	{
		get
		{
			ItemSeed itemSeed = currentSeed;
			if (itemSeed == null || !itemSeed.IsCloned)
			{
				return string.Empty;
			}
			return DolocConfig.StaticTexts.ItemSeedCloned.Colored(DolocUiColor.SLIENTCOLOR_RED) + "\u00a0";
		}
	}

	public string GrowthLevelInfo
	{
		get
		{
			if (!HasCrop)
			{
				return "";
			}
			return prefix + DolocUtils.Format(DolocConfig.StaticTexts.UiCropInfoGrowthLevel, $"{crop.CurrentLevel}/{crop.seedProto.MatureLevel}");
		}
	}

	public string HarvestCountInfo
	{
		get
		{
			if (!HasCrop)
			{
				return "";
			}
			if (crop.MaxLifespan <= 1)
			{
				return "";
			}
			return DolocUtils.Format(DolocConfig.StaticTexts.UiCropInfoHarvestCount, $"{crop.data.lifespan}/{crop.MaxLifespan}");
		}
	}

	public string GeneInfo
	{
		get
		{
			if (!HasCrop || !crop.IsGeneCrop)
			{
				return "";
			}
			return crop.GetDescription();
		}
	}

	public float GrowthProgress
	{
		get
		{
			if (!HasCrop)
			{
				return 0f;
			}
			return crop.TotalGrowthProgress;
		}
	}

	public float HealthProgress
	{
		get
		{
			if (!HasCrop)
			{
				return 0f;
			}
			return crop.HealthProgress;
		}
	}

	public float WaterProgress
	{
		get
		{
			if (!HasCrop)
			{
				return 0f;
			}
			return Supply.WaterRatio;
		}
	}

	public float FilmProgress
	{
		get
		{
			if (!HasCrop)
			{
				return 0f;
			}
			return Supply.ProtectedPercent;
		}
	}

	public float FertilizerProgress
	{
		get
		{
			if (HasCrop)
			{
				ItemInfo fertilizerItemInfo = supply.FertilizerData.fertilizerItemInfo;
				if (fertilizerItemInfo != null && fertilizerItemInfo.Function is ItemFunctionFertilizer itemFunctionFertilizer)
				{
					return (float)supply.FertilizerData.duration / (float)itemFunctionFertilizer.Duration;
				}
			}
			return 0f;
		}
	}

	public bool IsLighting
	{
		get
		{
			if (HasCrop)
			{
				return supply.IsLighting;
			}
			return false;
		}
	}

	public bool Plant(ItemSeed seed, bool shouldRender = false, bool sendMessage = false, bool playSoundEvent = true)
	{
		if (IsRemoved)
		{
			return false;
		}
		if (seed == null || crop != null || seed.seedProto.SeedType != SeedTypeInfo.Id)
		{
			return false;
		}
		seed = (ItemSeed)seed.Clone(1);
		crop = new Crop(seed.seedProto, this, seed.geneGroup.Genes);
		if (shouldRender)
		{
			RenderCrop();
			if (playSoundEvent)
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_PLANT);
			}
		}
		if (sendMessage)
		{
			DolocAPI.BroadcastString(GameEventType.PLANT_CROP, crop.seedProto.Id);
			SendUseEquipmentMessage();
		}
		currentSeed = seed;
		return true;
	}

	public void Harvest(bool putInBackpack = false, bool sendMessage = true)
	{
		if (crop == null)
		{
			return;
		}
		if (crop.isDead)
		{
			if (base.IsRender)
			{
				InstantParticleEffectsType type = ((crop.CurrentLevel != 0) ? InstantParticleEffectsType.LEAVES : InstantParticleEffectsType.LEAVES_AND_SOILS);
				DolocAPI.RaiseInstantPSEffects(base.Position, type);
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_SHOVEL);
			}
			DolocAPI.BroadcastString(GameEventType.CLEAR_CROP, crop.seedProto.Id);
			crop.AfterClearWither(base.IsRender);
			AfterHarvest(isDead: true);
		}
		else if (crop.isMature)
		{
			if (base.IsRender)
			{
				DolocAPI.RaiseInstantPSEffects(base.Position, InstantParticleEffectsType.LEAVES);
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_HARVEST);
			}
			if (sendMessage)
			{
				DolocAPI.BroadcastString(GameEventType.HARVEST_CROP, crop.seedProto.Id);
				DolocAPI.AddTechExp(TechPointType.NATURE, crop.seedProto.TechPoint);
			}
			crop.GenCropOutput(putInBackpack);
			crop.AfterHarvest(base.IsRender);
			AfterHarvest();
		}
	}

	public void HarvestTarget(Crop targetCrop, bool putInBackpack = false, bool sendMessage = true)
	{
		if (crop == targetCrop)
		{
			Harvest(putInBackpack, sendMessage);
		}
	}

	protected virtual void AfterHarvest(bool isDead = false)
	{
		if (isDead || !crop.Regrow(base.IsRender))
		{
			ClearCrop();
		}
	}

	public virtual void Water(int value, bool shouldRender = false, bool sendMessage = true, bool invokeGeneCallback = true)
	{
		if (sendMessage && supply.WaterRatio < 0.6f)
		{
			DolocAPI.AddTechExp(TechPointType.NATURE, 1);
			DolocAPI.BroadcastString(GameEventType.WATER_CROP, crop?.seedProto.Id);
		}
		supply.Water(value);
		crop?.Water(shouldRender, invokeGeneCallback);
		if (shouldRender)
		{
			UpdateBasinWetSprite();
			DolocAPI.RaiseInstantAnimEffects(base.PositionTop, InstAnimEffectType.WATER_LARGE);
		}
	}

	public virtual void Water(bool shouldRender = false, bool sendMessage = true, bool invokeGeneCallback = true)
	{
		if (sendMessage && supply.WaterRatio < 0.6f)
		{
			DolocAPI.AddTechExp(TechPointType.NATURE, 1);
			DolocAPI.BroadcastString(GameEventType.WATER_CROP, crop?.seedProto.Id);
		}
		supply.Water();
		crop?.Water(shouldRender, invokeGeneCallback);
		if (shouldRender)
		{
			UpdateBasinWetSprite();
			DolocAPI.RaiseInstantAnimEffects(base.PositionTop, InstAnimEffectType.WATER_LARGE);
		}
	}

	public bool TryCostSupplyWater(int require)
	{
		return supply.GetMoist(require);
	}

	public void ClearSupplyWater(bool shouldRender = false)
	{
		supply.ClearWater();
		UpdateBasinWetSprite();
		if (shouldRender)
		{
			ShowEvaporationEffect();
		}
	}

	public virtual void Light(bool shouldRender = false)
	{
		supply.Light();
		if (shouldRender)
		{
			UpdateCropShinyRenderer();
		}
	}

	public bool CanFertilizer(ItemFertilizer fertilizer)
	{
		ItemFunctionFertilizer itemFunctionFertilizer = fertilizer?.func;
		if (itemFunctionFertilizer == null || itemFunctionFertilizer.IsTree)
		{
			return false;
		}
		return CanFertilizer();
	}

	public bool CanFertilizer()
	{
		return !supply.IsFertilized;
	}

	public bool Fertilizer(ItemFertilizer fertilizer, bool shouldRender = false, bool sendMessage = false, bool playSoundEvent = true)
	{
		ItemFunctionFertilizer itemFunctionFertilizer = fertilizer?.func;
		if (itemFunctionFertilizer == null || itemFunctionFertilizer.IsTree)
		{
			return false;
		}
		return Fertilizer(fertilizer.func.Duration, fertilizer.func.Addition, fertilizer.proto, shouldRender, sendMessage, playSoundEvent);
	}

	public virtual bool Fertilizer(int duration, float addition, ItemInfo fertilizerItem, bool shouldRender = false, bool sendMessage = false, bool playSoundEvent = true)
	{
		if (!CanFertilizer())
		{
			return false;
		}
		if (shouldRender)
		{
			supply.Fertilizer(duration, addition, fertilizerItem);
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.LEAVES_AND_SOILS);
			RenderFertilizer();
			UpdateCropPosition();
			if (playSoundEvent)
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_FERTILIZE);
			}
		}
		else
		{
			supply.Fertilizer(duration, addition, fertilizerItem);
		}
		if (sendMessage)
		{
			DolocAPI.AddTechExp(TechPointType.NATURE, 1);
			if (crop != null)
			{
				DolocAPI.BroadcastString(GameEventType.FERTILIZE_CROP, crop.seedProto.Id);
			}
		}
		return true;
	}

	public bool Protect(ItemFilm itemFilm, bool isRender)
	{
		if (supply.IsProtectedFull)
		{
			return false;
		}
		supply.Protect(itemFilm);
		crop?.Protected(isRender);
		if (isRender)
		{
			RenderProtector();
		}
		return true;
	}

	private void WaterWithBottleOfWater(Item item)
	{
		DolocAPI.agent._Interact(delegate
		{
			item.CostSelf();
			DolocAPI.RaiseSpriteFadeUp(PositionTip, DolocAPI.GlobalParameter.ItemRefBottleOfWater);
			DolocAPI.GenerateDropItems(base.CurrentRoom, DolocAPI.GlobalParameter.ItemRefWastePlasticBottle, base.PositionCenter);
			Water(shouldRender: true);
		});
	}

	protected void GenerateCropOutputOnRemoved(bool putInBackpack)
	{
		if (crop != null && !crop.isDead)
		{
			if (crop.CurrentLevel == 0)
			{
				this.PlaceItemInBagOrCreateDropItem(currentSeed, putInBackpack, sendMessage: false);
			}
			else if (crop.isMature)
			{
				crop.GenCropOutput(putInBackpack);
			}
			else
			{
				this.PlaceItemInBagOrCreateDropItem(DolocAPI.GlobalParameter.ItemRefWeeds, putInBackpack, sendMessage: true);
			}
		}
	}

	public PlantBasin(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncPlantBasinBase)proto.Function;
		supply = new PlantBasinSupply(func.SupplyCapacity);
		updateCounter = DolocAPI.GlobalParameter.NewTuCounter;
		currentSeed = null;
	}

	[JsonConstructor]
	protected PlantBasin(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Crop crop, PlantBasinSupply supply, Counter updateCounter = null, ItemSeed currentSeed = null)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		this.updateCounter = updateCounter ?? DolocAPI.GlobalParameter.NewTuCounter;
		this.updateCounter.ValidateInterval(DolocAPI.GlobalParameter.TULength);
		this.crop = ((crop != null && crop.IsValid) ? crop : null);
		this.supply = supply;
		func = proto.Function as EquipmentFuncPlantBasinBase;
		this.currentSeed = currentSeed ?? crop?.RebuildSeed() ?? null;
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		crop?.AfterLoadData(this);
		ValidateSeedItem();
	}

	private void ValidateSeedItem()
	{
		if (crop == null)
		{
			currentSeed = null;
		}
		else if (currentSeed == null)
		{
			currentSeed = crop.RebuildSeed();
		}
	}

	private void RecoverySpirit()
	{
		if (crop != null && crop.CheckOxygen(base.CurrentRoom.AgentPositionCell, out var value))
		{
			DolocAPI.archiveHandle.farmData.agentData.AddSpirit(value);
			DolocAPI.RaiseUiEffects(value, DolocUiColor.AGENT_SPIRIT);
		}
	}

	protected sealed override void Update()
	{
		RecoverySpirit();
		if (!updateCounter.Tick() || (crop != null && !crop.CheckGrowthMonth(shouldRender: true)))
		{
			return;
		}
		if (base.Host.CurrentWeatherInfo.IsRainy)
		{
			if (crop != null)
			{
				if (crop.IsPolluted)
				{
					crop.Water(shouldRender: true, invokeCallback: true);
				}
				crop.UpdateNormal(shouldRender: true, isMoist: true, GrowthAddition, supply.GetFertilizerGrowthAddition());
				UpdateCropPosition();
			}
			supply.Water();
			UpdateBasinWetSprite();
			UpdateCropShinyRenderer();
			UpdateFertilizerRenderer();
			UpdateCropPosition();
			return;
		}
		if (crop != null)
		{
			crop.UpdateNormal(shouldRender: true, supply.GetMoist(), GrowthAddition, supply.GetFertilizerGrowthAddition());
			if (RandomUtils.Dice(0.1f))
			{
				crop.Swing();
			}
			UpdateCropPosition();
		}
		UpdateFertilizerRenderer();
		UpdateBasinWetSprite();
		UpdateCropPosition();
	}

	protected sealed override void UpdateNoRender()
	{
		if (!updateCounter.Tick() || (crop != null && !crop.CheckGrowthMonth(shouldRender: false)))
		{
			return;
		}
		if (base.Host.CurrentWeatherInfo.IsRainy)
		{
			supply.Water();
			if (crop != null)
			{
				crop.Water(shouldRender: false, invokeCallback: true);
				crop.UpdateNormal(shouldRender: false, isMoist: true, GrowthAddition, supply.GetFertilizerGrowthAddition());
			}
		}
		else
		{
			crop?.UpdateNormal(shouldRender: false, supply.GetMoist(), GrowthAddition, supply.GetFertilizerGrowthAddition());
		}
	}

	public void UpdateAcidRain()
	{
		if (updateCounter.Tick() && (crop == null || crop.CheckGrowthMonth(shouldRender: true)))
		{
			if (supply.GetProtected())
			{
				crop?.UpdateAcidRain(shouldRender: true, isMoist: true, GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: true, 0f);
				UpdateProtectorRenderer();
				supply.Water();
			}
			else
			{
				supply.CostSW();
				crop?.UpdateAcidRain(shouldRender: true, isMoist: false, GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: false, DolocAPI.GlobalParameter.AcidRainDamage);
			}
			UpdateBasinWetSprite();
			UpdateCropShinyRenderer();
			UpdateFertilizerRenderer();
			UpdateCropPosition();
		}
	}

	public void UpdateAcidRainNoRender()
	{
		if (updateCounter.Tick() && (crop == null || crop.CheckGrowthMonth(shouldRender: false)))
		{
			if (supply.GetProtected())
			{
				crop?.UpdateAcidRain(shouldRender: false, isMoist: true, GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: true, 0f);
				supply.Water();
			}
			else
			{
				supply.CostSW();
				crop?.UpdateAcidRain(shouldRender: false, isMoist: false, GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: false, DolocAPI.GlobalParameter.AcidRainDamage);
			}
		}
	}

	public void UpdateScorchSun()
	{
		if (updateCounter.Tick() && (crop == null || crop.CheckGrowthMonth(shouldRender: true)))
		{
			if (supply.GetProtected())
			{
				crop?.UpdateScorchSun(shouldRender: true, supply.GetMoist(), GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: true, 0f);
				UpdateProtectorRenderer();
			}
			else
			{
				supply.CostSW();
				crop?.UpdateScorchSun(shouldRender: true, isMoist: false, GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: false, DolocAPI.GlobalParameter.ScorchSunDamage);
			}
		}
	}

	public void UpdateScorchSunNoRender()
	{
		if (updateCounter.Tick() && (crop == null || crop.CheckGrowthMonth(shouldRender: false)))
		{
			if (supply.GetProtected())
			{
				crop?.UpdateScorchSun(shouldRender: false, supply.GetMoist(), GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: true, 0f);
				return;
			}
			supply.CostSW();
			crop?.UpdateScorchSun(shouldRender: false, isMoist: false, GrowthAddition, supply.GetFertilizerGrowthAddition(), isProtected: false, DolocAPI.GlobalParameter.ScorchSunDamage);
		}
	}

	protected override void OnTouch()
	{
		if (crop != null)
		{
			crop.Swing();
			if (crop.isDead)
			{
				ShowTip(DolocConfig.StaticTexts.UiOperationClear);
			}
			else if (crop.isMature)
			{
				ShowTip(DolocConfig.StaticTexts.UiOperationHarvest);
			}
		}
	}

	protected override void OnDisTouch()
	{
		this.HideSceneOperationTip();
		crop?.Swing();
		DolocAPI.HideSceneBox();
	}

	protected override void OnInteract()
	{
		if (crop == null)
		{
			return;
		}
		if (CouldHarvest)
		{
			PushTipToDisappear();
			DolocAPI.agent._Interact(delegate
			{
				Harvest();
			});
			return;
		}
		PushTip();
		if (!HandleItemWithCrop())
		{
			DolocAPI.uiSystem.sceneBoxGroup.RenderAndShow(base.Renderer.transform.position.x, this, DolocAPI.GlobalParameter.UiSceneInfoTipDuration);
		}
	}

	protected override void OnRender()
	{
		waterReminder.Reset();
		UpdateProtectorRenderer();
		UpdateBasinWetSprite();
		UpdateFertilizerRenderer();
		UpdateCropShinyRenderer();
		RenderCrop();
	}

	protected override void OnUnRender()
	{
		crop?.OnUnRender();
		base.Host.RecycleCropRenderer(crop?.Renderer);
		ClearFertilizerRenderer();
		DolocAPI.EntitySystem.Recycle(protectorRenderer);
		protectorRenderer = null;
		cropShinyRenderer.Recycle();
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		ClearCrop();
	}

	protected override void OnManualWater()
	{
		if (base.Host.CurrentWeatherInfo.Id == WeatherType.SCORCH_SUN && !supply.IsProtected)
		{
			ShowEvaporationEffect();
			if (waterReminder.Tick())
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrWaterEvaporated);
			}
		}
		else
		{
			Water(shouldRender: true);
		}
	}

	public override void OnThunder(bool shouldRender)
	{
		crop?.Thunder(shouldRender);
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		GenerateCropOutputOnRemoved(putInBackpack);
	}

	private bool HandleItemWithCrop()
	{
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem == null)
		{
			return false;
		}
		if (selectedItem.name == DolocAPI.GlobalParameter.ItemRefBottleOfWater)
		{
			WaterWithBottleOfWater(selectedItem);
			return true;
		}
		return false;
	}

	private void RenderCrop()
	{
		if (crop != null)
		{
			if (crop.Renderer == null)
			{
				base.Host.RenderCrop(crop, (!crop.isDead && supply.IsFertilized) ? PositionCropFertilized : PositionCrop);
			}
			if (crop.isDead)
			{
				crop.Renderer.SetMoist(isMoist: false);
				return;
			}
			crop.Renderer.SetMoist(crop.isMoist, crop.IsPolluted || (base.Host.CurrentWeatherInfo.Id == WeatherType.ACID_RAIN && !supply.IsProtected));
			crop.Renderer.UpdateMatureRenderer();
			crop.OnRender();
		}
	}

	private void UpdateCropPosition()
	{
		if (crop != null && !(crop.Renderer == null))
		{
			crop.Renderer.position = ((!crop.isDead && supply.IsFertilized) ? PositionCropFertilized : PositionCrop);
		}
	}

	private void RenderFertilizer()
	{
		if (fertilizerRenderer == null)
		{
			fertilizerRenderer = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
			if (fertilizerRenderer == null)
			{
				return;
			}
		}
		fertilizerRenderer.sprite = supply.FertilizerSprite;
		fertilizerRenderer.position = PositionFertilizer;
		fertilizerRenderer.SetVisible(value: true);
	}

	private void ClearFertilizerRenderer()
	{
		if (!(fertilizerRenderer == null))
		{
			DolocAPI.EntitySystem.Recycle(fertilizerRenderer);
			fertilizerRenderer = null;
		}
	}

	protected virtual void UpdateFertilizerRenderer()
	{
		if (!supply.IsFertilized)
		{
			ClearFertilizerRenderer();
		}
		else
		{
			RenderFertilizer();
		}
	}

	private void RenderProtector()
	{
		if (protectorRenderer == null)
		{
			protectorRenderer = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
			if (protectorRenderer == null)
			{
				return;
			}
		}
		protectorRenderer.sprite = supply.ProtectorSprite;
		protectorRenderer.position = PositionProtector;
		protectorRenderer.SetVisible(value: true);
	}

	private void ClearProtectorRenderer()
	{
		if (!(protectorRenderer == null))
		{
			DolocAPI.EntitySystem.Recycle(protectorRenderer);
			protectorRenderer = null;
		}
	}

	protected virtual void UpdateProtectorRenderer()
	{
		if (supply.IsProtected)
		{
			RenderProtector();
		}
		else
		{
			ClearProtectorRenderer();
		}
	}

	protected virtual void UpdateCropShinyRenderer()
	{
		if (supply.IsLighting)
		{
			ContinuesAnimationEffects effects = cropShinyRenderer.Effects;
			if (effects != null)
			{
				effects.position2d = PositionCrop;
				effects.material = LocMaterials.GAME_MAT_EFFECTS_SHINE;
				effects.SetVisible(value: true);
				cropShinyRenderer.PlayRandom();
			}
		}
		else
		{
			cropShinyRenderer.Recycle();
		}
	}

	protected void UpdateBasinWetSprite()
	{
		if (!(base.Renderer == null))
		{
			if (supply.IsMoist)
			{
				base.Renderer.Sprite = ((func.SpriteWetAsset.Asset != null) ? func.SpriteWetAsset.Asset : proto.Sprite);
			}
			else
			{
				base.Renderer.Sprite = proto.Sprite;
			}
		}
	}

	public void ClearCrop()
	{
		if (crop != null)
		{
			crop.OnUnRender();
			base.Host.RecycleCropRenderer(crop.Renderer);
			crop = null;
		}
	}

	void IAffectorReceiver.Affect(IAffector affector)
	{
		switch (affector.AffectType)
		{
		case AffectorType.Sprinkler:
			Water(shouldRender: true);
			break;
		case AffectorType.Light:
			Light(shouldRender: true);
			break;
		case AffectorType.Sound:
			break;
		}
	}

	void IAffectorReceiver.AffectNoRender(IAffector affector)
	{
		switch (affector.AffectType)
		{
		case AffectorType.Sprinkler:
			Water();
			break;
		case AffectorType.Light:
			Light();
			break;
		case AffectorType.Sound:
			break;
		}
	}

	private bool IsCropEatable(out int energy)
	{
		energy = 0;
		if (crop == null || crop.isDead)
		{
			return false;
		}
		if (crop.CurrentLevel < 2)
		{
			return false;
		}
		return DolocConfig.Tables.TbHusbandryEnergy.IsHusbandryFeeds(FeedsName, out energy);
	}

	public int TakeFeeds(int require, out string name)
	{
		name = FeedsName;
		if (!IsCropEatable(out var energy))
		{
			return 0;
		}
		crop.OriginGrowBack(shouldRender: false, shouldClearGrowth: false);
		UniTask.Delay(300).ContinueWith(delegate
		{
			if (crop != null)
			{
				crop.UpdateRenderer();
				if (base.IsRender)
				{
					DolocAPI.RaiseInstantPSEffects(base.PositionTop, InstantParticleEffectsType.LEAVES_AND_SOILS);
				}
			}
		}).Forget();
		return energy;
	}
}
