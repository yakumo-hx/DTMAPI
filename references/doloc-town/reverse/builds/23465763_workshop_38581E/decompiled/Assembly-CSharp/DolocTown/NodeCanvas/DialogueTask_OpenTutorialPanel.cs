using Cysharp.Threading.Tasks;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("弹出教程页面", 0)]
[Description("打开教程页面")]
public class DialogueTask_OpenTutorialPanel : DialogueTask
{
	[RequiredField]
	public string imgName;

	public bool waitUntilNormalState = true;

	public override string taskTitle => "弹出教程<" + imgName + ">";

	public override void DoAction(Graph graph)
	{
		DoExtension().Forget();
	}

	private async UniTaskVoid DoExtension()
	{
		await UniTask.NextFrame();
		if (waitUntilNormalState)
		{
			await UniTask.WaitUntil(() => DolocAPI.IsCurrentStateSupportCutscenes);
		}
		DolocAPI.EnterUI((TutorialPanelUiState state) => state.HandleStartUpArgs(imgName));
	}
}
