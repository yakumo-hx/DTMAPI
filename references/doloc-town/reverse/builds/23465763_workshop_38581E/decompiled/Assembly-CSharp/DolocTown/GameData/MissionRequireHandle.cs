using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public abstract class MissionRequireHandle
{
	[JsonProperty]
	public readonly MissionRequire require;

	public abstract bool IsComplete { get; }

	public abstract bool IsCompleteLoadArchive { get; }

	public abstract bool IsCompleteBeforeInit { get; }

	public abstract bool IsInvalid { get; }

	public virtual IEnumerable<MissionLog> MissionLogs => Array.Empty<MissionLog>();

	[JsonConstructor]
	protected MissionRequireHandle(MissionRequire require)
	{
		this.require = require;
	}

	public abstract bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged);

	public abstract bool TryInvokeHistoryInSandBox(out string reason);

	public abstract override string ToString();

	public abstract void ClearProgress();
}
