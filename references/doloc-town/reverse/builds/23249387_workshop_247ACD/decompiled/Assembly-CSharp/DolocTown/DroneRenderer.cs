using System;
using DolocTown.UI;
using RedSaw;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class DroneRenderer : DolocObject
{
	[SerializeField]
	protected Vector2 offsetReloadTip01;

	[SerializeField]
	protected Vector2 offsetBatteryTip;

	[SerializeField]
	private Transform shootOffsetPointer;

	[SerializeField]
	private Vector2 frequencyRange = new Vector2(0.44f, 1.58f);

	private readonly RSTimer timer = new RSTimer(5f);

	private ObjectPool<DroneComponentRenderer> componentPool;

	private SpriteRenderer spriteRenderer;

	private SecondOrderSystem secondOrderSystem;

	private DroneSwordCollider swordCollider;

	private DroneSwordBehaviour swordBehaviour;

	private BatteryTip batteryTip;

	private Light2D droneLight;

	[SerializeField]
	private bool shouldFollowTarget;

	private bool droneForeground;

	public float CurrentPowerProcess { get; private set; }

	public bool AutoChangeDir { get; set; } = true;


	public Vector2 reloadTipPosition => DolocAPI.WorldToScreen((Vector2)base.transform.position + offsetReloadTip01);

	public Vector2 batteryTipPosition => DolocAPI.WorldToScreen((Vector2)base.transform.position + offsetBatteryTip);

	public Sprite sprite
	{
		get
		{
			return spriteRenderer.sprite;
		}
		set
		{
			spriteRenderer.sprite = value;
			Animator component = GetComponent<Animator>();
			if (component != null)
			{
				component.enabled = false;
			}
		}
	}

	public Vector2 FollowTargetOffset
	{
		get
		{
			Sprite sprite = this.sprite;
			if (sprite == null)
			{
				return Vector2.zero;
			}
			return sprite.rect.size * 0.0625f;
		}
	}

	public RuntimeAnimatorController animatorController
	{
		set
		{
			Animator animator = GetComponent<Animator>();
			if (animator == null)
			{
				animator = base.gameObject.AddComponent<Animator>();
			}
			animator.runtimeAnimatorController = value;
			animator.enabled = value != null;
		}
	}

	public int SortingOrder
	{
		set
		{
			spriteRenderer.sortingOrder = value;
			componentPool.ForEach(delegate(DroneComponentRenderer obj)
			{
				obj.spriteRenderer.sortingOrder = value;
			});
		}
	}

	public string SortingLayerName
	{
		set
		{
			spriteRenderer.sortingLayerName = value;
			componentPool.ForEach(delegate(DroneComponentRenderer obj)
			{
				obj.spriteRenderer.sortingLayerName = value;
			});
		}
	}

	public bool shouldLightUp
	{
		get
		{
			return droneLight.enabled;
		}
		set
		{
			droneLight.enabled = value;
		}
	}

	public DroneSwordCollider SwordCollider => swordCollider;

	private Vector2 _shootPosition
	{
		get
		{
			float num = this.RayForNearestWall(3f);
			if (num < 3f)
			{
				float t = num / 3f;
				Vector3 b = shootOffsetPointer.position;
				return Vector3.Lerp(base.transform.position, b, t);
			}
			return shootOffsetPointer.position;
		}
	}

	public Vector2 ShootPosition => _shootPosition;

	public bool FaceRight
	{
		get
		{
			return base.transform.localScale.x > 0f;
		}
		set
		{
			base.transform.localScale = new Vector3(value ? 1 : (-1), 1f, 1f);
		}
	}

	public bool BatteryTipVisible
	{
		get
		{
			return batteryTip.isVisible;
		}
		set
		{
			batteryTip.SetVisible(value);
		}
	}

	public bool IsDash => swordBehaviour.IsDash;

	public void Init(Transform target)
	{
		try
		{
			base.__Init();
			shouldFollowTarget = true;
			componentPool = DolocGameAssets.GAME_ENTITY_DRONE_COMPONENT.CreatePool<DroneComponentRenderer>(base.transform);
			spriteRenderer = GetComponent<SpriteRenderer>();
			droneLight = GetComponentInChildren<Light2D>();
			secondOrderSystem = GetComponent<SecondOrderSystem>();
			secondOrderSystem.Init();
			secondOrderSystem.SetTarget(target, FollowTargetOffset);
			swordCollider = GetComponentInChildren<DroneSwordCollider>();
			swordCollider.Init();
			swordBehaviour = GetComponent<DroneSwordBehaviour>();
			swordBehaviour.Init();
			batteryTip = DolocAPI.uiSystem.GetFromPoolInScene<BatteryTip>();
			batteryTip.SetVisible(value: false);
			SetDroneForeground(value: true);
			Debug.Log("玩家无人机初始化完成");
		}
		catch (Exception exception)
		{
			Debug.LogError("生成玩家无人机时遇到异常");
			Debug.LogException(exception);
		}
	}

	public void SetDroneForeground(bool value)
	{
		droneForeground = value;
		if (droneForeground)
		{
			SortingLayerName = "WaterGround";
			SortingOrder = -1;
		}
		else
		{
			SortingLayerName = "Default";
			SortingOrder = 0;
		}
	}

	public void SetMoveSpeed(float t)
	{
		float frequency = Mathf.Lerp(frequencyRange.x, frequencyRange.y, Mathf.Clamp01(t));
		secondOrderSystem.SetFrequency(frequency);
	}

	public void PlayAnimation(string name)
	{
		Animator component = GetComponent<Animator>();
		if (component != null)
		{
			component.Play(name);
		}
	}

	public void OnFixedUpdate(float dt, bool followTarget)
	{
		if (shouldFollowTarget && followTarget)
		{
			FollowTarget(dt);
		}
		else
		{
			swordBehaviour.OnFixedUpdate(dt);
		}
		batteryTip.position = batteryTipPosition;
		if (timer.Tick(dt))
		{
			shouldLightUp = DolocAPI.archiveHandle.ShouldDroneLightUp;
		}
	}

	private void FollowTarget(float dt)
	{
		float num = secondOrderSystem.OnFixedUpdate(dt);
		if (AutoChangeDir && Mathf.Abs(num) > 0.01f)
		{
			base.transform.localScale = new Vector3((!(num < 0f)) ? 1 : (-1), 1f, 1f);
		}
		base.transform.rotation = Quaternion.Euler(0f, 0f, (0f - num) * 5f);
	}

	public void SetFollowEnabled(bool v)
	{
		shouldFollowTarget = v;
	}

	public void Dash(Vector2 direction, float dashDst, float dashSpeed, float recoilDur = 0.1f, Action touchCallback = null, Action postCallback = null)
	{
		shouldFollowTarget = false;
		FaceRight = direction.x > 0f;
		swordBehaviour.Dash(direction, dashDst, dashSpeed, touchCallback, delegate
		{
			postCallback?.Invoke();
			shouldFollowTarget = true;
			ResetPosition(base.transform.position);
		}, recoilDur);
	}

	public void SetShootOffset(Vector2 offset)
	{
		shootOffsetPointer.localPosition = offset;
	}

	public void ClearComponents()
	{
		componentPool.RecycleAll();
	}

	public DroneComponentRenderer RenderComponent(Sprite sprite, Vector3 position)
	{
		DroneComponentRenderer next = componentPool.Next;
		next.spriteRenderer.sprite = sprite;
		next.positionLocal = position;
		next.spriteRenderer.sortingOrder = spriteRenderer.sortingOrder;
		next.spriteRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
		return next;
	}

	public void SetFollowTarget(Transform target)
	{
		secondOrderSystem.SetTarget(target, Vector2.zero);
	}

	public void SetBatteryPercent(float v)
	{
		CurrentPowerProcess = v;
		batteryTip.SetValue(v);
	}

	public void HideBatteryTip()
	{
		batteryTip.SetVisible(value: false);
	}

	public void ShowBatteryTip()
	{
		batteryTip.SetVisible(value: true);
		batteryTip.position = batteryTipPosition;
	}

	public void ResetPosition()
	{
		secondOrderSystem.ResetPosition();
		batteryTip.position = batteryTipPosition;
	}

	public void ResetPosition(Vector2 position)
	{
		secondOrderSystem.ResetPosition(position);
		batteryTip.position = batteryTipPosition;
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value);
		if (batteryTip != null)
		{
			batteryTip.SetVisible(value);
		}
	}
}
