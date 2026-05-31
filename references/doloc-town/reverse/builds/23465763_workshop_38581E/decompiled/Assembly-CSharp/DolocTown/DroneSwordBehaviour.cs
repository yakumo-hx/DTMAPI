using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class DroneSwordBehaviour : DolocObject
{
	[SerializeField]
	private float rotation = 20f;

	[SerializeField]
	private Vector2 swing01Offset;

	[SerializeField]
	private Vector2 swing01Duration;

	private Tween tween;

	public bool IsDash => tween != null;

	public void OnFixedUpdate(float dt)
	{
		tween?.ManualUpdate(dt, Time.fixedUnscaledDeltaTime);
	}

	public void Dash(Vector2 direction, float dashDst, float dashSpeed, Action touchCallback = null, Action postCallback = null, float recoilDur = 0.1f)
	{
		tween?.Kill();
		direction = new Vector2(Mathf.Sign(direction.x), 0f);
		Sequence sequence = DOTween.Sequence();
		float num = base.transform.position.x + dashDst * direction.x;
		sequence.Append(base.transform.DOMoveX(base.transform.position.x + dashDst * direction.x, dashDst / dashSpeed).SetEase(Ease.OutExpo));
		sequence.AppendCallback(delegate
		{
			touchCallback?.Invoke();
		});
		sequence.Append(base.transform.DOMoveX(num - 0.15f * direction.x, recoilDur).SetEase(Ease.Linear));
		sequence.OnComplete(delegate
		{
			tween = null;
			postCallback?.Invoke();
		});
		sequence.SetUpdate(UpdateType.Manual);
		tween = sequence;
	}

	public void RotateTest()
	{
		DroneRenderer droneRenderer = GetComponent<DroneRenderer>();
		droneRenderer.SetFollowEnabled(v: false);
		Vector2 vector = base.transform.position;
		Sequence sequence = DOTween.Sequence();
		sequence.Append(base.transform.DOMove(vector + swing01Offset, swing01Duration.x).SetEase(Ease.OutExpo));
		sequence.Join(base.transform.DORotate(new Vector3(0f, 0f, rotation), swing01Duration.y).SetEase(Ease.OutExpo));
		sequence.Append(base.transform.DORotate(new Vector3(0f, 0f, 0f), 0.5f).SetEase(Ease.OutExpo));
		sequence.OnComplete(delegate
		{
			tween = null;
			droneRenderer.SetFollowEnabled(v: true);
			droneRenderer.ResetPosition(base.transform.position);
		});
		sequence.SetUpdate(UpdateType.Manual);
		tween = sequence;
	}
}
