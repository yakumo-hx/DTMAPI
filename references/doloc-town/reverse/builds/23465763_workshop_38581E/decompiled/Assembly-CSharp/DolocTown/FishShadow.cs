using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class FishShadow : DolocObject
{
	[SerializeField]
	private float pullSpeed = 0.4f;

	[SerializeField]
	private Vector2 pullDstRange = new Vector2(0.5f, 1.2f);

	[SerializeField]
	private Ease pullEase = Ease.InExpo;

	[SerializeField]
	private Ease backEase = Ease.OutBack;

	[SerializeField]
	private Vector2 intervalRange = new Vector2(0.5f, 1.2f);

	[SerializeField]
	private float angleRange = 1.5f;

	private Sequence _sequence;

	public Transform Hook { get; set; }

	public void Stop()
	{
		_sequence?.Kill();
	}

	public void Battle(Vector2 rodPosition)
	{
		base.transform.localScale = new Vector3((base.transform.position.x > rodPosition.x) ? 1 : (-1), 1f, 1f);
		_sequence = DOTween.Sequence();
		float num = pullDstRange.Between();
		float duration = num / pullSpeed;
		Vector3 vector = base.transform.position;
		Vector2 randomDirection = GetRandomDirection(rodPosition, vector);
		_sequence.Append(base.transform.DOMove(base.transform.position + (Vector3)randomDirection * num, duration).SetEase(pullEase));
		_sequence.Join(base.transform.DORotateQuaternion(randomDirection.GetRotation(), duration));
		_sequence.Append(base.transform.DOMove(vector, duration).SetEase(backEase));
		randomDirection = GetRandomDirection(rodPosition, vector);
		_sequence.Join(base.transform.DORotateQuaternion(randomDirection.GetRotation(), duration));
		_sequence.AppendInterval(intervalRange.Between());
		_sequence.OnComplete(delegate
		{
			Battle(rodPosition);
		});
	}

	public Vector2 GetRandomDirection(Vector2 origin, Vector2 current)
	{
		Vector2 vector = ((current.x > origin.x) ? new Vector2(1f, 1f) : new Vector2(1f, -1f));
		vector *= angleRange;
		Vector2 normalized = (current + vector - origin).normalized;
		Vector2 normalized2 = (current - vector - origin).normalized;
		float value = Random.value;
		return new Vector2(Mathf.Lerp(normalized.x, normalized2.x, value), Mathf.Lerp(normalized.y, normalized2.y, value)).normalized;
	}

	private void Update()
	{
		if (!(Hook == null))
		{
			Hook.position = base.transform.position;
		}
	}
}
