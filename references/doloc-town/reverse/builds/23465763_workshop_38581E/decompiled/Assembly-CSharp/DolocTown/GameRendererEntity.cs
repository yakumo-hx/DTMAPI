using UnityEngine;

namespace DolocTown;

public abstract class GameRendererEntity : GameEntity
{
	private EntityOutlineRenderer _outlineRenderer;

	public abstract SpriteRenderer outlineTarget { get; }

	protected virtual Sprite overrideOutlineSprite { get; }

	protected virtual ObjectOutlineType outlineType => ObjectOutlineType.ExcludeBottom;

	protected virtual bool useInnerOutline => false;

	public bool ShowOutline
	{
		set
		{
			if (value)
			{
				if ((object)_outlineRenderer == null)
				{
					_outlineRenderer = DolocAPI.EntitySystem.Next<EntityOutlineRenderer>();
				}
				if (!(_outlineRenderer == null))
				{
					_outlineRenderer.ShowOutline(outlineTarget, outlineType, overrideOutlineSprite, useInnerOutline);
				}
			}
			else
			{
				DolocAPI.EntitySystem.Recycle(_outlineRenderer);
				_outlineRenderer = null;
			}
		}
	}

	public override void OnRecycle()
	{
		ShowOutline = false;
		base.OnRecycle();
	}
}
