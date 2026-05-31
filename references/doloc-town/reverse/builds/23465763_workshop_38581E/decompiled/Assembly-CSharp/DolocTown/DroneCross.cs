using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class DroneCross : DolocObject
{
	public enum SpreadDirection
	{
		Diagonal,
		Orthogonal
	}

	[SerializeField]
	private SpreadDirection spreadDirection;

	[SerializeField]
	private Ease punchEase;

	[SerializeField]
	private GameObject diagonalRoot;

	[SerializeField]
	private GameObject orthogonalRoot;

	[SerializeField]
	private SpriteRenderer crossLT;

	[SerializeField]
	private SpriteRenderer crossRT;

	[SerializeField]
	private SpriteRenderer crossLB;

	[SerializeField]
	private SpriteRenderer crossRB;

	[SerializeField]
	private SpriteRenderer crossT;

	[SerializeField]
	private SpriteRenderer crossB;

	[SerializeField]
	private SpriteRenderer crossL;

	[SerializeField]
	private SpriteRenderer crossR;

	[SerializeField]
	private float maxDistance;

	[SerializeField]
	private float maxOffsetByDistance;

	[SerializeField]
	private float minOffsetByDistance;

	private Tween attackTween;

	private Vector3 crossOffset;

	private float rotation;

	public float Distance
	{
		set
		{
			float num = Mathf.Clamp(value, 0f, maxDistance);
			float num2 = Mathf.Lerp(minOffsetByDistance, maxOffsetByDistance, num / maxDistance);
			CrossOffset = num2;
		}
	}

	private float CrossOffset
	{
		set
		{
			crossOffset = new Vector3(value, value, value);
			switch (spreadDirection)
			{
			case SpreadDirection.Diagonal:
				crossLT.transform.localPosition = new Vector3(0f - value, value, 0f);
				crossRT.transform.localPosition = new Vector3(value, value, 0f);
				crossLB.transform.localPosition = new Vector3(0f - value, 0f - value, 0f);
				crossRB.transform.localPosition = new Vector3(value, 0f - value, 0f);
				break;
			case SpreadDirection.Orthogonal:
				crossT.transform.localPosition = new Vector3(0f, value, 0f);
				crossB.transform.localPosition = new Vector3(0f, 0f - value, 0f);
				crossL.transform.localPosition = new Vector3(0f - value, 0f, 0f);
				crossR.transform.localPosition = new Vector3(value, 0f, 0f);
				break;
			}
		}
	}

	private void Start()
	{
		diagonalRoot.gameObject.SetActive(spreadDirection == SpreadDirection.Diagonal);
		orthogonalRoot.gameObject.SetActive(spreadDirection == SpreadDirection.Orthogonal);
	}

	protected override void __Init()
	{
		base.__Init();
		CrossOffset = minOffsetByDistance;
	}

	public void PlayAttackTween()
	{
		if (attackTween != null)
		{
			attackTween.Kill();
			base.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
			CrossOffset = minOffsetByDistance;
		}
		Sequence s = DOTween.Sequence();
		s.Join(DOTween.Punch(() => crossOffset, delegate(Vector3 v)
		{
			CrossOffset = v.x;
		}, Vector3.one, 0.2f).SetEase(punchEase));
		rotation = (rotation + 90f) % 360f;
		s.Join(base.transform.DORotate(new Vector3(0f, 0f, rotation), 1f).SetEase(Ease.OutBack));
		attackTween = s;
		attackTween.OnComplete(delegate
		{
			CrossOffset = minOffsetByDistance;
		});
	}
}
