using UnityEngine;

namespace DolocTown;

[GameEntityManager("/sub_entity/crop_shiny", DolocGameAssets.GAME_ENTITY_CROP_SHINY)]
[RequireComponent(typeof(ParticleSystem))]
public class CropShinyRenderer : GameEntity
{
	[SerializeField]
	private ParticleSystem ps;

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		if (value)
		{
			ps.Play(withChildren: true);
		}
		else
		{
			ps.Stop();
		}
	}
}
