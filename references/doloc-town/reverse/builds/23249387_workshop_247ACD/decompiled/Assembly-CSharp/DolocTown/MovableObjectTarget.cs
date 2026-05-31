using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class MovableObjectTarget : InteractableObject
{
	[SerializeField]
	private MovableObjectSwitch objSwitch;

	[SerializeField]
	private float operateDuration = 0.5f;

	[SerializeField]
	private float liftDistance = 3f;

	private Vector3 originPos;

	private Vector3 targetPos;

	protected override void __Init()
	{
		base.__Init();
		Vector3 vector = new Vector3(0f, liftDistance * 1.5f, 0f);
		originPos = base.transform.position;
		targetPos = originPos + base.transform.rotation * vector;
	}

	protected override void OnLoadData(Room room)
	{
		base.OnLoadData(room);
		if (!(objSwitch == null))
		{
			bool value = base.archiveData.LoadToggleState(objSwitch.guid) || base.archiveData.LoadToggleState(base.guid);
			SetState(value, useAnimation: false);
		}
	}

	private void OnSwitchOn(bool useAnimation)
	{
		base.transform.DOMove(targetPos, useAnimation ? operateDuration : 0f);
	}

	private void OnSwitchOff(bool useAnimation)
	{
		base.transform.DOMove(originPos, useAnimation ? operateDuration : 0f);
	}

	public void SetState(bool value, bool useAnimation)
	{
		if (value)
		{
			OnSwitchOn(useAnimation);
		}
		else
		{
			OnSwitchOff(useAnimation);
		}
	}
}
