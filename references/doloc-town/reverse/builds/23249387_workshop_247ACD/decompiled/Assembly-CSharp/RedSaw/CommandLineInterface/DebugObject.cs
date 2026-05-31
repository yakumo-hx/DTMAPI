namespace RedSaw.CommandLineInterface;

public class DebugObject : DebugInfo
{
	public readonly DebugInfo[] infos;

	public DebugObject(string title, string groupId, string color, DebugField field, DebugInfo[] infos)
		: base(title, groupId, color, allowEdit: false, field)
	{
		this.infos = infos;
	}
}
