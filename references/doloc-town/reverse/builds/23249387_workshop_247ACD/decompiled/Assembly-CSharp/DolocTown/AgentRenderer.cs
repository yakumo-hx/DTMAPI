using Cysharp.Threading.Tasks;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

public class AgentRenderer : CharacterRenderer
{
	public override string EntityId => "player";

	public override float DefaultWalkSpeed => DolocConfig.Tables.TbNpc.GetOrDefault("player")?.WalkSpeed ?? 0f;

	public override bool DisableActing => false;

	public override void SetToMarkPoint(string markPointId)
	{
		StopWalk();
		DolocAPI.DoTransport(markPointId);
	}

	public override async UniTask WalkTo(Vector2 pos, float speed)
	{
		await InternalWalkTo(pos, speed, delegate(Vector2 pos)
		{
			positionWS = pos;
			return true;
		}, null);
	}

	public override void PlayAnimation(string name, bool force, float normalizedTime = 0f)
	{
		AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (force || !animatorStateInfo.IsName(name))
		{
			DolocAPI.agent.PlayAnimation(name, normalizedTime);
		}
	}

	protected override void OnWalkEnd()
	{
		base.OnWalkEnd();
		DolocAPI.agent.StateManager.Overwrite<AgentStateIdle>();
	}

	public override void SetSortingOrder(string sortingLayerName, int order)
	{
		DolocAPI.agent.SortingLayerName = sortingLayerName;
		DolocAPI.agent.SortingOrder = order;
	}
}
