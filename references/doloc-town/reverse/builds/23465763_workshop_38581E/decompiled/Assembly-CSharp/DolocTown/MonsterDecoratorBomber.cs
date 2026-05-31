using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorBomber : MonsterDecorator
{
	[SerializeField]
	private Sprite _maskSprite;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _maskColorNormal = Color.white;

	protected override void OnInitDecorator()
	{
		base.OnInitDecorator();
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component == null)
		{
			Debug.LogError("炸弹无人机:未设置SpriteRenderer组件");
		}
		else
		{
			component.ToggleLightOn(_maskSprite, _maskColorNormal);
		}
	}
}
