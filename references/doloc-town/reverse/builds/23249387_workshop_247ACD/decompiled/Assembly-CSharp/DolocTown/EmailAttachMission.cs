using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class EmailAttachMission : EmailAttachBase
{
	[JsonProperty]
	private string missionChainId;

	[JsonProperty]
	private bool autoAccept;

	[JsonProperty]
	private bool isAccept;

	public bool AutoAccept => autoAccept;

	public override bool HasAttach => true;

	public override bool IsAccept => isAccept;

	public EmailAttachMission(string missionChainId, bool autoAccept)
	{
		this.missionChainId = missionChainId;
		this.autoAccept = autoAccept;
	}

	[JsonConstructor]
	private EmailAttachMission(string missionChainId, bool autoAccept, bool isAccept)
	{
		this.missionChainId = missionChainId;
		this.autoAccept = autoAccept;
		this.isAccept = isAccept;
	}

	public override void OnFirstRead()
	{
		if (autoAccept)
		{
			if (!DolocAPI.StartMissionChain(missionChainId))
			{
				Debug.LogError("任务链<" + missionChainId + ">启动失败");
			}
			else
			{
				isAccept = true;
			}
		}
	}

	public override bool OnAccept()
	{
		if (isAccept)
		{
			return true;
		}
		isAccept = DolocAPI.StartMissionChain(missionChainId);
		return isAccept;
	}

	public override string ToString()
	{
		return "任务附件:<" + missionChainId + ">";
	}
}
