using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown.GameData;

public class MissionContentHandleSingle : MissionContentHandle
{
	[JsonProperty]
	private MissionRequireHandle handle;

	public override bool IsInvalid => handle.IsInvalid;

	public override bool IsComplete => handle.IsComplete;

	public override bool IsCompleteLoadArchive => handle.IsCompleteLoadArchive;

	public override string MissionStatus => handle.ToString();

	public override string BriefStatus => handle.ToString();

	public override IEnumerable<MissionLog> MissionLogs => handle.MissionLogs;

	public MissionContentHandleSingle(MissionContentSingle content)
	{
		handle = content.require.CreateHandle();
	}

	[JsonConstructor]
	private MissionContentHandleSingle(MissionRequireHandle handle)
	{
		this.handle = handle;
	}

	public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
	{
		return handle.SendMessage(eventType, args, out statusChanged);
	}

	public override bool TryInvokeHistoryInSandBox(out string reason)
	{
		return handle.TryInvokeHistoryInSandBox(out reason);
	}

	public override bool IsCompleteBeforeInit()
	{
		return handle.IsCompleteBeforeInit;
	}

	public override void ClearProgress()
	{
		handle.ClearProgress();
	}
}
