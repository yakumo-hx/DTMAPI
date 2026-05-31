using System;
using Cysharp.Threading.Tasks;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("设置场景提示符", 0)]
[Description("设置场景提示符")]
public class DialogueTask_OpenOperationTip : DialogueTask
{
	[SerializeField]
	private bool delay;

	[SerializeField]
	private float delayTime;

	[SerializeField]
	public bool visible = true;

	[SerializeField]
	public string tipId;

	public override string taskTitle => (visible ? "显示" : "关闭") + "场景提示符<" + tipId + ">";

	public override void DoAction(Graph graph)
	{
		Debug.Log("执行场景提示符\"" + tipId + "\"");
		if (delay)
		{
			UniTask.Delay((int)(delayTime * 1000f)).ContinueWith((Action)InvokeTip).Forget();
		}
		else
		{
			InvokeTip();
		}
	}

	private void InvokeTip()
	{
		if (visible)
		{
			DolocAPI.InvokeSceneResidentTip(tipId);
		}
		else
		{
			DolocAPI.HideSceneResidentTip(tipId);
		}
	}
}
