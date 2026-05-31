using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class AgentHatAnimator : DolocObject
{
	private SpriteRenderer _spriteRenderer;

	private Animator _animator;

	private AgentHatRenderer _parent;

	public Sprite Sprite
	{
		get
		{
			return _spriteRenderer.sprite;
		}
		set
		{
			_spriteRenderer.sprite = value;
		}
	}

	public Material Material
	{
		get
		{
			return _spriteRenderer.sharedMaterial;
		}
		set
		{
			_spriteRenderer.sharedMaterial = value;
		}
	}

	public RuntimeAnimatorController AnimatorController
	{
		get
		{
			return _animator.runtimeAnimatorController;
		}
		set
		{
			_animator.runtimeAnimatorController = value;
		}
	}

	public int SortingOrder
	{
		get
		{
			return _spriteRenderer.sortingOrder;
		}
		set
		{
			_spriteRenderer.sortingOrder = value;
		}
	}

	public string SortingLayerName
	{
		get
		{
			return _spriteRenderer.sortingLayerName;
		}
		set
		{
			_spriteRenderer.sortingLayerName = value;
		}
	}

	public bool PauseAnimator
	{
		get
		{
			return !_animator.enabled;
		}
		set
		{
			_animator.enabled = !value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		_spriteRenderer = GetComponent<SpriteRenderer>();
		_animator = GetComponent<Animator>();
		_parent = GetComponentInParent<AgentHatRenderer>();
	}

	public void PlayAnimation(string name, float normalizedTime = 0f)
	{
		if (base.isVisible)
		{
			_animator.Play(name, 0, normalizedTime);
		}
	}

	public void SetAnimatorUpdateUnscaled(bool value)
	{
		_animator.updateMode = (value ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal);
	}

	public void OnFaint()
	{
		if (!(_parent == null))
		{
			_parent.OnFaint();
		}
	}
}
