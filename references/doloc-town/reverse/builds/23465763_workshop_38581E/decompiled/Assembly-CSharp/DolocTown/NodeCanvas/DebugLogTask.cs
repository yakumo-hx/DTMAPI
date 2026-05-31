using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇")]
[Name("UnityLog", 0)]
[Description("打印Unity日志")]
public class DebugLogTask : ActionTask
{
	public BBParameter<string> log;

	protected override string info => "Unity.Log:" + log.ToString();

	protected override void OnExecute()
	{
		Debug.Log(log.value);
		EndAction(success: true);
	}
}
