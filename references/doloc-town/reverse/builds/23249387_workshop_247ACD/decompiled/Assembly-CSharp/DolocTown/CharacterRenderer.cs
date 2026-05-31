using System;
using Cysharp.Threading.Tasks;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(Collider2D))]
public abstract class CharacterRenderer : GameRendererEntity, IDialogueEntity
{
	[SerializeField]
	protected Animator animator;

	[SerializeField]
	protected SpriteRenderer sp;

	[SerializeField]
	protected Transform operationTipPosHandle;

	private float walkDurationRemaining;

	public override SpriteRenderer outlineTarget => sp;

	public SpriteRenderer SpriteRenderer => sp;

	protected bool flipX
	{
		get
		{
			return base.transform.localScale.x <= 0f;
		}
		set
		{
			base.transform.localScale = (value ? new Vector3(-1f, 1f, 1f) : Vector3.one);
		}
	}

	public bool IsFaceRight => !flipX;

	public RuntimeAnimatorController animatorController
	{
		get
		{
			return animator.runtimeAnimatorController;
		}
		set
		{
			animator.runtimeAnimatorController = value;
		}
	}

	public AnimatorStateInfo currentAnimatorStateInfo => animator.GetCurrentAnimatorStateInfo(0);

	public Material sharedMaterial
	{
		get
		{
			return sp.sharedMaterial;
		}
		set
		{
			sp.sharedMaterial = value;
		}
	}

	public virtual Vector2 positionWS
	{
		get
		{
			return base.transform.position;
		}
		set
		{
			Transform transform = base.transform;
			transform.position = new Vector3(value.x, value.y, transform.position.z);
		}
	}

	public Vector2 operationPopPosition => operationTipPosHandle.position;

	public abstract string EntityId { get; }

	public Transform EntityTransform => base.transform;

	public Vector2 WorldPosition => base.transform.position;

	public Vector2 UiPopPosition => operationPopPosition;

	public abstract bool DisableActing { get; }

	public abstract float DefaultWalkSpeed { get; }

	public virtual void PlayAnimation(string name, bool force, float normalizedTime = 0f)
	{
		if (!animator.enabled)
		{
			animator.enabled = true;
		}
		AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (force || !animatorStateInfo.IsName(name))
		{
			animator.Play(name, 0, normalizedTime);
		}
	}

	protected virtual void PlayAnimation(int animationHash, float normalizedTime = 0f)
	{
		if (!animator.enabled)
		{
			animator.enabled = true;
		}
		if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != animationHash)
		{
			animator.Play(animationHash, 0, normalizedTime);
		}
	}

	public abstract void SetToMarkPoint(string markPointId);

	public async UniTask PlayAnimationAsync(string name, bool force)
	{
		PlayAnimation(name, force);
		await UniTask.WaitWhile(() => currentAnimatorStateInfo.normalizedTime < 1f);
	}

	public void LookAt(Vector2 positionWS)
	{
		LookAt(positionWS.x);
	}

	public void LookAt(float x)
	{
		if (!(Mathf.Abs(x - base.transform.position.x) < 0.001f))
		{
			flipX = x <= base.transform.position.x;
		}
	}

	public void FaceLeft(bool value = true)
	{
		flipX = value;
	}

	public abstract UniTask WalkTo(Vector2 pos, float speed);

	public bool GetSortingOrder(out string sortingLayerName, out int order)
	{
		sortingLayerName = sp.sortingLayerName;
		order = sp.sortingOrder;
		return true;
	}

	public virtual void SetSortingOrder(string sortingLayerName, int order)
	{
		sp.sortingLayerName = sortingLayerName;
		sp.sortingOrder = order;
	}

	protected virtual async UniTask InternalWalkTo(Vector2 pos, float speed, Func<Vector2, bool> onUpdatePosition, Action onWalkEnd)
	{
		if (onUpdatePosition == null)
		{
			onUpdatePosition = (Vector2 _) => true;
		}
		OnWalkStart();
		bool originEnabled = base.enabled;
		Vector2 vector = pos - positionWS;
		bool value = vector.x < 0f;
		FaceLeft(value);
		walkDurationRemaining = vector.magnitude / speed;
		Vector2 speedVec = speed * vector.normalized;
		int previousAnimationHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
		PlayAnimation("walk", force: false);
		Vector2 curPos = positionWS;
		float time = 0f;
		SetEnabled(value: false);
		while (time < walkDurationRemaining)
		{
			PlayAnimation("walk", force: false);
			time += Time.deltaTime;
			curPos += speedVec * Time.deltaTime;
			if (!onUpdatePosition(curPos))
			{
				walkDurationRemaining = 0f;
			}
			await UniTask.NextFrame();
		}
		SetEnabled(originEnabled);
		PlayAnimation(previousAnimationHash);
		onWalkEnd?.Invoke();
		OnWalkEnd();
	}

	public void StopWalk()
	{
		walkDurationRemaining = 0f;
	}

	protected virtual void OnWalkStart()
	{
	}

	protected virtual void OnWalkEnd()
	{
	}

	protected void SetEnabled(bool value)
	{
		base.enabled = value;
	}

	public virtual void SetWorldPosition(Vector2 position)
	{
		StopWalk();
		positionWS = position;
	}

	public void StartSay()
	{
		OnSay();
	}

	protected virtual void OnSay()
	{
	}

	public void StopSay()
	{
		OnStopSay();
	}

	protected virtual void OnStopSay()
	{
	}

	public void ShowEmotion(string emotionName)
	{
		DolocAPI.emotionSystem.Raise(base.transform, emotionName.ConvertToEnumOrDefault<EmotionName>());
	}

	public void SetEntityVisible(bool value)
	{
		SetVisible(value);
	}

	public void SetToForeground(bool active)
	{
		if (active)
		{
			SetSortingOrder("GroundBack", 2);
		}
		else
		{
			SetSortingOrder("Default", 0);
		}
	}
}
