using UnityEngine;

namespace DolocTown;

[DefaultExecutionOrder(0)]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class SpriteOverrideHandler : MonoBehaviour
{
	public bool onlyOverrideAnimatedSprites;

	protected SpriteRenderer sr;

	protected Animator animator;

	protected Sprite lastSprite;

	protected virtual bool useModOverride => DolocAPI.UseMods;

	protected virtual bool useRuntimeOverride => false;

	protected virtual void Awake()
	{
		sr = GetComponent<SpriteRenderer>();
		lastSprite = sr.sprite;
		animator = GetComponent<Animator>();
	}

	private void OnEnable()
	{
		lastSprite = null;
	}

	protected void LateUpdate()
	{
		Sprite sprite = sr.sprite;
		if ((useRuntimeOverride || useModOverride) && !(sprite == null) && (!onlyOverrideAnimatedSprites || (!(animator == null) && animator.enabled)) && sprite != lastSprite)
		{
			lastSprite = sprite;
			if (useModOverride && TryGetModOverrideSprite(sr.sprite.name, out var overrideSprite) && overrideSprite != sprite)
			{
				sr.sprite = overrideSprite;
				lastSprite = overrideSprite;
			}
			else if (useRuntimeOverride && TryGetRuntimeOverrideSprite(sr.sprite.name, out overrideSprite) && overrideSprite != sprite)
			{
				sr.sprite = overrideSprite;
				lastSprite = overrideSprite;
			}
		}
	}

	protected virtual bool TryGetRuntimeOverrideSprite(string oldSpriteName, out Sprite overrideSprite)
	{
		overrideSprite = null;
		return false;
	}

	protected virtual bool TryGetModOverrideSprite(string oldSpriteName, out Sprite overrideSprite)
	{
		return DolocAPI.modManager.LoadSpriteFromFile(sr.sprite.name, out overrideSprite);
	}
}
