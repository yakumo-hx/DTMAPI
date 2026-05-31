using DG.Tweening;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.UI;

public class DolocPlainTextTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Ease showEase = Ease.OutBack;

	[SerializeField]
	private Ease hideEase = Ease.OutExpo;

	[SerializeField]
	private float time = 0.2f;

	private Text __text;

	private DolocTweenScale __anim;

	public string text
	{
		get
		{
			return __text.text;
		}
		set
		{
			__text.text = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		__text = GetComponentInChildren<Text>();
		__anim = new DolocTweenScale(base.transform, showEase, time);
	}

	public void show(Vector2 position)
	{
		base.transform.position = position;
		base.transform.localScale = Vector3.zero;
		__anim.setParams(showEase, time);
		__anim.forcePlay(Vector3.one);
	}

	public void hide()
	{
		__anim.setParams(hideEase, time);
		__anim.forcePlay(Vector3.zero);
	}
}
