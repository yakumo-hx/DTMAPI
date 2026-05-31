using DolocTown.Config.Resource;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VegetationDandelion : VegetationGrow
{
	protected override bool generateDrop => true;

	protected VegetationFuncDandelion FuncDandelion => (VegetationFuncDandelion)proto.Function;

	private bool hasTouched => base.currentLevel < base.maxLevel;

	public VegetationDandelion(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(proto, anchor, position, cvPositions)
	{
	}

	[JsonConstructor]
	public VegetationDandelion(int index, Vector2Int anchor, Vector3 position, string vegetationName, int currentLevel, int currentGrowth, int randomSeed)
		: base(index, anchor, position, vegetationName, currentLevel, currentGrowth, randomSeed)
	{
	}

	public override void OnTouch()
	{
		base.OnTouch();
		Touched();
	}

	public override bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		if (!CheckToolConstraints(tool))
		{
			return false;
		}
		Touched();
		FellingEffect();
		GenerateDropItems();
		base.Host.RemoveVegetation(this);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_HARVEST);
		return true;
	}

	public override void OnWindBlow(Vector2 pos)
	{
		base.OnWindBlow(pos);
		Touched();
	}

	public override void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		Touched();
		FellingEffect();
		base.OnBomb(damage, ctr, pos);
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		base.OnMonsterTouch(pos);
		Touched();
	}

	private void FellingEffect()
	{
		Vector3 vector = base.Position;
		vector.y += CurrentSprite.rect.size.y * 0.0625f;
		DolocAPI.RaiseInstantPSEffects(vector, InstantParticleEffectsType.DANDELION_PARTS);
	}

	private void Touched()
	{
		if (!hasTouched)
		{
			ReGrow();
			RaiseInstantPSEffects(FuncDandelion.EffectsName, base.Renderer.position2d + FuncDandelion.EffectsOffset);
		}
	}
}
