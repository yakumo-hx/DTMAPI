using DolocTown.Config;
using DolocTown.Config.Resource;
using DolocTown.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class DungeonResourceModelPaperBox : DungeonResource
{
	public override bool OnlyTouch => false;

	public DungeonResourceModelPaperBox(IDungeonResourceHost host, ResourceInfo proto, Vector3 wp, Vector2Int anchor)
		: base(host, proto, wp, anchor)
	{
	}

	[JsonConstructor]
	protected DungeonResourceModelPaperBox(int id, Vector2Int anchor, Vector3 position, string ResourceName, int GrowthLevel, int currentGrowth, int currentHealth, int skinIdx, int randomSeed)
		: base(id, anchor, position, ResourceName, GrowthLevel, currentGrowth, currentHealth, skinIdx, randomSeed)
	{
	}

	public override bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		return false;
	}

	public override void OnInteract()
	{
		base.OnInteract();
		Vector2 positionWS = GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);
		DolocAPI.effectProvider.RaiseInstPS(positionWS, InstantParticleEffectsType.PAPERBOX);
		DolocAPI.RaiseInstantPSEffects(positionWS, InstantParticleEffectsType.SMOKE_BRUST_02);
		GenerateDropItems(isRender: true, null, null);
		OnCompleteFell();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_RESOURCE_PAPER_BOX);
	}

	public override void OnDisTouch()
	{
		Renderer.ShowOutline = false;
		Renderer.HideSceneOperationTip();
	}

	public override void OnTouch()
	{
		Renderer.ShowOutline = true;
		Vector2 position2d = Renderer.position2d;
		position2d.y += 4.5f;
		Renderer.ShowSceneOperationTip(position2d, DolocConfig.StaticTexts.UiOperationOpen, DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected override void OnCompleteFell()
	{
		base.OnCompleteFell();
		Renderer.HideSceneOperationTip();
	}

	public override void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		pos = GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);
		DolocAPI.effectProvider.RaiseInstPS(pos, InstantParticleEffectsType.PAPERBOX);
		GenerateDropItems(isRender: true, null, null);
		OnCompleteFell();
	}

	protected override void _ClearEffects(Vector2 hitPosition)
	{
		Vector2 positionWS = GeometryUtils.CalcIndicatePos(Renderer.Sr, DolocAPI.eftConfig.dungeonResourceInstPsYRate);
		DolocAPI.effectProvider.RaiseInstPS(positionWS, InstantParticleEffectsType.PAPERBOX);
		DolocAPI.RaiseInstantPSEffects(positionWS, InstantParticleEffectsType.SMOKE_BRUST_02);
	}
}
