using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇")]
[Name("DolocLog", 0)]
public class GlobalTask_Output : ActionTask
{
	public BBParameter<string> log;

	protected override string info => "Doloc.Log:" + log.ToString();

	protected override void OnExecute()
	{
		DolocAPI.output(log.value);
		EndAction(success: true);
	}
}
