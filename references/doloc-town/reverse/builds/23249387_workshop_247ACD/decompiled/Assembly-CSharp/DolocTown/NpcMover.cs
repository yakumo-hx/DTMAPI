using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(NpcRenderer))]
public class NpcMover : MonoBehaviour
{
	private NpcRenderer _npcRenderer;

	private bool isPaused;

	private bool isMoving;

	private float duration;

	private Vector2 velocity;

	public void Initialize(NpcRenderer renderer)
	{
		_npcRenderer = renderer;
	}

	private void FixedUpdate()
	{
		if (isMoving && !isPaused)
		{
			if (duration > 0f)
			{
				duration -= Time.fixedDeltaTime;
				base.transform.Translate(velocity * Time.fixedDeltaTime);
			}
			else
			{
				isMoving = false;
				_npcRenderer.PlayAnimation("idle", force: false);
			}
		}
	}

	public void SetPaused(bool value)
	{
		isPaused = value;
	}

	public void Move(float delta, float speed)
	{
		if (delta != 0f)
		{
			float num = Mathf.Abs(delta);
			duration = num / speed;
			isMoving = true;
			float num2 = Mathf.Sign(delta);
			velocity = new Vector2(num2 * speed, 0f);
			base.transform.localScale = new Vector3(num2, 1f, 1f);
			_npcRenderer.PlayAnimation("walk", force: false);
		}
	}

	public void StopMove()
	{
		isMoving = false;
		_npcRenderer.PlayAnimation("idle", force: false);
	}

	public void ResetMover()
	{
		isPaused = false;
		isMoving = false;
		duration = 0f;
	}
}
