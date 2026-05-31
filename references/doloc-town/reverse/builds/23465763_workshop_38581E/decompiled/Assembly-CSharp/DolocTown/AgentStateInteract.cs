using System;
using UnityEngine;

namespace DolocTown;

public class AgentStateInteract : AgentStateBase
{
	private Action callback;

	private string currentName;

	private bool _supportJump;

	private bool _supportDash;

	public override bool SupportUseItem => false;

	public override bool SupportJump
	{
		get
		{
			if (_supportJump)
			{
				return base.SupportJump;
			}
			return false;
		}
	}

	public override bool SupportDash => _supportDash;

	public AgentStateInteract(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	public void SetStatus(Action callback, string animName, bool supportJump = true, bool supportDash = true)
	{
		currentName = animName;
		this.callback = callback;
		_supportJump = supportJump;
		_supportDash = supportDash;
	}

	protected override AgentStateBase NextState()
	{
		if (!IsAnimationDone(currentName))
		{
			return this;
		}
		return parent.GetState<AgentStateIdle>();
	}

	public override void OnEnter()
	{
		body.PlayAnimation(currentName);
		status.Velocity = Vector2.zero;
	}

	public override void OnExit()
	{
		try
		{
			callback?.Invoke();
			callback = null;
		}
		catch (Exception ex)
		{
			Debug.LogError("交互动作在执行回调函数时遇到异常:" + ex.Message);
			Debug.LogException(ex);
			callback = null;
		}
	}
}
