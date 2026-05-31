using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class MoistRenderer : DolocRecyclableObject
{
	[SerializeField]
	private Vector2ListSO _sizeList;

	[SerializeField]
	private Animator _animtor;

	public void UpdateAnimation(Sprite sprite, bool isPolluted)
	{
		if (base.isVisible && sprite != null)
		{
			PlayAnimation(sprite.rect.size, isPolluted);
		}
	}

	public void PlayAnimation(Vector2 size, bool isPolluted)
	{
		string text = (isPolluted ? "acid_" : "") + GetAnimationName(size);
		if (!string.IsNullOrEmpty(text))
		{
			_animtor?.Play(text, 0, Random.value);
		}
	}

	protected string GetAnimationName(Vector2 size)
	{
		if (_sizeList == null || _sizeList.invalid)
		{
			return null;
		}
		int num = 0;
		float num2 = CalcAreaDistance(size, _sizeList[0]);
		for (int i = 1; i < _sizeList.Length; i++)
		{
			float num3 = CalcAreaDistance(size, _sizeList[i]);
			if (num3 < num2)
			{
				num2 = num3;
				num = i;
			}
		}
		return $"level_{num + 1}";
	}

	protected float CalcAreaDistance(Vector2 L, Vector2 R)
	{
		return Mathf.Abs(L.x - R.x) * Mathf.Abs(L.y - R.y);
	}
}
