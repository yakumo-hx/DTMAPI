using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public abstract class MissionContentHandle
{
	public abstract bool IsInvalid { get; }

	public abstract bool IsComplete { get; }

	public abstract bool IsCompleteLoadArchive { get; }

	public abstract string MissionStatus { get; }

	public abstract string BriefStatus { get; }

	public virtual IEnumerable<MissionLog> MissionLogs => Array.Empty<MissionLog>();

	public abstract bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged);

	public abstract bool TryInvokeHistoryInSandBox(out string reason);

	public abstract bool IsCompleteBeforeInit();

	public abstract void ClearProgress();
}
