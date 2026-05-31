using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇")]
[Name("输出", 0)]
[Description("在游戏内部的控制台输出一句话")]
public class DialogueTask_Output : DialogueTask
{
	[RequiredField]
	[SerializeField]
	public string _outputContent = "hello world";

	[SerializeField]
	public Color _outputColor = Color.white;

	public override string taskTitle => "log:\"" + _outputContent.CapLength(15) + "\"";

	public override void DoAction(Graph graph)
	{
		DolocAPI.output(_outputContent, _outputColor);
	}
}
