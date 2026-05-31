using Cysharp.Threading.Tasks;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown;

public class AnimatedGate : InteractableObject, IGate
{
	[SerializeField]
	private string id;

	[SerializeField]
	private SpriteRenderer movableTarget;

	[SerializeField]
	private Vector2 moveDestination;

	[SerializeField]
	private float noFadeDistance = 30f;

	[SerializeField]
	[Range(0.01f, 2f)]
	private float moveSpeed = 1f;

	[SerializeField]
	private Ease speedEase = Ease.InCubic;

	[SerializeField]
	private SpriteRenderer boardingPoint;

	[SerializeField]
	private float boardingWaiteDuration;

	[SerializeField]
	private bool adjustZOnBoardingPoint;

	[SerializeField]
	private SpriteRenderer waitingPoint;

	[SerializeField]
	private bool playWalkAnimDuringMove;

	[SerializeField]
	private float fadeDuration = 1f;

	[SerializeField]
	private float waitDuration = 0.75f;

	[SerializeField]
	private float launchDuration = 1f;

	[SerializeField]
	[Range(0f, 1f)]
	private float launchShakeStrength = 0.5f;

	private PortalInfo _portalProto;

	private bool interactWithOtherKey;

	protected override bool shouldShowTipInRiding => true;

	protected override string InteractKeyName => ((IGate)this).GetInteractKeyName();

	private PortalInfo PortalProto
	{
		get
		{
			if (_portalProto == null)
			{
				_portalProto = DolocConfig.Tables.TbPortal.GetOrDefault(id);
			}
			return _portalProto;
		}
	}

	public bool AvailableToMotor => PortalProto?.AvailableToMotor ?? false;

	public bool NeedInteract => true;

	public bool KeepHorizontalSpeed => PortalProto?.KeepHorizontalSpeed ?? false;

	public bool KeepVerticalSpeed => PortalProto?.KeepVerticalSpeed ?? false;

	public PortalInteractKey InteractKey => PortalProto?.InteractKeyType ?? PortalInteractKey.None;

	protected override void __Init()
	{
		base.__Init();
		if (PortalProto == null)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	void IGate.OnInteract()
	{
		interactWithOtherKey = true;
		((IInteractable)this).OnInteract();
	}

	void IGate.OnTouch()
	{
		((IInteractable)this).OnTouch();
	}

	void IGate.OnDisTouch()
	{
		((IInteractable)this).OnDisTouch();
	}

	protected sealed override void OnInteract()
	{
		if (DolocAPI.IsCurrentStateSupportInteract && PortalProto != null && (interactWithOtherKey || PortalProto.InteractKeyType == PortalInteractKey.Interact) && DolocAPI.CheckAvailableInCurrentState(PortalProto.Id))
		{
			interactWithOtherKey = false;
			base.OnInteract();
			DoTransport(PortalProto?.TargetId).Forget();
		}
	}

	protected override string GetDefaultTipText()
	{
		if (PortalProto == null || CheckTipShouldOverwrite())
		{
			return base.GetDefaultTipText();
		}
		return PortalProto.EnableTip;
	}

	private async UniTask TransportAnim(string markPointId)
	{
		bool colliderOriginEnabled = _collider.enabled;
		_collider.enabled = false;
		IDialogueEntity player = DolocAPI.GetDialogueTargetViewOrDefault("player");
		player.GetSortingOrder(out var originLayerName, out var originOrder);
		float magnitude = moveDestination.magnitude;
		bool faceLeft = (waitingPoint.transform.localScale.y < 0f) ^ waitingPoint.flipX;
		float moveDuration = Mathf.Abs(magnitude / (moveSpeed * 10f));
		float noFadeDuration = Mathf.Abs(noFadeDistance / (moveSpeed * 10f));
		Vector2 boardingPos = new Vector2(boardingPoint.transform.position.x, DolocAPI.AgentPosition.y);
		await player.WalkTo(boardingPos, player.DefaultWalkSpeed);
		if (adjustZOnBoardingPoint)
		{
			player.SetSortingOrder(boardingPoint.sortingLayerName, boardingPoint.sortingOrder);
		}
		Vector3 waitingPos = waitingPoint.transform.position;
		if (Mathf.Abs(waitingPos.x - boardingPos.x) > 0.1f)
		{
			player.LookAt(waitingPos.x);
			await UniTask.Delay((int)(1000f * boardingWaiteDuration));
			await player.WalkTo(waitingPos, player.DefaultWalkSpeed);
		}
		player.FaceLeft(faceLeft);
		player.SetSortingOrder(waitingPoint.sortingLayerName, waitingPoint.sortingOrder);
		await UniTask.Delay((int)(waitDuration * 1000f));
		if (launchDuration > 0f)
		{
			if (launchShakeStrength > 0f)
			{
				DolocAPI.cameraController.ShakeScreen(launchDuration, launchShakeStrength * 0.1f);
			}
			await UniTask.Delay((int)(launchDuration * 1000f));
		}
		Sequence sequence = DOTween.Sequence();
		if (movableTarget != null)
		{
			Vector3 endValue = movableTarget.transform.position + new Vector3(moveDestination.x, moveDestination.y);
			sequence.Join(movableTarget.transform.DOMove(endValue, moveDuration).SetEase(speedEase));
		}
		Vector3 endValue2 = DolocAPI.AgentPosition + new Vector3(moveDestination.x, moveDestination.y);
		sequence.Join(DolocAPI.agent.transform.DOMove(endValue2, moveDuration).SetEase(speedEase));
		if (playWalkAnimDuringMove)
		{
			player.PlayAnimationAsync("walk", force: false);
		}
		sequence.Play();
		await UniTask.Delay((int)(noFadeDuration * 1000f));
		DolocAPI.ppm.FadeIn(fadeDuration);
		await UniTask.Delay((int)(fadeDuration * 1000f));
		sequence.Kill();
		_collider.enabled = colliderOriginEnabled;
		DolocAPI.agent.StateManager.Overwrite<AgentStateIdle>();
		player.SetSortingOrder(originLayerName, originOrder);
	}

	private async UniTaskVoid DoTransport(string markPointId)
	{
		if (!markPointId.IsNullOrEmpty())
		{
			DisableOutline();
			HideTip();
			if (DolocAPI.userSettings.usePortalAnimation && !DolocAPI.IsAgentRiding)
			{
				DolocAPI.SetResidentUiInteractable(value: false);
				await UniTask.DelayFrame(1);
				await CutSceneState.PlayTask(TransportAnim(markPointId), null, forceHideBasicTip: true, forceHideOperationTip: false);
			}
			DolocAPI.archiveHandle.farmData.mapManager.SetPortalVisited(id);
			DolocAPI.DoTransport(markPointId);
		}
	}
}
