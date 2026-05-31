using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceCrop : DungeonResource
{
	public override bool OnlyTouch => false;

	public DungeonResourceCrop(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceCrop(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	public override bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		return false;
	}

	public override void OnTouch()
	{
		base.OnTouch();
		Renderer.SwingOnTouch();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_ENTER_WEEDS);
		if (base.currentLevel == base.Proto.MaxLevel)
		{
			this.ShowSceneOperationTip(base.PositionTip, DolocConfig.StaticTexts.UiOperationHarvest, DolocAPI.UserInput.GlobalInteractActionName);
		}
	}

	public override void OnInteract()
	{
		base.OnInteract();
		if (base.currentLevel >= base.Proto.MaxLevel)
		{
			this.PushSceneOperationTip();
			DolocAPI.agent._Interact(delegate
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_HARVEST);
				DolocAPI.RaiseInstantPSEffects(base.Position + new Vector3(0f, 1.5f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
				GenerateDropItems(isRender: true, null, null);
				OnCompleteFell();
			});
		}
	}

	public override void OnDisTouch()
	{
		base.OnDisTouch();
		Renderer.SwingOnTouch();
		if (base.currentLevel == base.Proto.MaxLevel)
		{
			this.HideSceneOperationTip();
		}
	}

	public override void OnMonsterTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		Renderer.SwingOnTouch();
	}

	public override void OnMonsterDisTouch(Vector2 pos)
	{
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.WEEDS);
		Renderer.SwingOnTouch();
	}

	public override void OnWindBlow(Vector2 pos)
	{
		Renderer.SwingOnBlow();
	}

	public override void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		if (base.currentLevel == base.Proto.MaxLevel)
		{
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_HARVEST);
			DolocAPI.RaiseInstantPSEffects(base.Position + new Vector3(0f, 1.5f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
			GenerateDropItems(isRender: true, null, null);
			OnCompleteFell();
		}
		base.Host.RemoveDungeonResource(this);
	}

	protected override void _ClearEffects(Vector2 hitPosition)
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_HARVEST);
		DolocAPI.RaiseInstantPSEffects(base.Position + new Vector3(0f, 1.5f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
	}
}
