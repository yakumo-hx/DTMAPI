using DG.Tweening;
using DG.Tweening.Core;

public class UIAnimFloat : DolocTweenAnimation
{
	private DOGetter<float> getter;

	private DOSetter<float> setter;

	public float to { get; set; }

	public UIAnimFloat(DOGetter<float> getter, DOSetter<float> setter)
	{
		this.getter = getter;
		this.setter = setter;
	}

	public override Tween buildAnim()
	{
		return DOTween.To(getter, setter, to, base.time).SetEase(base.ease);
	}
}
