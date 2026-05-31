using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/其他")]
[Name("弹出表情", 0)]
[Description("针对目标对象弹出一个表情")]
public class DialogueTask_RaiseEmotion : DialogueTask
{
	public enum EntityLabel
	{
		Player,
		Npc
	}

	[SerializeField]
	public EmotionName emotionName;

	[SerializeField]
	public EntityLabel entityLabel;

	public override string taskTitle => $"在{entityLabel}附近弹出表情<{emotionName}>";

	private Transform TargetTransform
	{
		get
		{
			if (entityLabel != 0)
			{
				return DolocAPI.CurrentDialogueHostTransform;
			}
			return DolocAPI.AgentTransform;
		}
	}

	public override void DoAction(Graph graph)
	{
		Transform targetTransform = TargetTransform;
		if (targetTransform != null)
		{
			DolocAPI.RaiseEmotion(targetTransform, emotionName);
		}
	}
}
