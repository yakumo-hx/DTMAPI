using System.Collections.Generic;
using DolocTown;
using RedSaw;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GroundChecker : MonoBehaviour
{
	[SerializeField]
	private BoxCollider2D collider2d;

	private int walkableCounter;

	private readonly List<PlatformEntityOneway> onewayPlatforms = new List<PlatformEntityOneway>();

	public bool isTouched { get; private set; }

	public bool isTouchedExcludePlatforms
	{
		get
		{
			if (isTouched)
			{
				return onewayPlatforms.Count == 0;
			}
			return false;
		}
	}

	public int PlatformCount => onewayPlatforms.Count;

	public void Reset()
	{
		walkableCounter = 0;
		isTouched = false;
		onewayPlatforms.Clear();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (RSUtils.LayerMaskCheck(other.gameObject, DolocAPI.gameConfig.walkableMask))
		{
			walkableCounter++;
			isTouched = true;
		}
		PlatformEntityOneway component = other.GetComponent<PlatformEntityOneway>();
		if (component != null)
		{
			onewayPlatforms.Add(component);
			if (!component.ignoreMsg)
			{
				DolocAPI.Broadcast(GameEventType.STAND_PLATFORM);
			}
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (RSUtils.LayerMaskCheck(other.gameObject, DolocAPI.gameConfig.walkableMask))
		{
			walkableCounter = Mathf.Max(0, walkableCounter - 1);
			isTouched = walkableCounter > 0;
		}
		PlatformEntityOneway component = other.GetComponent<PlatformEntityOneway>();
		if (component != null)
		{
			onewayPlatforms.Remove(component);
		}
	}
}
