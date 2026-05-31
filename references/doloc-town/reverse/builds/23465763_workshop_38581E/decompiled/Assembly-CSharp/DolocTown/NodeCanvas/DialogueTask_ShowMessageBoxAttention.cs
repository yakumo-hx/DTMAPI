using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("弹出重要提示", 0)]
[Category("多洛可小镇/UI")]
public class DialogueTask_ShowMessageBoxAttention : DialogueTask
{
	[SerializeField]
	private string message;

	public override string taskTitle => ("弹出重要提示: \"" + message + "\" ").CapLength(30);

	public override void DoAction(Graph graph)
	{
		DolocAPI.ShowMessageBoxAttention(message);
	}
}
