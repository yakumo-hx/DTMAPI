using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class EmotionRenderer : GameEntity
{
	[SerializeField]
	private SpriteRenderer emotionRenderer;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private float normalTime = 2f;

	[SerializeField]
	private float fastTime = 1.2f;

	[SerializeField]
	private Vector2 showingOffsetFactor = Vector2.one;

	private Queue<Sprite> emotionBuffer;

	private bool isShowing;

	private Vector3 showingOffset;

	public Transform target { get; private set; }

	public Action<EmotionRenderer> recycle { get; set; }

	public override void OnCreated()
	{
		base.OnCreated();
		emotionBuffer = new Queue<Sprite>();
		isShowing = false;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		isShowing = false;
	}

	public void Update()
	{
		if (target == null)
		{
			SetVisible(value: false);
			recycle?.Invoke(this);
		}
		else
		{
			base.transform.position = target.position + showingOffset;
		}
	}

	public void Clear()
	{
		emotionBuffer.Clear();
		recycle(this);
		target = null;
	}

	public void Raise(Transform target, Sprite emotion)
	{
		if (isShowing)
		{
			emotionBuffer.Enqueue(emotion);
			return;
		}
		isShowing = true;
		SetVisible(value: true);
		this.target = target;
		showingOffset = Vector3.zero;
		EmotionPositionControl component = target.GetComponent<EmotionPositionControl>();
		if (component != null)
		{
			showingOffset = component.EmotionOffset;
		}
		else
		{
			SpriteRenderer componentInChildren = target.GetComponentInChildren<SpriteRenderer>(includeInactive: true);
			if (componentInChildren != null && componentInChildren.sprite != null)
			{
				showingOffset = componentInChildren.sprite.rect.size * 0.125f * showingOffsetFactor;
			}
		}
		emotionRenderer.sprite = emotion;
		emotionRenderer.color = DolocColor.empty;
		animator.Play("show", 0, 0f);
	}

	private void __on_hidedone()
	{
		if (emotionBuffer.Count > 0)
		{
			emotionRenderer.sprite = emotionBuffer.Dequeue();
			animator.Play("show", 0, 0f);
		}
		else
		{
			recycle?.Invoke(this);
		}
	}

	private void __on_showdone()
	{
		emotionRenderer.color = Color.white;
		StartCoroutine(__wait());
	}

	private IEnumerator __wait()
	{
		yield return new WaitForSeconds((emotionBuffer.Count == 0) ? normalTime : fastTime);
		emotionRenderer.color = DolocColor.empty;
		animator.Play("hide");
	}
}
