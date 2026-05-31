using UnityEngine;

namespace DolocTown;

public class WaterDropChecker : InteractableObjectExclude
{
	[SerializeField]
	public SpriteRenderer wakeupRenderer;

	[SerializeField]
	public bool faceLeftAfterWakeup;

	[SerializeField]
	public string targetRoomId;

	protected override ITouchCheckStrategy touchChecker { get; set; }

	public float passThroughTime { get; private set; }

	public Vector2 wakeupPosition => wakeupRenderer.transform.position;

	protected override void __Init()
	{
		base.__Init();
		touchChecker = new DrowningTriggerChecker();
		passThroughTime = -1f;
		wakeupRenderer.gameObject.SetActive(value: false);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		passThroughTime = Time.time;
	}
}
