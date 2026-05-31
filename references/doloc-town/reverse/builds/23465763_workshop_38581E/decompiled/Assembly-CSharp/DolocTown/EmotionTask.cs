using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown;

[Category("多洛可小镇/怪物")]
[Name("弹出表情", 0)]
[Description("在当前单位的头顶弹出一个表情")]
public class EmotionTask : ActionTask
{
	[RequiredField]
	public EmotionName emotionName;

	protected override void OnExecute()
	{
		DolocAPI.emotionSystem.Raise(base.agent.transform, emotionName);
		EndAction(success: true);
	}
}
