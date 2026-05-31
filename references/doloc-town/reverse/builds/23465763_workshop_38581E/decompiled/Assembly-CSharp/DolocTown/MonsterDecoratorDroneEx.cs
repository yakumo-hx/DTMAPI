using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorDroneEx : MonsterDecorator
{
	[SerializeField]
	private Sprite _maskSprite;

	[SerializeField]
	[ColorUsage(true, true)]
	private Color _maskColorNormal = Color.white;

	protected override void OnInitDecorator()
	{
		base.OnInitDecorator();
		GetComponent<SpriteRenderer>().ToggleLightOn(_maskSprite, _maskColorNormal);
	}
}
