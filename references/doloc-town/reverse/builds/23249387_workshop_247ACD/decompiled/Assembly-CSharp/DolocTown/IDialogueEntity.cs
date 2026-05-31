using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DolocTown;

public interface IDialogueEntity
{
	string EntityId { get; }

	Transform EntityTransform { get; }

	Vector2 WorldPosition { get; }

	Vector2 UiPopPosition { get; }

	bool DisableActing { get; }

	float DefaultWalkSpeed { get; }

	void StartSay();

	void StopSay();

	void FaceLeft(bool value = true);

	void LookAt(float x);

	void ShowEmotion(string emotionName);

	void SetWorldPosition(Vector2 position);

	void SetToMarkPoint(string markPointId);

	UniTask PlayAnimationAsync(string animName, bool force);

	UniTask WalkTo(Vector2 pos, float speed);

	bool GetSortingOrder(out string sortingLayerName, out int order);

	void SetSortingOrder(string sortingLayerName, int order);

	void SetEntityVisible(bool value);
}
