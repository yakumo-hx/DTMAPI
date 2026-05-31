using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class MovableObjectSwitch : InteractableObject
{
	[SerializeField]
	private MovableObjectTarget target;

	[SerializeField]
	private bool useCamAnim = true;

	[SerializeField]
	private Transform camMark;

	[SerializeField]
	private float camSpeed = 30f;

	private bool inAnimation;

	protected override void __Init()
	{
		base.__Init();
		if (camMark != null)
		{
			camMark.gameObject.SetActive(value: false);
		}
	}

	protected override void OnLoadData(Room room)
	{
		base.OnLoadData(room);
		if (!(target == null))
		{
			bool value = base.archiveData.LoadToggleState(base.guid);
			target.SetState(value, useAnimation: false);
		}
	}

	protected override void OnToggle(bool value, bool visible)
	{
		base.OnToggle(value, visible);
		if (visible)
		{
			DolocAPI.Sound.PostSoundEvent(value ? SoundEvents.PLAY_CHARACTER_SWITCH_ON : SoundEvents.PLAY_CHARACTER_SWITCH_OFF);
		}
		if (target == null)
		{
			return;
		}
		base.archiveData.SaveToggleState(target.guid, value);
		if (!inAnimation)
		{
			if (base.isTouched && useCamAnim && camMark != null)
			{
				CutSceneState.PlayTask(ZoomTargetAnim(value)).Forget();
			}
			else
			{
				target.SetState(value, useAnimation: false);
			}
		}
	}

	private async UniTask ZoomTargetAnim(bool value)
	{
		inAnimation = true;
		Vector2 camPos = DolocAPI.cameraController.position2d;
		bool wait = true;
		DolocAPI.cameraController.MoveToBySpeed(camMark.position, camSpeed, Ease.Linear, delegate
		{
			wait = false;
		});
		await UniTask.WaitUntil(() => !wait);
		await UniTask.Delay(500);
		target.SetState(value, useAnimation: true);
		await UniTask.Delay(1000);
		wait = true;
		DolocAPI.cameraController.MoveToBySpeed(camPos, camSpeed, Ease.Linear, delegate
		{
			wait = false;
		});
		await UniTask.WaitUntil(() => !wait);
		inAnimation = false;
	}
}
