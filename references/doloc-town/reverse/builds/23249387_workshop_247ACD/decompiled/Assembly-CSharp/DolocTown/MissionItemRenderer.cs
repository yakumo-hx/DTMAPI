using DolocTown.Config;
using DolocTown.UI;

namespace DolocTown;

[GameEntityManager("/global/mission_items", DolocGameAssets.GAME_ENTITY_MISSION_ITEM)]
public class MissionItemRenderer : WorldContentRenderer
{
	private ContinuesParticleEffects starEffects;

	public override bool OnlyTouch => false;

	public void ToggleItemShine()
	{
		Sr.ToggleItemShine();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		starEffects?.Recycle();
	}

	public override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(DolocAPI.CalcPopPosition(base.transform, 1.2f), DolocConfig.StaticTexts.UiOperationPick, DolocAPI.UserInput.GlobalInteractActionName);
	}

	public override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	public override void OnInteract()
	{
		base.OnInteract();
		base.WorldContent?.OnInteract();
	}
}
