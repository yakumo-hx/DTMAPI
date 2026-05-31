using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VegetationGrow : Vegetation
{
	[JsonProperty]
	private int randomSeed;

	private int[] growthThresholds;

	protected VegetationFuncGrowBase Func => proto.Function as VegetationFuncGrowBase;

	public override Sprite CurrentSprite => Func.LevelSprites.assets[currentLevel];

	protected int maxLevel => Func.LevelSprites.assets.Length - 1;

	protected override bool generateDrop => currentLevel == maxLevel;

	[JsonProperty]
	public int currentLevel { get; private set; }

	[JsonProperty]
	public int currentGrowth { get; private set; }

	public VegetationGrow(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
		randomSeed = Random.Range(0, 100000);
		RevertRandomValues(randomSeed);
	}

	[JsonConstructor]
	public VegetationGrow(int index, Vector2Int anchor, Vector3 position, string vegetationName, int currentLevel, int currentGrowth, int randomSeed)
		: base(index, anchor, position, vegetationName)
	{
		if (proto != null)
		{
			this.currentLevel = currentLevel;
			this.currentGrowth = currentGrowth;
			this.randomSeed = randomSeed;
			RevertRandomValues(randomSeed);
		}
	}

	public override void RandomInitState()
	{
		int growthLevel = (CheckGrowingMonth() ? Random.Range(0, Func.LevelSprites.assets.Length) : 0);
		SetGrowthLevel(growthLevel, useTween: false);
	}

	public override void UpdatePerTu()
	{
		if (CheckGrowingMonth())
		{
			Grow(useTween: true);
		}
	}

	public override void UpdatePerTuNoRender()
	{
		if (CheckGrowingMonth())
		{
			Grow(useTween: false);
		}
	}

	public void Grow(bool useTween)
	{
		if (currentLevel < maxLevel && ++currentGrowth >= growthThresholds[currentLevel])
		{
			SetGrowthLevel(currentLevel + 1, useTween);
		}
	}

	protected void ReGrow()
	{
		SetGrowthLevel(0, useTween: false);
		randomSeed = Random.Range(0, 100000);
		RevertRandomValues(randomSeed);
	}

	protected virtual void SetGrowthLevel(int growthLevel, bool useTween)
	{
		currentGrowth = 0;
		currentLevel = Mathf.Min(growthLevel, maxLevel);
		if (base.isRender)
		{
			if (useTween)
			{
				base.Renderer.Sr.GrowToNext(CurrentSprite);
			}
			else
			{
				base.Renderer.Sr.sprite = CurrentSprite;
			}
		}
	}

	private void RevertRandomValues(int seed)
	{
		growthThresholds = new int[maxLevel];
		for (int i = 0; i < maxLevel; i++)
		{
			growthThresholds[i] = Func.GrowthValue.Random(seed);
		}
	}
}
