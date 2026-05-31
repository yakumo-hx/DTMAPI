using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

public class GameProcessEntrance : InteractableObject
{
	[SerializeField]
	private string gameProcessName;

	[SerializeField]
	private GameObject[] extraDisabledObjects;

	[SerializeField]
	private string dialogueAfterProcess;

	[SerializeField]
	private float fadeDuration = 1.5f;

	private void SetEnvEnabled(bool value)
	{
		StaticTarget[] componentsInChildren = GetComponentsInChildren<StaticTarget>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.SetActive(value);
		}
		if (!extraDisabledObjects.IsNullOrEmpty())
		{
			GameObject[] array = extraDisabledObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value);
			}
		}
	}

	private void StopProcess(bool isBreak)
	{
		Debug.Log("结束靶场流程");
		if (isBreak)
		{
			DolocAPI.StopGameProcess(gameProcessName);
		}
		DolocAPI.TransitFadeInout(delegate
		{
			SetEnvEnabled(value: true);
		}, fadeDuration, fadeDuration);
	}

	private void StartProcess()
	{
		Debug.Log("启动靶场流程");
		DolocAPI.TransitFadeInout(delegate
		{
			DolocAPI.RunGameProcess(gameProcessName, delegate
			{
				DolocAPI.TransitFadeInout(delegate
				{
					SetEnvEnabled(value: true);
				});
			});
			SetEnvEnabled(value: false);
		}, fadeDuration, fadeDuration);
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (gameProcessName.IsNullOrEmpty())
		{
			Debug.LogWarning("未设置目标流程图，启动失败..");
		}
		else
		{
			DolocAPI.ShowQuestionBox(DolocConfig.StaticTexts.UiOptionConfirmShootingRangeExit, Exit);
		}
	}

	private void HandleOptions(string text)
	{
		if (text == DolocConfig.StaticTexts.UiOptionShootingRangeStart)
		{
			StartProcess();
		}
		else if (text == DolocConfig.StaticTexts.UiOptionShootingRangeEnd)
		{
			StopProcess(isBreak: true);
		}
		else if (text == DolocConfig.StaticTexts.UiOptionShootingRangeExit)
		{
			Exit();
		}
	}

	private void Exit()
	{
		DolocAPI.StopGameProcess(gameProcessName);
		DolocAPI.StartDialogueNode(dialogueAfterProcess);
	}
}
