using DG.Tweening;
using DolocTown.Editor;
using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
[GameEntityManager("/farm/automate_bot", DolocGameAssets.GAME_ENTITY_AUTOMATE_BOT, CustomManagement = true)]
public class AutomateBotRenderer : GameEntity, ISteeringParamsProvider
{
	[SerializeField]
	protected float maxSpeed = 15f;

	[SerializeField]
	protected float parkingRadius = 3f;

	[SerializeField]
	protected float responseDuration = 0.75f;

	public Tween fadeAnimation;

	private Steering steering;

	private SteeringDebug debug;

	private Vector2 currentTarget;

	private bool shouldRun;

	private Material currentMaterial;

	public bool IsRunning => shouldRun;

	public Vector2 CurrentTarget => currentTarget;

	public AutomateBot CurrentBot => Bot;

	public SpriteRenderer Sr { get; private set; }

	public Animator Animator { get; private set; }

	public RuntimeAnimatorController AnimatorController
	{
		get
		{
			return Animator.runtimeAnimatorController;
		}
		set
		{
			Animator.runtimeAnimatorController = value;
		}
	}

	public Sprite Sprite
	{
		get
		{
			return Sr.sprite;
		}
		set
		{
			Sr.sprite = value;
		}
	}

	public Color Color
	{
		get
		{
			return Sr.color;
		}
		set
		{
			Sr.color = value;
		}
	}

	public int PowerSpriteIndex
	{
		set
		{
			currentMaterial.SetInt("_Index", value);
		}
	}

	public AutomateBot Bot { get; set; }

	float ISteeringParamsProvider.MaxSpeed => maxSpeed;

	float ISteeringParamsProvider.ParkingRadius => parkingRadius;

	float ISteeringParamsProvider.ResponseDuration => responseDuration;

	public bool ToggleLocalBattery
	{
		set
		{
			currentMaterial.SetFloat("_ToggleLocalBattery", value ? 1 : 0);
		}
	}

	public float[] LocalBatteryInfos
	{
		set
		{
			if (value != null && value.Length >= 4)
			{
				Vector4 value2 = new Vector4(value[0], value[1], value[2], value[3]);
				currentMaterial.SetVector("_SubInfo", value2);
			}
		}
	}

	public void ShowUp(float duration = 1f)
	{
		fadeAnimation?.Kill();
		Sr.color = new Color(1f, 1f, 1f, 0f);
		fadeAnimation = Sr.DOFade(1f, duration).SetEase(Ease.InOutSine).OnComplete(delegate
		{
			fadeAnimation = null;
		});
	}

	public void Move(Vector2 target, float duration = 1f)
	{
		SetRunning(value: true);
		Sr.flipX = base.transform.position.x < target.x;
		maxSpeed = Vector2.Distance(base.transform.position, target) / duration;
		currentTarget = target;
	}

	public void RestoreMove(Vector2 velocityDir)
	{
		SetRunning(value: true);
		currentTarget = position2d + velocityDir * maxSpeed * 0.5f;
		debug.SetVelocity(velocityDir * maxSpeed);
		steering.SetVelocity(velocityDir * maxSpeed);
	}

	public void DrawPath(Vector2Int[] path, Vector2 offset)
	{
		PathDrawer component = GetComponent<PathDrawer>();
		if (!(component == null))
		{
			component.Path = path;
			component.Offset = offset;
		}
	}

	public void OnCharge(Vector3 position)
	{
		this.position = position;
		Animator.Play("charge");
		GetComponent<SpriteRenderer>().sortingLayerName = "Default";
		LocalBatteryInfos = Bot.proto.Appearance_Ref.UVInfosLocalBatteryInfosCharge;
		DolocAPI.RaiseInstantAnimEffects(position, InstAnimEffectType.SMALL_SMOKE);
		if (Vector2.Distance(DolocAPI.AgentPosition, position) < 10f)
		{
			DolocAPI.cameraController.ShakeScreen(0.2f, 0.15f);
		}
	}

	public void OnStopCharge()
	{
		Animator.Play("idle");
		GetComponent<SpriteRenderer>().sortingLayerName = "GroundFront";
		LocalBatteryInfos = Bot.proto.Appearance_Ref.UVInfosLocalBatteryInfosIdle;
	}

	public void SetRunning(bool value)
	{
		if (Bot != null)
		{
			shouldRun = value;
		}
	}

	protected void FixedUpdate()
	{
		if (shouldRun)
		{
			Vector3 vector = base.transform.position;
			base.transform.position = debug.Move(vector, currentTarget, Time.fixedDeltaTime);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
		currentMaterial = new Material(Sr.sharedMaterial);
		Sr.sharedMaterial = currentMaterial;
		Animator = GetComponent<Animator>();
		steering = new Steering(parkingRadius, responseDuration, maxSpeed);
		debug = new SteeringDebug(this);
		shouldRun = false;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		Sr.color = Color.white;
		if (Bot != null)
		{
			Bot.Renderer = null;
			Bot = null;
		}
		shouldRun = false;
	}

	private void OnDestroy()
	{
		Object.Destroy(currentMaterial);
	}
}
