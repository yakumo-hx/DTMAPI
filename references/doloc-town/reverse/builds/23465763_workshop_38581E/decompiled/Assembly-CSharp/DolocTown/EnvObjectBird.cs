using DolocTown.Config.Resource;
using UnityEngine;

namespace DolocTown;

[EnvObject(EnvObjectType.BIRD)]
public class EnvObjectBird : EnvObject
{
	public EnvObjectBird(EnvObjectInfo proto, Vector2 position)
		: base(proto, position)
	{
	}

	protected override bool ValidateDeserialization()
	{
		return base.Proto != null;
	}

	public override void OnRender()
	{
		base.OnRender();
		base.Renderer.SetSrFlipX(Random.value > 0.5f);
		int animIndex = Random.Range(0, 3);
		DolocAPI.Delay(Random.value, delegate
		{
			if (base.Renderer != null)
			{
				base.Renderer.PlayAnimation($"idle_{animIndex}");
			}
		});
	}

	public override void OnTouch()
	{
		base.OnTouch();
		if (!HasTouched)
		{
			HasTouched = true;
			base.Renderer.PlayAnimation("fly");
			int num = ((DolocAPI.AgentPosition.x < base.PositionWS.x) ? 1 : (-1));
			Vector2 roomSize = base.Host.CurrentRoom.baseProto.geometry.roomSize;
			roomSize.x *= num;
			base.Renderer.SetMoveTarget(roomSize, base.Proto.Speed);
			base.Host.DM_envObject.RemoveEnvObject(this);
			(base.Host as IDropItemHost)?.CreateDropItemAnimated(DolocAPI.archiveHandle.timeData.SeasonProto.BirdDropSpawnEntry.SpawnItems(), base.PositionWS, shouldSendMsg: true);
		}
	}
}
