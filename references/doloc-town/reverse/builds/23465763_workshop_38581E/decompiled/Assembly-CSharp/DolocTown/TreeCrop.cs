using System.Collections.Generic;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using DolocTown.Config.Resource;
using DolocTown.Config.TechTree;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class TreeCrop
{
	public readonly TreeSeedInfo protoTree;

	private List<GameObject> subRenderers = new List<GameObject>();

	[DebugInfo]
	[JsonProperty]
	public float currentGrowthValue;

	[DebugInfo]
	[JsonProperty]
	public int currentHealth;

	[DebugInfo("肥料信息")]
	[JsonProperty]
	public FertilizerData fertilizerData;

	[DebugInfo]
	[JsonProperty]
	private int skinIdx;

	[JsonProperty]
	private int randomSeed;

	private int[] maxGrowthValues;

	public PlantBasinTree Basin { get; set; }

	public TreeCropRenderer Renderer { get; set; }

	[DebugInfo]
	[JsonProperty]
	protected string seedName => protoTree.Id;

	[DebugInfo]
	[JsonProperty]
	public int CurrentLevel { get; private set; }

	[DebugInfo("是否成熟")]
	private bool isMature => CurrentLevel >= protoTree.MaxLevel;

	public bool IsFertilizered => fertilizerData.duration > 0;

	public ResourceLevelData CurrentLevelData => protoTree.GetLevelData(CurrentLevel);

	public Sprite CurrentSprite => CurrentLevelData.GetCurrentSkin(skinIdx);

	private int maxGrowthValue => maxGrowthValues[Mathf.Clamp(CurrentLevel, 0, maxGrowthValues.Length - 1)];

	public float GrowthProgress => currentGrowthValue / (float)Mathf.Max(1, maxGrowthValue);

	public bool ShouldRepeat => protoTree.RepeatLevel > 0;

	public float Growth
	{
		get
		{
			if (fertilizerData.duration <= 0)
			{
				return 1f;
			}
			fertilizerData.duration--;
			return fertilizerData.growthAddition + 1f;
		}
	}

	public TreeCrop(PlantBasinTree basin, TreeSeedInfo protoTree)
	{
		Basin = basin;
		this.protoTree = protoTree;
		CurrentLevel = 0;
		currentGrowthValue = 0f;
		currentHealth = CurrentLevelData.MaxHealth;
		fertilizerData = default(FertilizerData);
		randomSeed = Random.Range(0, int.MaxValue);
		skinIdx = this.protoTree.RandomSkinIndex(randomSeed);
		maxGrowthValues = protoTree.GetRandomMaxGrowthValues(randomSeed);
	}

	[JsonConstructor]
	protected TreeCrop(string seedName, float currentGrowthValue, int CurrentLevel, int currentHealth, FertilizerData fertilizerData = default(FertilizerData), int randomSeed = 0)
	{
		if (DolocConfig.Tables.TbTreeSeed.DataMap.TryGetValue(seedName, out protoTree))
		{
			this.currentGrowthValue = currentGrowthValue;
			this.CurrentLevel = CurrentLevel;
			this.currentHealth = currentHealth;
			this.fertilizerData = fertilizerData;
			this.fertilizerData.fertilizerItemInfo = FertilizerData.ValidateTreeFertilizerItemInfo(fertilizerData);
			this.randomSeed = randomSeed;
			skinIdx = protoTree.RandomSkinIndex(randomSeed);
			maxGrowthValues = protoTree.GetRandomMaxGrowthValues(randomSeed);
		}
	}

	public bool Validate()
	{
		return protoTree != null;
	}

	public bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		ItemFunctionTool functionTool = tool.functionTool;
		if (!CurrentLevelData.GetTargetToolLevelByType(tool.ToolType, out var minLevel))
		{
			return false;
		}
		DolocAPI.effectProvider.RaiseInstAnim(hitPoint, InstAnimEffectType.DIFFUSION_BUBBLE);
		DolocAPI.Sound.PostSoundEvent((functionTool.Level < minLevel) ? SoundEvents.PLAY_RESOURCE_FELL_ERROR : SoundEvents.PLAY_RESOURCE_FELL);
		if (Basin.TakeOffDecalsOfType<ResinCollector>())
		{
			return false;
		}
		if (functionTool.Level < minLevel)
		{
			DolocAPI.agent.StateManager.Overwrite<AgentStateHit>();
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			return false;
		}
		if (functionTool.ChopNumber < currentHealth)
		{
			currentHealth -= functionTool.ChopNumber;
			Renderer.Shine(DolocAPI.eftConfig.dungeonResourceShineTime);
			Renderer.Shake(DolocAPI.eftConfig.dungeonResourceShakeTime);
			DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.SAWDUST);
			if (CurrentLevel >= 3)
			{
				DolocAPI.effectProvider.RaiseInstPS(Renderer.position2d + new Vector2(0f, CurrentSprite.bounds.size.y * 0.75f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
			}
			return true;
		}
		if (CurrentLevel >= 3)
		{
			DolocAPI.effectProvider.RaiseInstPS(Renderer.position2d + new Vector2(0f, CurrentSprite.bounds.size.y * 0.75f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
		}
		DolocAPI.effectProvider.RaiseInstPS(hitPoint, InstantParticleEffectsType.SPARKS);
		DolocAPI.RaiseInstantPSEffects(Renderer.position, InstantParticleEffectsType.SMOKE_BRUST_02);
		bool useBuff = !ShouldRepeat || !isMature;
		Basin.GenerateCropOutput(CurrentLevelData.DropSpawnEntry, useBuff);
		OnCompleteFell();
		return true;
	}

	private void OnCompleteFell()
	{
		DolocTown.Config.TechTree.TechPointAdder[] techPoints = CurrentLevelData.TechPoints;
		foreach (DolocTown.Config.TechTree.TechPointAdder techPointAdder in techPoints)
		{
			DolocAPI.AddTechExp(techPointAdder.Type, techPointAdder.Count);
		}
		if (isMature && ShouldRepeat)
		{
			CurrentLevel = Mathf.Clamp(protoTree.RepeatLevel, 0, protoTree.MaxLevel);
			currentHealth = CurrentLevelData.MaxHealth;
			currentGrowthValue = 0f;
			PlantBasinTree basin = Basin;
			if (basin != null && basin.IsRender)
			{
				RenderOnGrow();
			}
			DolocAPI.BroadcastString(GameEventType.HARVEST_CROP, protoTree.Id);
		}
		else
		{
			Basin?.ClearCrop();
			DolocAPI.BroadcastString(GameEventType.FELL_DUNGEON_RESOURCE, protoTree.Id);
			DolocAPI.BroadcastString(GameEventType.FELL_DUNGEON_RESOURCE_CLASS, DungeonResourceClass.Plant.ToString().ToLower());
			DolocAPI.archiveHandle.RecordCollection(CollectionType.Resource, protoTree.Id);
		}
	}

	public void Update(WeatherInfo weather, bool isRender)
	{
		if (isMature)
		{
			return;
		}
		currentGrowthValue += Growth;
		if (currentGrowthValue >= (float)maxGrowthValue)
		{
			CurrentLevel++;
			if (isRender)
			{
				RenderOnGrow();
			}
			currentHealth = CurrentLevelData.MaxHealth;
			currentGrowthValue = ((CurrentLevel >= protoTree.MaxLevel) ? maxGrowthValue : 0);
		}
	}

	public void OnRender()
	{
		RefreshSubRenderers();
	}

	public void OnUnRender()
	{
		ClearSubRenders();
	}

	private void RefreshSubRenderers()
	{
		if (Renderer == null)
		{
			return;
		}
		ClearSubRenders();
		PrefabAsset[] subPrefabs = CurrentLevelData.SubPrefabs;
		foreach (PrefabAsset prefabAsset in subPrefabs)
		{
			if (!(prefabAsset.Asset == null))
			{
				GameObject item = Object.Instantiate(prefabAsset.Asset, Renderer.transform);
				subRenderers.Add(item);
			}
		}
	}

	private void ClearSubRenders()
	{
		foreach (GameObject subRenderer in subRenderers)
		{
			Object.Destroy(subRenderer.gameObject);
		}
		subRenderers.Clear();
	}

	public void RenderOnGrow()
	{
		Sprite currentSprite = CurrentSprite;
		Renderer.sp.GrowToNext(currentSprite, new Vector2(0.2f, 0.3f), Ease.OutExpo, 0.4f, 2f);
		Renderer.UpdateColliderPath();
		RefreshSubRenderers();
		Vector2 position2d = Renderer.position2d;
		position2d.y += currentSprite.bounds.size.y * 0.75f;
		DolocAPI.RaiseInstantPSEffects(position2d, InstantParticleEffectsType.LEAVES);
	}

	public void Fertilizer(int duration, float addition, ItemInfo fertilizerItem)
	{
		if (fertilizerData.duration <= 0)
		{
			fertilizerData = new FertilizerData(duration, addition, fertilizerItem);
		}
	}

	public void SetMaxHealth()
	{
		currentHealth = CurrentLevelData.MaxHealth;
	}

	public void DEBUG_SetLevel(int lv)
	{
		CurrentLevel = Mathf.Clamp(lv, 0, protoTree.MaxLevel);
		currentHealth = CurrentLevelData.MaxHealth;
		currentGrowthValue = 0f;
		if (Renderer != null)
		{
			RenderOnGrow();
		}
	}
}
