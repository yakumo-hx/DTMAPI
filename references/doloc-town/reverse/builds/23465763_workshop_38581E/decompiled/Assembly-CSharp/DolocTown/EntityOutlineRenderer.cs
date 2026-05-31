using UnityEngine;

namespace DolocTown;

[DefaultExecutionOrder(1000)]
[GameEntityManager("/sub_entity/outlines", DolocGameAssets.GAME_ENTITY_OUTLINE, Frequency = 3)]
public class EntityOutlineRenderer : GameEntity
{
	private Material _instancedOutlineMaterial;

	private SpriteRenderer targetSpriteRenderer;

	private SpriteRenderer spriteRenderer;

	private ObjectOutlineType currentOutlineType;

	private Sprite currentSprite;

	private Sprite overrideSprite;

	private bool useInnerOutline;

	private static readonly int SpriteWidth = Shader.PropertyToID("_SpriteWidth");

	private static readonly int SpriteHeight = Shader.PropertyToID("_SpriteHeight");

	private static readonly int ShowTopOutline = Shader.PropertyToID("_ShowTopOutline");

	private static readonly int ShowBottomOutline = Shader.PropertyToID("_ShowBottomOutline");

	private static readonly int UseInnerOutline = Shader.PropertyToID("_UseInnerOutline");

	private static readonly int SpriteUVMin = Shader.PropertyToID("_SpriteUVMin");

	private static readonly int SpriteUVMax = Shader.PropertyToID("_SpriteUVMax");

	private Material outlineMaterial
	{
		get
		{
			if (_instancedOutlineMaterial == null && spriteRenderer.sharedMaterial != null)
			{
				_instancedOutlineMaterial = new Material(spriteRenderer.sharedMaterial);
				spriteRenderer.sharedMaterial = _instancedOutlineMaterial;
			}
			return _instancedOutlineMaterial;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.enabled = false;
	}

	private void LateUpdate()
	{
		if (DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState.ShowOutline)
		{
			spriteRenderer.enabled = true;
			RefreshTarget();
		}
		else
		{
			spriteRenderer.enabled = false;
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		spriteRenderer.enabled = false;
		currentSprite = null;
		currentOutlineType = ObjectOutlineType.ExcludeBottom;
		targetSpriteRenderer = null;
	}

	public void ShowOutline(SpriteRenderer target, ObjectOutlineType outlineType, Sprite overrideSprite = null, bool useInnerOutline = false)
	{
		if (!(target == null) && !(spriteRenderer == null) && !(outlineMaterial == null) && !(target.sprite == null))
		{
			targetSpriteRenderer = target;
			spriteRenderer.enabled = true;
			currentOutlineType = outlineType;
			this.overrideSprite = overrideSprite;
			this.useInnerOutline = useInnerOutline;
			RefreshTarget();
		}
	}

	private void RefreshTarget()
	{
		if (!(targetSpriteRenderer == null))
		{
			RefreshSprite((overrideSprite == null) ? targetSpriteRenderer.sprite : overrideSprite);
			base.transform.localScale = targetSpriteRenderer.transform.localScale;
			spriteRenderer.flipX = targetSpriteRenderer.flipX;
			spriteRenderer.flipY = targetSpriteRenderer.flipY;
			spriteRenderer.sortingOrder = targetSpriteRenderer.sortingOrder;
			spriteRenderer.sortingLayerID = targetSpriteRenderer.sortingLayerID;
			base.transform.position = targetSpriteRenderer.transform.position;
		}
	}

	private void RefreshSprite(Sprite sprite)
	{
		if (currentSprite == sprite)
		{
			return;
		}
		currentSprite = sprite;
		spriteRenderer.sprite = sprite;
		if (!(currentSprite == null))
		{
			GetSpriteUVRange(sprite, out var uvMin, out var uvMax);
			outlineMaterial.SetFloat(SpriteWidth, sprite.rect.width);
			outlineMaterial.SetFloat(SpriteHeight, sprite.rect.height);
			outlineMaterial.SetVector(SpriteUVMin, new Vector4(uvMin.x, uvMin.y, 0f, 0f));
			outlineMaterial.SetVector(SpriteUVMax, new Vector4(uvMax.x, uvMax.y, 0f, 0f));
			if (currentOutlineType == ObjectOutlineType.ExcludeBottom)
			{
				outlineMaterial.SetFloat(ShowTopOutline, 1f);
				outlineMaterial.SetFloat(ShowBottomOutline, 0f);
			}
			else
			{
				outlineMaterial.SetFloat(ShowTopOutline, 1f);
				outlineMaterial.SetFloat(ShowBottomOutline, 1f);
			}
			outlineMaterial.SetFloat(UseInnerOutline, useInnerOutline ? 1f : 0f);
		}
	}

	private static void GetSpriteUVRange(Sprite sprite, out Vector2 uvMin, out Vector2 uvMax)
	{
		Vector2[] uv = sprite.uv;
		if (uv == null || uv.Length == 0)
		{
			uvMin = Vector2.zero;
			uvMax = Vector2.one;
			return;
		}
		float x = uv[0].x;
		float y = uv[0].y;
		float x2 = uv[0].x;
		float y2 = uv[0].y;
		for (int i = 1; i < uv.Length; i++)
		{
			Vector2 vector = uv[i];
			if (vector.x < x)
			{
				x = vector.x;
			}
			if (vector.y < y)
			{
				y = vector.y;
			}
			if (vector.x > x2)
			{
				x2 = vector.x;
			}
			if (vector.y > y2)
			{
				y2 = vector.y;
			}
		}
		uvMin = new Vector2(x, y);
		uvMax = new Vector2(x2, y2);
	}

	public override void OnReuse()
	{
		base.OnReuse();
		currentSprite = null;
	}

	private void OnDestroy()
	{
		if (_instancedOutlineMaterial != null)
		{
			Object.Destroy(_instancedOutlineMaterial);
			_instancedOutlineMaterial = null;
		}
	}
}
