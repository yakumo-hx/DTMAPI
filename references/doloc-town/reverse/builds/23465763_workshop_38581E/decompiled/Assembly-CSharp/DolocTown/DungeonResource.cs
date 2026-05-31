using System.Collections.Generic;
using DolocTown.Config.General;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public abstract class DungeonResource : TerrainContent
{
	public List<GameObject> subRenderers = new List<GameObject>();

	[JsonProperty]
	protected int currentGrowth;

	[JsonProperty]
	private readonly int randomSeed;

	[JsonProperty]
	private readonly int skinIdx;

	[JsonProperty]
	public int currentHealth;

	private int[] growthThresholds;

	public IDungeonResourceHost Host { get; set; }

	public ResourceInfo Proto { get; private set; }

	public virtual DungeonResourceRenderer Renderer { get; set; }

	public virtual Sprite CurrentSprite => currentLevelData.GetCurrentSkin(skinIdx);

	public virtual bool IsValid => Proto != null;

	[JsonProperty]
	public string ResourceName => Proto.Id;

	public DungeonResourceType ResourceType => Proto.ResourceType;

	public bool isRender { get; private set; }

	[JsonProperty("GrowthLevel")]
	public int currentLevel { get; private set; }

	public ResourceLevelData currentLevelData => Proto.GetLevelData(currentLevel);

	public Vector2 PositionCenter
	{
		get
		{
			float num = CurrentSprite.rect.size.y * 0.125f * 0.5f;
			Vector2 position2d = Renderer.position2d;
			position2d.y += num;
			return position2d;
		}
	}

	public Vector2 PositionTip
	{
		get
		{
			float b = CurrentSprite.rect.size.y * 0.125f + 1.5f;
			b = Mathf.Max(4.5f, b);
			Vector2 position2d = Renderer.position2d;
			position2d.y += b;
			return position2d;
		}
	}

	public int CurrentGrowth => currentGrowth;

	public bool CanGrow
	{
		get
		{
			if (currentLevelData.CanGrow)
			{
				return currentLevel < MaxLevel;
			}
			return false;
		}
	}

	public int MaxLevel => Proto.MaxLevel;

	private int currentGrowthThreshold => growthThresholds[currentLevel];

	protected Vector2 indicatePos => GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);

	public virtual bool OnlyTouch => true;

	public bool CanInteractContinues => false;

	public virtual bool DisableAfterInteract => false;

	public DungeonResource(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(-1, anchor, wp)
	{
		Proto = proto;
		Host = host;
		skinIdx = Proto.RandomSkinIndex();
		randomSeed = Random.Range(0, 100000);
		RevertRandomValues(randomSeed);
	}

	[JsonConstructor]
	protected DungeonResource(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position)
	{
		if (DolocAPI.QueryResourceProto(ResourceName, out var proto))
		{
			Proto = proto;
			currentLevel = GrowthLevel;
			this.currentGrowth = currentGrowth;
			this.currentHealth = currentHealth;
			this.skinIdx = skinIdx;
			this.randomSeed = randomSeed;
			RevertRandomValues(randomSeed);
		}
	}

	private void RevertRandomValues(int randomSeed)
	{
		growthThresholds = new int[MaxLevel];
		for (int i = 0; i < MaxLevel; i++)
		{
			ResourceLevelData levelData = Proto.GetLevelData(i);
			growthThresholds[i] = levelData.GrowthValue.Random(randomSeed);
		}
	}

	protected override bool ValidateDeserialization()
	{
		return Proto != null;
	}

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		return new Dictionary<TerrainLayerName, Vector2Int[]> { 
		{
			Proto.TerrainLayer,
			Grid2D.CoverHArray(anchor, Proto.Width)
		} };
	}

	protected void SendMessage(ResourceInfo proto)
	{
		DolocAPI.BroadcastString(GameEventType.FELL_DUNGEON_RESOURCE, proto.Id);
		DolocAPI.BroadcastString(GameEventType.FELL_DUNGEON_RESOURCE_CLASS, proto.ResourceClass.ToString().ToLower());
		DolocAPI.archiveHandle.RecordCollection(CollectionType.Resource, proto.Id);
	}

	protected void AddTechPoints()
	{
		if (!currentLevelData.TechPoints.IsNullOrEmpty())
		{
			DolocTown.Config.TechTree.TechPointAdder[] techPoints = currentLevelData.TechPoints;
			foreach (DolocTown.Config.TechTree.TechPointAdder techPointAdder in techPoints)
			{
				DolocAPI.AddTechExp(techPointAdder.Type, techPointAdder.Count);
			}
		}
	}

	public void Render()
	{
		isRender = true;
		OnRender();
	}

	protected virtual void OnRender()
	{
		float x = (float)Random.Range(-Mathf.Abs(Proto.PixelOffset.x), Mathf.Abs(Proto.PixelOffset.y)) * 0.125f;
		SetSortingLayer(Proto.ResourceType_Ref.SortingLayer);
		Renderer.position += new Vector3(x, -1E-06f * Renderer.position.x);
		RefreshSubRenderers();
	}

	protected void SetSortingLayer(SortingLayerInfo sortingLayerInfo)
	{
		Renderer.Sr.sortingLayerName = sortingLayerInfo.SortingLayerName;
		Renderer.Sr.sortingOrder = sortingLayerInfo.OrderInLayer;
		Vector3 vector = Renderer.position;
		vector.z = sortingLayerInfo.ZValue;
		Renderer.position = vector;
	}

	public void UnRender()
	{
		isRender = false;
		ClearSubRenders();
		OnUnRender();
	}

	private void RefreshSubRenderers()
	{
		if (!isRender || Renderer == null)
		{
			return;
		}
		ClearSubRenders();
		PrefabAsset[] subPrefabs = currentLevelData.SubPrefabs;
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

	protected virtual void OnUnRender()
	{
	}

	public virtual void OnRemove()
	{
	}

	public void RemoveResource(bool useEffect)
	{
		if (isRender)
		{
			if (useEffect)
			{
				_ClearEffects(Renderer.position2d + new Vector2(0f, 1.5f));
			}
			Host.RemoveDungeonResource(this);
		}
		else
		{
			Host.RemoveDungeonResourceNoRender(this);
		}
	}

	protected virtual void _ClearEffects(Vector2 hitPosition)
	{
	}

	protected void GenerateDropItems(bool isRender, string overrideSpawnLut, CountItem[] extraItems)
	{
		DolocAPI.SpawnResourceDropItems(this, isRender, overrideSpawnLut);
		GenerateDropItems(isRender, extraItems);
	}

	protected void GenerateDropItems(bool isRender, CountItem[] countItems)
	{
		if (countItems.IsNullOrEmpty())
		{
			return;
		}
		IDropItemHost dropItemHost = (IDropItemHost)Host;
		Vector3 vector = base.Position;
		if (isRender)
		{
			vector.y += DolocAPI.eftConfig.dungeonResourceDropItemPopYOffset;
			float num = (float)Proto.Width / 3f * 1.5f - 0.1f;
			for (int i = 0; i < countItems.Length; i++)
			{
				CountItem countItem = countItems[i];
				for (int j = 0; j < countItem.itemCount; j++)
				{
					dropItemHost.CreateDropItem(countItem.itemName, vector, shouldSendMsg: true, Random.Range(0f - num, num));
				}
			}
		}
		else
		{
			dropItemHost.CreateDropItemsNoRender(countItems, vector, shouldSendMessage: true);
		}
	}

	public void Grow(bool useTween)
	{
		if (CanGrow && ++currentGrowth >= currentGrowthThreshold)
		{
			SetGrowthLevel(currentLevel + 1, useTween);
		}
	}

	public virtual void SetGrowthLevel(int growthLevel, bool useTween)
	{
		currentGrowth = 0;
		int oldLevel = currentLevel;
		currentLevel = Mathf.Min(growthLevel, MaxLevel);
		currentHealth = currentLevelData.MaxHealth;
		OnGrowthLevelChange(oldLevel, currentLevel);
		if (isRender)
		{
			if (useTween)
			{
				Renderer.Sr.GrowToNext(CurrentSprite);
			}
			else
			{
				Renderer.Sr.sprite = CurrentSprite;
			}
		}
	}

	public void SetMaxGrowthLevel(bool useTween)
	{
		SetGrowthLevel(MaxLevel, useTween);
	}

	protected virtual void OnGrowthLevelChange(int oldLevel, int newLevel)
	{
		RefreshSubRenderers();
	}

	public void InitRandomGrowth(bool useMinHighLevelProb)
	{
		float num = (useMinHighLevelProb ? DolocAPI.GlobalParameter.HighLevelResourceMinProbabilityInDungeon : 0f);
		int value = ((useMinHighLevelProb && Random.value < num) ? Random.Range(MaxLevel - 1, MaxLevel + 1) : Random.Range(0, MaxLevel + 1));
		SetGrowthLevel(Mathf.Clamp(value, 0, MaxLevel), useTween: false);
	}

	public void SetMaxHealth()
	{
		currentHealth = currentLevelData.MaxHealth;
	}

	public virtual void OnAnimalTouch()
	{
	}

	public virtual void OnAnimalDistouch()
	{
	}

	public bool CheckToolTypeMatch(ToolType type)
	{
		int minLevel;
		return currentLevelData.GetTargetToolLevelByType(type, out minLevel);
	}

	protected bool CheckToolLevelMatch(ToolType type, int level)
	{
		if (!currentLevelData.GetTargetToolLevelByType(type, out var minLevel))
		{
			return false;
		}
		return level >= minLevel;
	}

	public virtual bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		ResourceFellData fellData = new ResourceFellData(this, tool, hitPoint);
		if (!fellData.Valid)
		{
			return false;
		}
		_OnFell(fellData);
		return true;
	}

	public void _Fell(ResourceFellData data)
	{
		if (data.Valid)
		{
			_OnFell(data);
		}
	}

	protected virtual void _OnFell(ResourceFellData fellData)
	{
	}

	protected virtual void OnCompleteFell()
	{
		ResourceInfo proto = Proto;
		AddTechPoints();
		Host.RemoveDungeonResource(this);
		SendMessage(proto);
	}

	protected void RaiseRandomImpact(Vector2 pos)
	{
		InstAnimEffectType[] array = new InstAnimEffectType[3]
		{
			InstAnimEffectType.IMPACT_01,
			InstAnimEffectType.IMPACT_02,
			InstAnimEffectType.IMPACT_03
		};
		int num = Random.Range(0, array.Length);
		DolocAPI.RaiseInstantAnimEffects(pos, array[num]);
	}

	public virtual void OnTouch()
	{
	}

	public virtual void OnDisTouch()
	{
	}

	public virtual void OnInteract()
	{
	}

	public virtual bool OnHit(Vector2 pos)
	{
		return false;
	}

	public virtual void OnWater()
	{
	}

	public virtual void OnMonsterTouch(Vector2 pos)
	{
	}

	public virtual void OnMonsterDisTouch(Vector2 pos)
	{
	}

	public virtual void OnWindBlow(Vector2 pos)
	{
	}

	public virtual void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
	}
}
