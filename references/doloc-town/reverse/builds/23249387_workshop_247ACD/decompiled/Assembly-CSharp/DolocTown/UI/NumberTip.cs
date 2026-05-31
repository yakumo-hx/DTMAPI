using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class NumberTip : DolocUiRecyclableObject
{
	[SerializeField]
	private TMP_Text text;

	private Sequence seq;

	private Vector2 ws;

	public Action OnDispose;

	public float Number
	{
		get
		{
			int.TryParse(text.text, out var result);
			return result;
		}
		set
		{
			text.text = Mathf.FloorToInt(value).ToString();
		}
	}

	public Color Color
	{
		set
		{
			text.color = value;
		}
	}

	public Vector3 WorldPosition
	{
		set
		{
			ws = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		Hide();
	}

	private void LateUpdate()
	{
		Vector2 vector = DolocAPI.WorldToScreen(ws);
		base.transform.position = new Vector3(vector.x, vector.y, base.transform.position.z);
	}

	public void Render(Vector3 worldPosition, float number, Color color)
	{
		ws = worldPosition;
		Number = number;
		Color = color;
	}

	public void Show()
	{
		SetVisible(value: true);
	}

	public void Hide()
	{
		SetVisible(value: false);
	}

	public void Dispose()
	{
		Hide();
		OnDispose?.Invoke();
	}
}
