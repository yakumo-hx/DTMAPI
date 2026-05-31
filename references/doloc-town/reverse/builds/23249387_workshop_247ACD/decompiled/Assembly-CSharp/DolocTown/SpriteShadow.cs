using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

[GameEntityManager("/global", DolocGameAssets.GAME_ENTITY_SPRITE_SHADOW)]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteShadow : GameEntity
{
	private SpriteRenderer _spriteRenderer;

	public Sprite ShadowSprite
	{
		get
		{
			return _spriteRenderer.sprite;
		}
		set
		{
			_spriteRenderer.sprite = value;
			if (!(value == null) && !(_spriteRenderer.sharedMaterial == null))
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				_spriteRenderer.GetPropertyBlock(materialPropertyBlock);
				Vector4 outerUV = DataUtility.GetOuterUV(value);
				materialPropertyBlock.SetVector("_MainTex_UVInfos", outerUV);
				_spriteRenderer.SetPropertyBlock(materialPropertyBlock);
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		_spriteRenderer = GetComponent<SpriteRenderer>();
	}
}
