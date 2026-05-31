using System.Collections.Generic;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown.NodeCanvas;

[JsonObject(MemberSerialization.OptIn)]
public abstract class MissionRequireTemplateHandle
{
	[JsonProperty]
	public string fingerPrint;

	private List<MissionLog> missionLogs = new List<MissionLog>();

	private MissionRequireTemplate _template;

	public abstract bool IsComplete { get; }

	public abstract bool IsCompleteBeforeInit { get; }

	public IEnumerable<MissionLog> MissionLogs => missionLogs;

	protected MissionRequireTemplateHandle(MissionRequireTemplate template)
	{
		fingerPrint = template.GetTemplateFingerPrint() ?? string.Empty;
		_template = template;
	}

	[JsonConstructor]
	protected MissionRequireTemplateHandle(string fingerPrint)
	{
		this.fingerPrint = fingerPrint ?? string.Empty;
	}

	public abstract bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged);

	public abstract bool TryInvokeHistoryInSandBox(out string reason);

	public abstract bool LoadTemplate(MissionRequireTemplate template);

	public abstract void ClearProgress();

	public void Log(string msg, bool isImportant)
	{
		missionLogs.Add(new MissionLog(isImportant, msg, DolocUtils.timeStr));
	}

	public abstract override string ToString();
}
