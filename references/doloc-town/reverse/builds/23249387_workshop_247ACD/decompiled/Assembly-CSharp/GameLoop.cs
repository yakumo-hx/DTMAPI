using System;
using System.Collections;
using RedSaw;
using UnityEngine;

public class GameLoop : MonoBehaviour
{
	private GameStateMachine userInput;

	private readonly RSTimer _secondTimer = new RSTimer();

	public bool IsPaused { get; set; }

	public bool IsGlobalPaused { get; set; }

	public void SetEnabled(bool value)
	{
		base.enabled = value && userInput != null;
	}

	public void Init(GameStateMachine userInput)
	{
		base.enabled = false;
		this.userInput = userInput;
	}

	private void Update()
	{
		userInput.Update(Time.deltaTime);
		if (userInput.CurrentState == null)
		{
			userInput.ClearState(DolocAPI.gameStateManager.normalGameState);
		}
	}

	private void LateUpdate()
	{
		userInput.LateUpdate(Time.deltaTime);
	}

	private void FixedUpdate()
	{
		userInput.FixedUpdate(Time.fixedDeltaTime);
		if (!IsPaused && !IsGlobalPaused && DolocAPI.archiveHandle != null && _secondTimer.Tick(Time.fixedDeltaTime))
		{
			DolocAPI.archiveHandle.Update();
		}
	}

	public Coroutine Delay(Action callback, float time)
	{
		return StartCoroutine(__Wait(callback, time));
	}

	private IEnumerator __Wait(Action callback, float time)
	{
		yield return new WaitForSeconds(time);
		callback?.Invoke();
	}

	public void DelayFrame(Action callback, int frame)
	{
		StartCoroutine(__WaitFrame(callback, frame));
	}

	private IEnumerator __WaitFrame(Action callback, int frame)
	{
		frame = Mathf.Max(1, frame);
		while (frame-- > 0)
		{
			yield return new WaitForEndOfFrame();
		}
		callback?.Invoke();
	}
}
