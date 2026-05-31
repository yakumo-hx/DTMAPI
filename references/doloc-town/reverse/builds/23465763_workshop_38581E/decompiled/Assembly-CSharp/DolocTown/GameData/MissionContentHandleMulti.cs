using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DolocTown.GameData;

public class MissionContentHandleMulti : MissionContentHandle
{
	[JsonProperty]
	private MissionRequireHandle[] handles;

	[JsonProperty]
	private MissionContentMulti.RequireMode mode;

	[JsonProperty]
	private List<int> completedSubRequireIndex;

	public override bool IsInvalid => handles.Any((MissionRequireHandle handle) => handle.IsInvalid);

	public override bool IsComplete
	{
		get
		{
			if (mode != 0)
			{
				return AnyComplete;
			}
			return AllComplete;
		}
	}

	public override bool IsCompleteLoadArchive
	{
		get
		{
			if (mode != 0)
			{
				return handles.Any((MissionRequireHandle handle) => handle.IsCompleteLoadArchive);
			}
			return handles.All((MissionRequireHandle handle) => handle.IsCompleteLoadArchive);
		}
	}

	private bool AllComplete => completedSubRequireIndex.Count >= handles.Length;

	private bool AnyComplete => completedSubRequireIndex.Count > 0;

	public override string MissionStatus
	{
		get
		{
			string text = string.Empty;
			MissionRequireHandle[] array = handles;
			foreach (MissionRequireHandle missionRequireHandle in array)
			{
				text += $"{missionRequireHandle.require}{missionRequireHandle}\n";
			}
			return text;
		}
	}

	public override string BriefStatus
	{
		get
		{
			MissionRequireHandle[] array = handles;
			foreach (MissionRequireHandle missionRequireHandle in array)
			{
				if (!missionRequireHandle.IsComplete)
				{
					return missionRequireHandle.ToString();
				}
			}
			return string.Empty;
		}
	}

	public override IEnumerable<MissionLog> MissionLogs => handles.SelectMany((MissionRequireHandle handle) => handle.MissionLogs);

	public MissionContentHandleMulti(MissionContentMulti content, MissionContentMulti.RequireMode requireMode = MissionContentMulti.RequireMode.All)
	{
		mode = requireMode;
		handles = new MissionRequireHandle[content.missionEvents.Length];
		for (int i = 0; i < handles.Length; i++)
		{
			handles[i] = content.missionEvents[i].CreateHandle();
		}
		completedSubRequireIndex = new List<int>();
	}

	[JsonConstructor]
	private MissionContentHandleMulti(MissionRequireHandle[] handles, MissionContentMulti.RequireMode mode, List<int> completedSubRequireIndex = null)
	{
		this.handles = handles;
		this.completedSubRequireIndex = completedSubRequireIndex ?? new List<int>();
		this.mode = mode;
	}

	private int GetRequireIndex(MissionRequireHandle handle)
	{
		for (int i = 0; i < handles.Length; i++)
		{
			if (handles[i] == handle)
			{
				return i;
			}
		}
		return -1;
	}

	protected bool OnSubRequireCompleted(MissionRequireHandle handle)
	{
		if (mode != 0)
		{
			return true;
		}
		int requireIndex = GetRequireIndex(handle);
		if (requireIndex == -1)
		{
			return false;
		}
		if (completedSubRequireIndex.Contains(requireIndex))
		{
			return AllComplete;
		}
		completedSubRequireIndex.Add(requireIndex);
		return AllComplete;
	}

	public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
	{
		statusChanged = false;
		MissionRequireHandle[] array = handles;
		foreach (MissionRequireHandle missionRequireHandle in array)
		{
			if (missionRequireHandle.SendMessage(eventType, args, out var statusChanged2) && OnSubRequireCompleted(missionRequireHandle))
			{
				statusChanged = statusChanged2 | statusChanged;
				return true;
			}
		}
		return false;
	}

	public override bool TryInvokeHistoryInSandBox(out string reason)
	{
		int num = 0;
		StringBuilder stringBuilder = new StringBuilder();
		MissionRequireHandle[] array = handles;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].TryInvokeHistoryInSandBox(out var reason2))
			{
				stringBuilder.AppendLine(reason2);
			}
			else
			{
				num++;
			}
		}
		reason = "触发事件次数不足";
		if (mode != MissionContentMulti.RequireMode.Any)
		{
			return num == handles.Length;
		}
		return num > 0;
	}

	public override void ClearProgress()
	{
		completedSubRequireIndex.Clear();
		MissionRequireHandle[] array = handles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ClearProgress();
		}
	}

	public override bool IsCompleteBeforeInit()
	{
		if (mode != 0)
		{
			return handles.Any((MissionRequireHandle h) => h.IsCompleteBeforeInit);
		}
		return handles.All((MissionRequireHandle h) => h.IsCompleteBeforeInit);
	}
}
