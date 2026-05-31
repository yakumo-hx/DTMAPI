using System;
using DolocTown.Config;
using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown;

public class SimpleGate : Gate
{
	[SerializeField]
	public string templatePortalId;

	[SerializeField]
	private SimpleGate targetGate;

	[SerializeField]
	protected string overrideRoomId;

	public override PortalInfo config
	{
		get
		{
			if (_config == null)
			{
				_config = DolocConfig.Tables.TbPortal.GetOrDefault(templatePortalId ?? "");
			}
			return _config;
		}
	}

	protected override bool _DoTransport(Action callback)
	{
		if (targetGate == null || config == null || targetGate.config == null)
		{
			return false;
		}
		if (!DolocAPI.agent.IsCurrentStateSupportTeleport)
		{
			return false;
		}
		float y = (config.NeedInteract ? 0f : Mathf.Max(0f, DolocAPI.AgentPosition.y - position.y));
		TransportOverrideInfo overrideInfo = new TransportOverrideInfo(targetGate.overrideRoomId, targetGate.position);
		return DolocAPI.DoTransport(config.TargetId, new Vector2(0f, y), callback, config.NeedInteract, disableFadeIn: false, disableFadeOut: false, overrideInfo);
	}
}
