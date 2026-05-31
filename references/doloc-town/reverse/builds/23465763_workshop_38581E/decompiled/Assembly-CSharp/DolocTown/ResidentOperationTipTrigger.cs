using System.Collections.Generic;
using System.Linq;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class ResidentOperationTipTrigger : MonoBehaviour, IInteractable
{
	private enum TriggerMode
	{
		Duration,
		Trigger,
		GameMessage,
		Manual
	}

	[SerializeField]
	[Tooltip("为空时，提示将在触发器中心弹出")]
	private Transform triggerPosition;

	[SerializeField]
	[Tooltip("不设定提示符文本参数时，将使用该默认文本")]
	private TextConfig defaultTextConfig;

	[SerializeField]
	private TriggerMode triggerMode;

	[SerializeField]
	[Tooltip("当该提示符消失时，是否连携触发其他提示符")]
	private bool chainTrigger;

	[SerializeField]
	private string chainTriggerId;

	[SerializeField]
	private float duration = 3f;

	[SerializeField]
	private GameEventType _eventType;

	[SerializeField]
	private float disapperaDelay = 2f;

	private Coroutine _coroutine;

	private Coroutine _disappearCoroutine;

	private IEnumerable<string> AvailableChainTriggers => from t in base.transform.parent.GetComponentsInChildren<ResidentOperationTipTrigger>()
		select t.name into name
		where name != base.gameObject.name
		select name;

	private bool _DurationMode => triggerMode == TriggerMode.Duration;

	private bool _TriggerMode => triggerMode == TriggerMode.Trigger;

	private bool _GameMessageMode => triggerMode == TriggerMode.GameMessage;

	private bool _ManualMode => triggerMode == TriggerMode.Manual;

	private bool _NotManualAndDurationMode
	{
		get
		{
			if (!_ManualMode)
			{
				return !_DurationMode;
			}
			return false;
		}
	}

	public bool OnlyTouch => true;

	public bool CanInteractContinues => false;

	public void Invoke(string prompt = null)
	{
		Vector2 position = ((triggerPosition == null) ? base.transform.position : triggerPosition.position);
		Invoke(position, prompt);
	}

	public void Invoke(Vector2 position, string prompt = null)
	{
		if (prompt == null)
		{
			prompt = defaultTextConfig.Text;
		}
		this.ShowResidentOperationTip(position, prompt);
		base.gameObject.SetActive(value: true);
		AfterInvoke();
	}

	public void RefreshPrompt()
	{
		string text = defaultTextConfig.Text;
		this.RefreshResidentOperationTipPrompt(text);
	}

	private void AfterInvoke()
	{
		switch (triggerMode)
		{
		case TriggerMode.Manual:
			break;
		case TriggerMode.Trigger:
			GetComponent<Collider2D>().isTrigger = true;
			break;
		case TriggerMode.Duration:
			_coroutine = DolocAPI.Delay(duration, this.HideResidentOperationTip);
			break;
		case TriggerMode.GameMessage:
			break;
		}
	}

	private void Disappear()
	{
		if (chainTrigger && !chainTriggerId.IsNullOrEmpty())
		{
			DolocAPI.InvokeSceneResidentTip(chainTriggerId);
		}
		if (_disappearCoroutine != null)
		{
			DolocAPI.StopCoroutine(_disappearCoroutine);
		}
		if (disapperaDelay <= 0f)
		{
			this.HideResidentOperationTip();
			base.gameObject.SetActive(value: false);
			return;
		}
		_disappearCoroutine = DolocAPI.Delay(disapperaDelay, delegate
		{
			this.HideResidentOperationTip();
			base.gameObject.SetActive(value: false);
		});
	}

	public void SendMessage(GameMessage message)
	{
		if (triggerMode == TriggerMode.GameMessage && _eventType == message.Type)
		{
			Disappear();
		}
	}

	public void OnDestroy()
	{
		this.HideResidentOperationTip();
		if (_coroutine != null)
		{
			DolocAPI.StopCoroutine(_coroutine);
			_coroutine = null;
		}
		if (_disappearCoroutine != null)
		{
			DolocAPI.StopCoroutine(_disappearCoroutine);
			_disappearCoroutine = null;
		}
	}

	public void OnTouch()
	{
		if (_TriggerMode)
		{
			Disappear();
		}
	}

	public void OnDisTouch()
	{
	}

	public void OnInteract()
	{
	}
}
