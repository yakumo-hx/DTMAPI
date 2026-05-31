using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class PhysicalTest : MonoBehaviour
{
	[SerializeField]
	private Ease ease = Ease.Linear;

	[SerializeField]
	private float speed = 1f;

	[SerializeField]
	private Transform target;

	[SerializeField]
	[Range(0f, 0.5f)]
	private float dst = 0.24f;

	[SerializeField]
	private float heightScale = 0.5f;

	[SerializeField]
	private AnimationCurve _curve = new AnimationCurve();

	public void StartJump()
	{
		GetComponent<Animator>().Play("jump_ready");
	}

	private void Update()
	{
		AnimatorStateInfo currentAnimatorStateInfo = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName("jump_ready") && currentAnimatorStateInfo.normalizedTime >= 1f)
		{
			Test();
		}
	}

	public void Test()
	{
		Vector2 vector = base.transform.position;
		Vector2 vector2 = target.position;
		float controlHeight = Mathf.Abs(vector.x - vector2.x) * heightScale;
		BezierCurveParabolic curve = new BezierCurveParabolic(vector, vector2, dst, controlHeight);
		float t = 0f;
		Animator animator = GetComponent<Animator>();
		animator.Play("jump");
		float duration = Vector2.Distance(vector, vector2) / speed;
		DOTween.To(() => t, delegate(float v)
		{
			t = v;
		}, 1f, duration).SetEase(ease).OnUpdate(delegate
		{
			base.transform.position = curve.GetPositionEx(_curve.Evaluate(t));
			if (t >= 0.5f && animator.GetCurrentAnimatorStateInfo(0).IsName("jump"))
			{
				animator.Play("drop");
			}
		})
			.OnComplete(delegate
			{
				GetComponent<Animator>().Play("touch_ground");
			});
	}
}
