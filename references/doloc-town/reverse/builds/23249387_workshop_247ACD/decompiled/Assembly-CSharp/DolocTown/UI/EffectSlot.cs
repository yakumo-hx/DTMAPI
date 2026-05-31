using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EffectSlot : DolocRecyclableObject
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Text descText;

	private CanvasGroup canvasGroup;

	public RectTransform rectTransform => base.transform as RectTransform;

	public Sprite iconSprite => icon.sprite;

	public float Alpha
	{
		get
		{
			return canvasGroup.alpha;
		}
		set
		{
			canvasGroup.alpha = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		if (!TryGetComponent<CanvasGroup>(out canvasGroup))
		{
			canvasGroup = base.gameObject.AddComponent<CanvasGroup>();
		}
	}

	public void Render(Sprite sprite, string desc)
	{
		icon.sprite = sprite;
		icon.color = ((sprite == null) ? DolocColor.empty : Color.white);
		descText.text = desc;
	}
}
