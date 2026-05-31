using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Collider2D))]
public class MonsterSight : DolocObject
{
	[SerializeField]
	private float enemyThreshold = 5f;

	[SerializeField]
	private float minAlertAddition = 0.5f;

	[SerializeField]
	private float maxAlertAddition = 2f;

	[SerializeField]
	private float maxAlertDuration = 8f;

	[SerializeField]
	public float tooFarDistance = 40f;

	private CircleCollider2D _collider;

	private readonly RSTimerLock _emotionCD = new RSTimerLock(10f);

	private Transform _lockedTarget;

	private float _alertDuration;

	public CircleCollider2D Collider
	{
		get
		{
			if (_collider == null)
			{
				_collider = GetComponent<CircleCollider2D>();
			}
			return _collider;
		}
	}

	public float CircleRadius => Collider.radius;

	public Transform LockedTarget { get; private set; }

	private bool CheckDisguiseValid()
	{
		MonsterController componentInParent = base.gameObject.GetComponentInParent<MonsterController>();
		if (componentInParent == null)
		{
			return false;
		}
		if (componentInParent.Monster.HasBeenAttacked)
		{
			return false;
		}
		if (DolocAPI.AgentEquipmentParams.ShieldSightOfMonsterNames.Contains(componentInParent.Monster.Name))
		{
			Debug.Log("<color=#00ff00>由于主角装备了伪装装备，怪物无法识别主角为敌人</color>");
			return true;
		}
		return false;
	}

	private bool IsEnemy(GameObject other)
	{
		if (!other.gameObject.activeSelf)
		{
			return false;
		}
		if (other.GetComponent<BodyController>() != null)
		{
			return !CheckDisguiseValid();
		}
		if (other.GetComponent<MotorInteractable>() != null)
		{
			if (CheckDisguiseValid())
			{
				return false;
			}
			return DolocAPI.IsAgentRiding;
		}
		return false;
	}

	private void DecayAlertTime(float dt)
	{
		if (!(_alertDuration <= 0f))
		{
			_alertDuration -= dt;
			if (_alertDuration <= 0f)
			{
				LockedTarget = null;
			}
		}
	}

	private void AddAlertTime(float dt)
	{
		float num = Vector2.Distance(base.transform.position, _lockedTarget.position);
		float num2 = Mathf.Lerp(minAlertAddition, maxAlertAddition, num / CircleRadius) * dt;
		_alertDuration += num2;
		if (_alertDuration >= maxAlertDuration)
		{
			_alertDuration = maxAlertDuration;
		}
		if (LockedTarget == null && _alertDuration >= enemyThreshold)
		{
			LockedTarget = _lockedTarget;
			RaiseEmotion(EmotionName.ANGRY);
		}
	}

	private void RaiseEmotion(EmotionName emotion)
	{
		if (!_emotionCD.IsLocked)
		{
			DolocAPI.RaiseEmotion(base.transform.parent, emotion);
			_emotionCD.Lock();
		}
	}

	public void SetTarget(Transform target)
	{
		if (!(_lockedTarget != target))
		{
			LockedTarget = target;
		}
	}

	public void ClearEnemy()
	{
		LockedTarget = null;
		_lockedTarget = null;
		_alertDuration = 0f;
	}

	public void ValidateSight()
	{
		if (!Collider.enabled)
		{
			Collider.enabled = true;
		}
	}

	private void Update()
	{
		_emotionCD.Tick(Time.deltaTime);
		if (_lockedTarget != null)
		{
			if (!IsEnemy(_lockedTarget.gameObject))
			{
				ClearEnemy();
			}
			else
			{
				AddAlertTime(Time.deltaTime);
			}
		}
		else
		{
			DecayAlertTime(Time.deltaTime);
		}
	}

	private void _RefreshEnemy()
	{
		Collider.enabled = false;
		UniTask.DelayFrame(1).ContinueWith(delegate
		{
			if (!(_lockedTarget == null))
			{
				Collider.enabled = true;
			}
		}).Forget();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!(_lockedTarget != null) && IsEnemy(other.gameObject))
		{
			_lockedTarget = other.transform;
			RaiseEmotion((!RandomUtils.Dice(0.3f)) ? EmotionName.CONFUSE : EmotionName.AMAZING);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!(_lockedTarget != null) && IsEnemy(other.gameObject))
		{
			_lockedTarget = other.transform;
			RaiseEmotion((!RandomUtils.Dice(0.3f)) ? EmotionName.CONFUSE : EmotionName.AMAZING);
		}
	}
}
