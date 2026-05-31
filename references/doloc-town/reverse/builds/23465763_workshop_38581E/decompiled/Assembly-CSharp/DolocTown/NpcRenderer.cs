using System;
using Cysharp.Threading.Tasks;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator), typeof(SpriteRenderer), typeof(Collider2D))]
[GameEntityManager("/city/npcs", DolocGameAssets.GAME_ENTITY_NPC, CustomManagement = true)]
public class NpcRenderer : CharacterRenderer, IDialogueEntity, IInteractable
{
	[SerializeField]
	private SpriteRenderer eventFlagRenderer;

	[SerializeField]
	private float z = -0.3f;

	private Collider2D _collider2D;

	private float _manualSortingZ;

	private Action _onInteract;

	private readonly RSTimer timer = new RSTimer();

	private bool hasTouched;

	public NpcMover mover { get; private set; }

	public Npc npc { get; set; }

	public Vector2 emoPopPosition => eventFlagRenderer.transform.position;

	public Sprite eventFlagSprite
	{
		get
		{
			return eventFlagRenderer.sprite;
		}
		set
		{
			eventFlagRenderer.sprite = value;
		}
	}

	private bool eventFlagVisible
	{
		get
		{
			return eventFlagRenderer.gameObject.activeSelf;
		}
		set
		{
			eventFlagRenderer.gameObject.SetActive(value);
		}
	}

	public bool lockFlipStatus { get; set; }

	private bool IsInvoked => npc?.isInvoked ?? false;

	private Vector2 InvokeTargetPosition => (npc != null) ? npc.invokedTarget.transform.position : default(Vector3);

	public override Vector2 positionWS
	{
		set
		{
			base.transform.position = new Vector3(value.x, value.y, _manualSortingZ);
		}
	}

	public override string EntityId => npc.NpcName;

	public new Transform EntityTransform => base.transform;

	public new Vector2 UiPopPosition => DolocAPI.CalcUiPopPosition(base.transform, 0.75f);

	public override bool DisableActing => npc.proto.DisableActing;

	public Vector2 UiPopPositionWS => DolocAPI.CalcPopPosition(base.transform, 0.75f);

	public override float DefaultWalkSpeed => npc?.proto.WalkSpeed ?? 0f;

	public bool OnlyTouch => false;

	public bool CanInteractContinues => false;

	private void OnEnable()
	{
		lockFlipStatus = false;
	}

	public void Update()
	{
		if (lockFlipStatus || npc.proto.DisableActing)
		{
			return;
		}
		if (IsInvoked)
		{
			LookAt(InvokeTargetPosition);
		}
		else if (npc.disableFreeActing && npc.overrideFaceLeft.HasValue)
		{
			FaceLeft(npc.overrideFaceLeft.Value);
		}
		else if (npc.IsInStreetMission)
		{
			if (hasTouched)
			{
				LookAt(DolocAPI.AgentPosition);
			}
			else if (timer.Tick(Time.deltaTime))
			{
				base.flipX = UnityEngine.Random.value > 0.5f;
				timer.SetInterval(UnityEngine.Random.Range(5f, 10f));
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		mover = GetComponent<NpcMover>();
		mover.Initialize(this);
		_collider2D = GetComponent<Collider2D>();
		GetComponentInChildren<NpcVision>().Init();
	}

	public void SetNpcManualSortingOrder(int id)
	{
		_manualSortingZ = z - (float)id * 1E-05f;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		base.transform.localScale = Vector3.one;
		lockFlipStatus = false;
		if (npc != null)
		{
			npc = null;
			mover.ResetMover();
			SetSortingOrder("Default", 0);
		}
	}

	public bool CheckAnimation(string animName)
	{
		if (animName.IsNullOrEmpty())
		{
			return false;
		}
		return animator.GetCurrentAnimatorStateInfo(0).IsName(animName);
	}

	public void SetEventFlagState(bool value)
	{
		eventFlagVisible = value;
	}

	public void UpdateDirection()
	{
		LookAt(DolocAPI.AgentPosition);
	}

	protected override void OnSay()
	{
		if (npc != null)
		{
			eventFlagRenderer.gameObject.SetActive(value: false);
			PlayAnimation("say", force: false);
		}
	}

	protected override void OnStopSay()
	{
		if (npc != null)
		{
			PlayAnimation("idle", force: false);
		}
	}

	public override void SetWorldPosition(Vector2 position)
	{
		StopWalk();
		positionWS = position;
		npc?.ForceSetPosition(position);
	}

	public override void SetToMarkPoint(string markPointId)
	{
		StopWalk();
		npc?.ManualSetToMarkPoint(markPointId);
	}

	public override async UniTask WalkTo(Vector2 pos, float speed)
	{
		await UniTask.NextFrame();
		if (npc == null)
		{
			return;
		}
		bool originLockFlipStatus = lockFlipStatus;
		lockFlipStatus = false;
		await InternalWalkTo(pos, speed, delegate(Vector2 pos)
		{
			if (npc == null)
			{
				return false;
			}
			npc.ForceSetPosition(pos);
			return true;
		}, delegate
		{
			lockFlipStatus = originLockFlipStatus;
		});
	}

	public void __Idle()
	{
		PlayAnimation("idle", force: false);
	}

	public void OnTouch()
	{
		hasTouched = true;
		npc?.OnTouch();
	}

	public void OnDisTouch()
	{
		hasTouched = false;
		npc?.OnDisTouch();
	}

	public void OnInteract()
	{
		npc?.OnInteract();
	}
}
