using UnityEngine;

namespace DolocTown;

[GameEntityManager("/sub_entity/", DolocGameAssets.GAME_ENTITY_PS_STARS, Alias = "stars")]
[GameEntityManager("/sub_entity/", DolocGameAssets.GAME_ENTITY_PS_STINK, Alias = "stink")]
[GameEntityManager("/sub_entity/", DolocGameAssets.GAME_ENTITY_PS_OXYGEN, Alias = "oxygen")]
public class EquipmentParticleSystemRenderer : GameEntity
{
	public ParticleSystem ps { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		ps = GetComponent<ParticleSystem>();
		ps.Stop();
	}

	public void Hide()
	{
		if (ps.isPlaying)
		{
			ps.Stop(withChildren: true);
		}
		SetVisible(value: false);
	}

	public void Play(bool includeChild = true)
	{
		SetVisible(value: true);
		if (!ps.isPlaying)
		{
			ps.Play(includeChild);
		}
	}

	public void Play(Vector2 position, bool includeChild = true)
	{
		base.transform.position = position;
		SetVisible(value: true);
		if (!ps.isPlaying)
		{
			ps.Play(includeChild);
		}
	}
}
