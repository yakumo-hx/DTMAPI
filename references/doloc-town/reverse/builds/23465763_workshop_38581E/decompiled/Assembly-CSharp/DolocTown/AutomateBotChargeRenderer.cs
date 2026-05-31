using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/farm/automate_bot_charge", DolocGameAssets.GAME_ENTITY_AUTOMATE_BOT_CHARGE, CustomManagement = true)]
public class AutomateBotChargeRenderer : GameEntity
{
	public SpriteRenderer Sr { get; private set; }

	public Sprite Sprite
	{
		get
		{
			return Sr.sprite;
		}
		set
		{
			Sr.sprite = value;
		}
	}

	public int PowerSpriteIndex
	{
		set
		{
			Sr.material.SetInt("_Index", value);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
	}

	private void OnDestroy()
	{
		Object.Destroy(Sr.material);
	}
}
