using DolocTown.Config.Resource;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceWeedsSmall : DungeonResourceWeeds
{
	public DungeonResourceWeedsSmall(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceWeedsSmall(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	public override void OnTouch()
	{
		Renderer.SwingOnTouch(light: true);
		if (base.currentLevel >= 1 && RandomUtils.Dice(0.3f))
		{
			Vector2 position2d = Renderer.position2d;
			position2d.y += Renderer.Sr.sprite.bounds.size.y * 0.5f;
			DolocAPI.RaiseInstantPSEffects(position2d, InstantParticleEffectsType.WEEDS);
		}
	}

	public override void OnDisTouch()
	{
		Renderer.SwingOnTouch(light: true);
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		Renderer.SwingOnTouch(light: true);
	}

	public override void OnMonsterDisTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		Renderer.SwingOnTouch(light: true);
	}

	public override void OnWindBlow(Vector2 pos)
	{
		Renderer.SwingOnBlow();
	}
}
