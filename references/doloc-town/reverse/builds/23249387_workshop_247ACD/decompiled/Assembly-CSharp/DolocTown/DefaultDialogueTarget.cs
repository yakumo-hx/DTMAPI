using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DolocTown;

public class DefaultDialogueTarget : IDialogueEntity
{
	public string EntityId => "";

	public Transform EntityTransform => null;

	public Vector2 WorldPosition => DolocAPI.AgentRenderer.WorldPosition;

	public Vector2 UiPopPosition => DolocAPI.AgentRenderer.UiPopPosition;

	public bool DisableActing => true;

	public float DefaultWalkSpeed => 0f;

	public void StartSay()
	{
	}

	public void StopSay()
	{
	}

	public void FaceLeft(bool value = true)
	{
	}

	public void LookAt(float x)
	{
	}

	public void ShowEmotion(string emotionName)
	{
	}

	public void SetWorldPosition(Vector2 position)
	{
	}

	public void SetToMarkPoint(string markPointId)
	{
	}

	public async UniTask PlayAnimationAsync(string animName, bool force)
	{
		await UniTask.NextFrame();
	}

	public async UniTask WalkTo(Vector2 pos, float speed)
	{
		await UniTask.NextFrame();
	}

	public bool GetSortingOrder(out string sortingLayerName, out int order)
	{
		sortingLayerName = "Default";
		order = 0;
		return true;
	}

	public void SetSortingOrder(string sortingLayerName, int order)
	{
	}

	public void SetEntityVisible(bool value)
	{
	}
}
