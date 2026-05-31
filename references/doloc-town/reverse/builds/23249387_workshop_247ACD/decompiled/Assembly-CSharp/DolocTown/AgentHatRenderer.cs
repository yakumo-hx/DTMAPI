using System.Collections.Generic;
using DolocTown.Config.Player;
using RedSaw;
using UnityEngine;
using UnityEngine.Serialization;

namespace DolocTown;

public class AgentHatRenderer : DolocObject
{
	private static RuntimeAnimatorController _templateAnimator;

	private static RuntimeAnimatorController _frameAnimator;

	[FormerlySerializedAs("mainAnimator")]
	[SerializeField]
	protected AgentHatAnimator frameAnimator;

	[FormerlySerializedAs("subAnimator")]
	[SerializeField]
	protected AgentHatAnimator templateAnimator;

	private HashSet<string> animatedSet = new HashSet<string>();

	private HatInfo _currentHatRenderInfo;

	private Sprite _currentSprite;

	private Sprite _currentRevertSprite;

	private bool _useSprite;

	private Shiner _shinerMain;

	private Shiner _shinerSub;

	private static RuntimeAnimatorController TemplateAnimator
	{
		get
		{
			if (_templateAnimator == null)
			{
				_templateAnimator = DolocAPI.GetAsset<RuntimeAnimatorController>("game_anim_hat_template");
			}
			return _templateAnimator;
		}
	}

	private static RuntimeAnimatorController FrameAnimator
	{
		get
		{
			if (_frameAnimator == null)
			{
				_frameAnimator = DolocAPI.GetAsset<RuntimeAnimatorController>("game_anim_hat_welder_helmet");
			}
			return _frameAnimator;
		}
	}

	public GameObject GameObject => base.gameObject;

	public Sprite CurrentSprite
	{
		get
		{
			if (CurrentHatRenderInfo != null)
			{
				return _currentSprite;
			}
			return null;
		}
	}

	public HatInfo CurrentHatRenderInfo
	{
		get
		{
			return _currentHatRenderInfo;
		}
		set
		{
			if (value == null)
			{
				ClearAnimator();
				_currentHatRenderInfo = null;
				return;
			}
			_currentHatRenderInfo = value;
			RuntimeAnimatorController asset = value.Animator.Asset;
			Sprite asset2 = value.IdleSprite.Asset;
			Sprite asset3 = value.ClimbSprite.Asset;
			_useSprite = asset3 != null;
			_currentSprite = asset2;
			_currentRevertSprite = asset3;
			SetAnimator(asset2, value.Material.Asset, asset);
			animatedSet = new HashSet<string>(frameAnimator.AnimatorController.GetAnimationNames());
			Play("idle");
		}
	}

	public virtual int SortingOrder
	{
		set
		{
			frameAnimator.SortingOrder = value;
			templateAnimator.SortingOrder = value;
		}
	}

	public virtual string SortingLayerName
	{
		set
		{
			frameAnimator.SortingLayerName = value;
			templateAnimator.SortingLayerName = value;
		}
	}

	public bool PauseAnimator
	{
		set
		{
			frameAnimator.PauseAnimator = value;
			templateAnimator.PauseAnimator = value;
		}
	}

	public void SetAnimator(Sprite sprite, Material material = null, RuntimeAnimatorController controller = null)
	{
		SetVisible(value: false);
		SetVisible(value: true);
		templateAnimator.Sprite = sprite;
		templateAnimator.Material = material ?? LocMaterials.GAME_MAT_2D;
		frameAnimator.AnimatorController = controller ?? FrameAnimator;
	}

	protected override void __Init()
	{
		base.__Init();
		frameAnimator.Init();
		templateAnimator.Init();
		_shinerMain = new Shiner(frameAnimator.GetComponent<SpriteRenderer>());
		_shinerSub = new Shiner(templateAnimator.GetComponent<SpriteRenderer>());
		EnsureTemplateAnimator();
	}

	public void InitHat()
	{
		Init();
	}

	public virtual void UpdatePerTU()
	{
	}

	public void SetAsRiding()
	{
		SortingLayerName = "GroundFront";
		SortingOrder = 1;
	}

	public void ClearAnimator()
	{
		SetVisible(value: false);
	}

	private void EnsureTemplateAnimator()
	{
		if (!(templateAnimator.AnimatorController != null))
		{
			templateAnimator.AnimatorController = TemplateAnimator;
		}
	}

	protected bool ShouldRevert(string name)
	{
		return name == "climb";
	}

	public void Play(string animName, float normalizedTime = 0f)
	{
		if (base.isVisible && _currentHatRenderInfo != null)
		{
			bool flag = CheckUseFrameAnim(animName);
			SetTemplateMode(!flag);
			if (flag)
			{
				frameAnimator.PlayAnimation(animName, normalizedTime);
				return;
			}
			HandleRevert(animName);
			templateAnimator.PlayAnimation(animName, normalizedTime);
		}
	}

	protected virtual bool CheckUseFrameAnim(string animName)
	{
		if (_currentHatRenderInfo.HasAnimationAssets(animName))
		{
			return animatedSet.Contains(animName);
		}
		return false;
	}

	private void SetTemplateMode(bool value)
	{
		frameAnimator.SetVisible(!value);
		templateAnimator.SetVisible(value);
	}

	protected virtual void HandleRevert(string animName)
	{
		if (_useSprite)
		{
			templateAnimator.Sprite = (ShouldRevert(animName) ? _currentRevertSprite : _currentSprite);
		}
		else
		{
			base.transform.localScale = (ShouldRevert(animName) ? new Vector3(-1f, 1f, 1f) : Vector3.one);
		}
	}

	public void SetAnimatorUpdateUnscaled(bool value)
	{
		frameAnimator.SetAnimatorUpdateUnscaled(value);
		templateAnimator.SetAnimatorUpdateUnscaled(value);
	}

	public void Shine(float duration = 0.1f)
	{
		_shinerMain.Raise(LocMaterials.GAME_MAT_HIT, duration);
		_shinerSub.Raise(LocMaterials.GAME_MAT_HIT, duration);
	}

	public virtual void OnRender()
	{
	}

	public virtual void OnDash()
	{
	}

	public virtual void OnJump()
	{
	}

	public virtual void OnFaint()
	{
	}
}
