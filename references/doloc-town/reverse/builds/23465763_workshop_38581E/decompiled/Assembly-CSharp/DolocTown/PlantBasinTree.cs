using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class PlantBasinTree : Equipment, IHasCropInfo
{
	[JsonProperty]
	public Counter updateCounter;

	private SingleSpriteRender fertilizerRenderer;

	[JsonProperty]
	[DebugInfo]
	public TreeCrop Crop { get; private set; }

	private EquipmentFuncPlantBasinTree func => proto.Function as EquipmentFuncPlantBasinTree;

	public override bool IsOccupy
	{
		get
		{
			TreeCrop crop = Crop;
			if (crop != null)
			{
				return crop.CurrentLevel > 0;
			}
			return false;
		}
	}

	public Vector3 PositionTreeCrop => new Vector3(base.Position.x + func.FertilizerOffset * 0.125f, base.HeightLevel.z - 0.5f, base.PositionCenter.z - proto.WorldSpriteSize.y * 0.499f);

	public Vector3 PositionFertilizer
	{
		get
		{
			Vector3 positionTreeCrop = PositionTreeCrop;
			positionTreeCrop.z -= 0.01f;
			return positionTreeCrop;
		}
	}

	public bool HasCrop => Crop != null;

	public string CropTitle => Crop?.protoTree.Title ?? "";

	public string GrowthLevelInfo
	{
		get
		{
			if (!HasCrop)
			{
				return "";
			}
			return DolocUtils.Format(DolocConfig.StaticTexts.UiCropInfoGrowthLevel, $"{Crop.CurrentLevel}/{Crop.protoTree.MaxLevel}");
		}
	}

	public string HarvestCountInfo => "";

	public string GeneInfo => "";

	public float GrowthProgress
	{
		get
		{
			if (!HasCrop)
			{
				return 0f;
			}
			return Crop.GrowthProgress;
		}
	}

	public float HealthProgress => -1f;

	public float WaterProgress => -1f;

	public float FilmProgress => -1f;

	public float FertilizerProgress
	{
		get
		{
			if (HasCrop)
			{
				ItemInfo fertilizerItemInfo = Crop.fertilizerData.fertilizerItemInfo;
				if (fertilizerItemInfo != null && fertilizerItemInfo.Function is ItemFunctionFertilizer itemFunctionFertilizer)
				{
					return (float)Crop.fertilizerData.duration / (float)itemFunctionFertilizer.Duration;
				}
			}
			return 0f;
		}
	}

	public bool IsLighting => false;

	public override string GetOccupyInfo()
	{
		return DolocConfig.StaticTexts.FarmbuilderErrPlantbasinTreeOccupied;
	}

	public PlantBasinTree(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		updateCounter = DolocAPI.GlobalParameter.NewTuCounter;
	}

	[JsonConstructor]
	public PlantBasinTree(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, TreeCrop Crop, Counter updateCounter = null)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		this.Crop = Crop;
		this.updateCounter = updateCounter ?? DolocAPI.GlobalParameter.NewTuCounter;
		this.updateCounter.ValidateInterval(DolocAPI.GlobalParameter.TULength);
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		if (Crop != null)
		{
			if (Crop.Validate())
			{
				Crop.Basin = this;
				return;
			}
			Debug.LogError("无效的树作物，已删除");
			Crop = null;
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (Crop != null)
		{
			DolocAPI.uiSystem.sceneBoxGroup.RenderAndShow(base.Renderer.transform.position.x, this, DolocAPI.GlobalParameter.UiSceneInfoTipDuration);
		}
	}

	public bool TryPlantCrop()
	{
		if (Crop != null)
		{
			return false;
		}
		Item item = DolocAPI.SelectedItem;
		if (item == null)
		{
			return false;
		}
		ItemSeedTree itemSeedTree = item as ItemSeedTree;
		if (itemSeedTree != null && itemSeedTree.TreeSeedProto != null)
		{
			DolocAPI.RaiseSpriteFadeUp(base.PositionCenter, item.uiSprite);
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_PLANT);
			DolocAPI.agent._Interact(delegate
			{
				if (item.CostSelf(out var _, showFadeUpIcon: false))
				{
					Crop = new TreeCrop(this, itemSeedTree.TreeSeedProto);
					base.Renderer.RenderTreeCrop(Crop, PositionTreeCrop);
					DolocAPI.BroadcastString(GameEventType.PLANT_TREE, proto.Id);
				}
			});
			return true;
		}
		DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.UiOperationErrNotSeed, Title));
		return false;
	}

	protected override void Update()
	{
		if (updateCounter.Tick())
		{
			Crop?.Update(base.Host.CurrentWeatherInfo, isRender: true);
		}
	}

	protected override void UpdateNoRender()
	{
		if (updateCounter.Tick())
		{
			Crop?.Update(base.Host.CurrentWeatherInfo, isRender: false);
		}
	}

	protected override void OnRender()
	{
		if (Crop != null)
		{
			base.Renderer.RenderTreeCrop(Crop, PositionTreeCrop);
			UpdateFertilizerRenderer();
		}
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveRenderComponent<TreeCropRenderer>();
		ClearFertilizerRenderer();
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		TreeCrop crop = Crop;
		if (crop != null && crop.CurrentLevel == 0)
		{
			this.PlaceItemInBagOrCreateDropItem(Crop.protoTree.Id, putInBackpack, sendMessage: false);
		}
	}

	public void ClearCrop()
	{
		if (Crop != null)
		{
			base.Renderer.RemoveRenderComponent<TreeCropRenderer>();
			ClearFertilizerRenderer();
			Crop = null;
		}
	}

	public void GenerateCropOutput(ItemSpawnEntry entry, bool useBuff)
	{
		if (Crop != null)
		{
			((IDropItemHost)base.Host)?.GenerateDropItemsWithBuff(DungeonResourceType.TREE, entry, base.Renderer.position2d, shouldSendMsg: true, useBuff);
		}
	}

	public bool CanFertilizer(ItemFertilizer fertilizer)
	{
		ItemFunctionFertilizer itemFunctionFertilizer = fertilizer?.func;
		if (itemFunctionFertilizer == null || !itemFunctionFertilizer.IsTree)
		{
			return false;
		}
		return CanFertilizer();
	}

	public bool CanFertilizer()
	{
		TreeCrop crop = Crop;
		if (crop != null)
		{
			return !crop.IsFertilizered;
		}
		return false;
	}

	public bool Fertilizer(ItemFertilizer fertilizer, bool shouldRender = false, bool shouldSendMessage = false, bool playSoundEvent = true)
	{
		ItemFunctionFertilizer itemFunctionFertilizer = fertilizer?.func;
		if (itemFunctionFertilizer == null || !itemFunctionFertilizer.IsTree)
		{
			return false;
		}
		return Fertilizer(fertilizer.proto, fertilizer.func.Duration, fertilizer.func.Addition, shouldRender, shouldSendMessage, playSoundEvent);
	}

	public bool Fertilizer(ItemInfo fertilizerItem, int duration, float addition, bool shouldRender = false, bool shouldSendMessage = false, bool playSoundEvent = true)
	{
		if (Crop == null || Crop.IsFertilizered)
		{
			return false;
		}
		Crop.Fertilizer(duration, addition, fertilizerItem);
		if (shouldRender)
		{
			RenderFertilizer();
			DolocAPI.RaiseInstantPSEffects(base.PositionTop, InstantParticleEffectsType.LEAVES_AND_SOILS);
			if (playSoundEvent)
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_FERTILIZE);
			}
		}
		return true;
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
		fertilizerRenderer.position = PositionFertilizer;
		fertilizerRenderer.sprite = Crop.fertilizerData.FertilizerSprite;
	}

	private void ClearFertilizerRenderer()
	{
		if (!(fertilizerRenderer == null))
		{
			DolocAPI.EntitySystem.Recycle(fertilizerRenderer);
			fertilizerRenderer = null;
		}
	}

	private void UpdateFertilizerRenderer()
	{
		if (Crop == null || !Crop.IsFertilizered)
		{
			ClearFertilizerRenderer();
		}
		else
		{
			RenderFertilizer();
		}
	}
}
