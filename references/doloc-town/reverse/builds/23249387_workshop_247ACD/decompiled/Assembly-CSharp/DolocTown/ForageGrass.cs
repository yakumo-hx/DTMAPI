using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class ForageGrass : IFeeder, IAnimalInteractable
{
	private PlantBasinGrass _plantBasin;

	private Vector2 _positionWS;

	private readonly GameEntitySlot<ForageGrassRenderer> rendererHandle = new GameEntitySlot<ForageGrassRenderer>();

	public SeedInfo seedProto { get; private set; }

	public ForageGrassRenderer grassRenderer { get; set; }

	public Vector3 positionWS { get; private set; }

	public Vector2 positionWSFeeder { get; private set; }

	public Vector2Int positionCellFeeder { get; private set; }

	[JsonProperty]
	[DebugInfo("农作物ID")]
	public string protoName => seedProto.Id;

	[JsonProperty]
	[DebugInfo("当前阶段")]
	public int currentLevel { get; private set; }

	public CropLevelData currentLevelData => seedProto.GetLevelData(currentLevel);

	[JsonProperty]
	[DebugInfo("当前生长值")]
	public float currentGrowth { get; private set; }

	[DebugInfo("是否已经成熟")]
	public bool IsMature => currentLevel >= seedProto.MatureLevel;

	public Sprite currentSprite => seedProto.GetLevelSprite(currentLevel, _plantBasin.func.CropSkinIndex);

	public Vector2Int AnimalInteractablePosition => positionCellFeeder;

	public Vector2 AnimalInteractablePositionWS => positionWSFeeder;

	public int AnimalInteractableWidth => 2;

	public bool AnimalInteractableIsValid
	{
		get
		{
			if (_plantBasin == null)
			{
				return false;
			}
			return _plantBasin.index >= 0;
		}
	}

	public int AnimalCounter { get; set; }

	public int FeederPriority => 3;

	public bool IsFeederEmpty
	{
		get
		{
			if (DolocConfig.Tables.TbHusbandryEnergy.IsHusbandryFeeds(seedProto.Id, out var _))
			{
				return currentLevel < 2;
			}
			return true;
		}
	}

	public ForageGrass(SeedInfo proto, PlantBasinGrass plantBasin)
	{
		seedProto = proto;
		_plantBasin = plantBasin;
		currentLevel = 0;
	}

	[JsonConstructor]
	protected ForageGrass(string protoName, int currentLevel, float currentGrowth)
	{
		if (!protoName.IsNullOrEmpty() && DolocConfig.Tables.TbSeed.DataMap.TryGetValue(protoName, out var value))
		{
			seedProto = value;
			this.currentLevel = currentLevel;
			this.currentGrowth = currentGrowth;
		}
	}

	public void AfterLoadData(PlantBasinGrass plantBasin)
	{
		_plantBasin = plantBasin;
		ValidateProto(plantBasin.SeedProto);
		ResetPosition();
	}

	private void ValidateProto(SeedInfo proto)
	{
		if (proto == null)
		{
			seedProto = null;
		}
		else if (seedProto == null || !(seedProto.Id == proto.Id))
		{
			seedProto = proto;
		}
	}

	public void ResetPosition()
	{
		if (seedProto != null && _plantBasin != null)
		{
			if (!_plantBasin.TryIndex(this, out var index))
			{
				Debug.LogError("ForageGrass: The plant basin does not contain this crop.");
				return;
			}
			positionWS = _plantBasin.GetCropPosition(index);
			positionWSFeeder = new Vector2(positionWS.x, _plantBasin.Position.y);
			positionCellFeeder = _plantBasin.CurrentRoom.Geometry.CalcCellPosition(positionWSFeeder);
		}
	}

	public void DEBUG_SetLevel(int lv)
	{
		if (currentLevel != lv)
		{
			currentLevel = Mathf.Clamp(lv, 0, seedProto.MatureLevel);
			currentGrowth = 0f;
			if (grassRenderer != null && grassRenderer.isVisible)
			{
				grassRenderer.ToNext(currentSprite);
			}
		}
	}

	public void RenderCrop()
	{
		if (seedProto == null)
		{
			return;
		}
		if (grassRenderer == null)
		{
			grassRenderer = rendererHandle.Entity;
			if (grassRenderer == null)
			{
				return;
			}
			grassRenderer.Crop = this;
		}
		grassRenderer.Sprite = currentSprite;
		grassRenderer.position = positionWS;
		grassRenderer.SetVisible(value: true);
	}

	public void ClearRender()
	{
		rendererHandle.Release();
	}

	public void Grow()
	{
		if (seedProto != null && !IsMature && !((currentGrowth += 1f) < (float)currentLevelData.GrowthValue))
		{
			currentGrowth = 0f;
			currentLevel++;
			currentLevel = Mathf.Min(currentLevel, seedProto.MatureLevel);
			rendererHandle.Do(delegate(ForageGrassRenderer R)
			{
				R.ToNext(currentSprite);
			});
		}
	}

	public void GrowNoRender()
	{
		if (seedProto != null && !IsMature && !((currentGrowth += 1f) < (float)currentLevelData.GrowthValue))
		{
			currentGrowth = 0f;
			currentLevel++;
			currentLevel = Mathf.Min(currentLevel, seedProto.MatureLevel);
		}
	}

	public bool OnAttack(float atk, bool isCritical, Vector2 position, out bool isDead)
	{
		isDead = false;
		return false;
	}

	public bool OnFell(ItemTool tool, Vector2 hitPosition)
	{
		if (seedProto == null)
		{
			return false;
		}
		if (!IsMature)
		{
			return false;
		}
		if (tool.ToolType != ToolType.SICKLE)
		{
			return false;
		}
		DolocAPI.RaiseInstantAnimEffects(hitPosition, InstAnimEffectType.IMPACT_02);
		DolocAPI.RaiseInstantPSEffects(hitPosition, InstantParticleEffectsType.LEAVES);
		currentLevel = 0;
		if (grassRenderer != null)
		{
			grassRenderer.ToNext(currentSprite);
		}
		_plantBasin?.GenerateCropOutput(seedProto, putInBackpack: false, sendMessage: true);
		return true;
	}

	public void Harvest(bool shouldRender, bool putInBackpack = false, bool sendMessage = true)
	{
		if (seedProto != null && IsMature)
		{
			currentLevel = 0;
			if (shouldRender && grassRenderer != null)
			{
				grassRenderer.ToNext(currentSprite);
			}
			_plantBasin?.GenerateCropOutput(seedProto, putInBackpack, sendMessage);
		}
	}

	public int TakeFeeds(int require, out string name)
	{
		name = null;
		if (seedProto == null)
		{
			return 0;
		}
		if (currentLevel < 2)
		{
			return 0;
		}
		if (!DolocConfig.Tables.TbHusbandryEnergy.IsHusbandryFeeds(seedProto.Id, out var energy))
		{
			return 0;
		}
		if (energy <= 0)
		{
			return 0;
		}
		name = seedProto.Id;
		currentLevel--;
		currentGrowth = Mathf.Clamp(currentGrowth, 0f, currentLevelData.GrowthValue);
		UniTask.Delay(300).ContinueWith(delegate
		{
			if (_plantBasin.IsRender)
			{
				grassRenderer.ToNext(currentSprite);
			}
		}).Forget();
		return energy;
	}
}
